using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Cpc.CsMerge.Core;

using GitSharp.Core.DirectoryCache;

using LibGit2Sharp;

using NLog;

using Repository = GitSharp.Repository;

namespace CsMerge {
  public class GitHelper {

    private static string _gitExePath = null;

    private static string GitExePath {
      get {
        if ( _gitExePath == null ) {
          _gitExePath = Environment.GetEnvironmentVariable( "GIT_EXEC_PATH" );
          if ( string.IsNullOrEmpty( _gitExePath ) ) {
            // Try to find github installation
            var gitFolder =
              new DirectoryInfo( Path.Combine( Environment.GetEnvironmentVariable( "LOCALAPPDATA" ) ??
                string.Empty, "GitHub" ) ).GetDirectories( "PortableGit_*" ).FirstOrDefault();
            _gitExePath = gitFolder == null ?
              "git.exe" : // Hoping that its on path
              Path.Combine( gitFolder.FullName, "bin", "git.exe" );
          }
        }
        return _gitExePath;
      }
    }
    
    public static string RunGitCmd( string cmd, string gitCmdArgs, string workingDir = null, bool singleLine = false ) {
      workingDir = workingDir ?? Directory.GetCurrentDirectory();

      var file = GitExePath;

      var logger = LogManager.GetCurrentClassLogger();

      var arguments = cmd + " " + gitCmdArgs;
      logger.Info( "Executing " + file + " " + arguments );
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

      string result = singleLine ? process.StandardOutput.ReadLine() : process.StandardOutput.ReadToEnd();
      process.WaitForExit();
      return result;
    }

    public static string GetMergeCmdLine() {
      string mergetool = RunGitCmd( cmd : "config", gitCmdArgs : "merge.tool", singleLine: true );
      return RunGitCmd( cmd : "config", gitCmdArgs : "mergetool." + mergetool + ".cmd", singleLine : true );
    }

    public static int RunStandardMergetool( string @base, string local, string resolved, string theirs, Logger logger ) {
      string cmdLine =
        GetMergeCmdLine()
          .Replace( "$BASE", @base )
          .Replace( "$LOCAL", local )
          .Replace( "$MERGED", resolved )
          .Replace( "$REMOTE", theirs );

      logger.Debug( "Invoking:\n" + cmdLine );

      var processStartInfo = new ProcessStartInfo( "cmd.exe", "/C \"" + cmdLine + "\"" ) {
        CreateNoWindow = true,
        UseShellExecute = false
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
      return RunGitCmd( cmd : "show", gitCmdArgs : ":" + stage + ":" + path, workingDir : folder );
    }

    public static void ResolveWithStandardMergetool(
      LibGit2Sharp.Repository repository,
      string fullConflictPath,
      XDocument baseContent,
      XDocument localContent,
      XDocument theirContent,
      Logger logger,
      string conflict ) {
      // Run the standard mergetool to deal with any remaining issues.
      var basePath = fullConflictPath + "_base";
      var localPath = fullConflictPath + "_local";
      var theirsPath = fullConflictPath + "_theirs";


      Package.WriteXml( basePath, baseContent );
      Package.WriteXml( localPath, localContent );
      Package.WriteXml( theirsPath, theirContent );

      if ( RunStandardMergetool( basePath, localPath, fullConflictPath, theirsPath, logger ) == 0 ) {
        // The merge tool reports that the conflict was resolved
        logger.Info( "Manually resolved " + fullConflictPath );
        File.Delete( fullConflictPath );
        File.Move( localPath, fullConflictPath );

        //RunGitCmd( "add", workingDir: Path.GetDirectoryName( fullConflictPath ), gitCmdArgs: conflict );
        repository.Stage( conflict );
      }
      else {
        logger.Info( "Did not resolve " + fullConflictPath );
        File.Delete( localPath );
      }

      File.Delete( basePath );
      File.Delete( theirsPath );
    }
  }
}