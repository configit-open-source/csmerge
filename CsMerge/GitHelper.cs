using System;
using System.Diagnostics;
using System.IO;

namespace CsMerge {
  public class GitHelper {
    private const string GitExePath = "C:\\Users\\pt\\AppData\\Local\\GitHub\\PortableGit_c2ba306e536fdf878271f7fe636a147ff37326ad\\cmd\\git.exe";

    public static string GetGitValue( string gitCmdArgs, string cmd = "config", string workingDir = null ) {
      workingDir = workingDir ?? Directory.GetCurrentDirectory();

      var file = GitExePath;
      Debug.Assert( File.Exists( file ) );

      var arguments = cmd + " " + gitCmdArgs;
      //Console.WriteLine( "Executing " + file + " " + arguments );
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
      string mergetool = GetGitValue( "merge.tool" );
      return GetGitValue( "mergetool." + mergetool + ".cmd" );
    }
  }
}