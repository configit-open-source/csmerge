using System;
using System.Diagnostics;
using System.IO;

using NLog;

namespace CsMerge {
  public class GitHelper {
    private const string GitExePath =
      "C:\\Users\\pt\\AppData\\Local\\GitHub\\PortableGit_c2ba306e536fdf878271f7fe636a147ff37326ad\\cmd\\git.exe";

    public static string RunGitCmd( string cmd = "config", string gitCmdArgs = null, string workingDir = null ) {
      workingDir = workingDir ?? Directory.GetCurrentDirectory();

      var file = GitExePath;
      Debug.Assert( File.Exists( file ) );

      var logger = LogManager.GetCurrentClassLogger();

      var arguments = cmd + " " + gitCmdArgs;
      logger.Debug( "Executing " + file + " " + arguments );
      var processStartInfo = new ProcessStartInfo( file, arguments );
      processStartInfo.RedirectStandardOutput = true;
      processStartInfo.UseShellExecute = false;
      processStartInfo.CreateNoWindow = true;
      processStartInfo.WorkingDirectory = workingDir;
      processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

      var process = Process.Start( processStartInfo );

      if ( process == null ) {
        throw new Exception( "Could not execute " + processStartInfo.FileName + " " + processStartInfo.Arguments );
      }

      string result = process.StandardOutput.ReadToEnd();
      process.WaitForExit();
      return result;
    }

    public static string GetMergeCmdLine() {
      string mergetool = RunGitCmd( gitCmdArgs : "merge.tool" );
      return RunGitCmd( gitCmdArgs : "mergetool." + mergetool + ".cmd" );
    }

    public static int RunStandardMergetool( string @base, string local, string resolved, string theirs, Logger logger ) {
      string cmdLine =
        GitHelper.GetMergeCmdLine()
          .Replace( "$BASE", @base )
          .Replace( "$LOCAL", local )
          .Replace( "$MERGED", resolved )
          .Replace( "$REMOTE", theirs );

      logger.Debug( "Invoking:\n" + cmdLine );

      var processStartInfo = new ProcessStartInfo( "cmd.exe", "/C \"" + cmdLine + "\"" ) {
        CreateNoWindow = true,
        UseShellExecute = true,
      };

      var process = Process.Start( processStartInfo );

      if ( process == null ) {
        throw new Exception( "Could not execute " + cmdLine );
      }

      process.WaitForExit();
      return process.ExitCode;
    }

    public static string GetContent( int stage, string path, string folder ) {
      // :<n>:<path>, e.g. :0:README, :README
      //A colon, optionally followed by a stage number (0 to 3) and a colon, 
      // followed by a path, names a blob object in the index at the given path.
      // A missing stage number (and the colon that follows it) names a stage 0 entry. 
      // During a merge, stage 1 is the common ancestor, stage 2 is the target branch’s version 
      // (typically the current branch), and stage 3 is the version from the branch which is being merged.
      return GitHelper.RunGitCmd( cmd : "show", gitCmdArgs : ":" + stage + ":" + path, workingDir : folder );
    }

    public static void ResolveWithStandardMergetool(
      string fullConflictPath,
      string baseContent,
      string localContent,
      string theirContent,
      Logger logger,
      string conflict ) {
      // Run the standard mergetool to deal with any remaining issues.
      var basePath = fullConflictPath + "_base";
      var localPath = fullConflictPath + "_local";
      var theirsPath = fullConflictPath + "_theirs";

      File.WriteAllText( basePath, baseContent );

      File.WriteAllText( localPath, localContent );

      File.WriteAllText( theirsPath, theirContent );
      if ( GitHelper.RunStandardMergetool( basePath, localPath, fullConflictPath, theirsPath, logger ) == 0 ) {
        // The merge tool reports that the conflict was resolved
        GitHelper.RunGitCmd( "add", workingDir : basePath, gitCmdArgs : conflict );
        logger.Info( "Manually resolved " + fullConflictPath );
      }
      else {
        logger.Info( "Did not resolve " + fullConflictPath );
      }
    }
  }
}