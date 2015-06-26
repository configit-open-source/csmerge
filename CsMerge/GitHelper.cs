using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Cpc.CsMerge.Core;

using LibGit2Sharp;

using NLog;
namespace CsMerge {

  public class GitHelper {

    public static string GetMergeCmdLine( Repository repository ) {
      ConfigurationEntry<string> mergeToolName = repository.Config.Get<string>( "merge.tool" );
      var cmd = repository.Config.Get<string>( "mergetool." + mergeToolName.Value + ".cmd" );
      return cmd.Value;
    }

    public static int RunStandardMergetool( Repository repository, string @base, string local, string resolved, string theirs, Logger logger ) {
      string cmdLine =
        GetMergeCmdLine( repository )
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

    public static string GetConflictContent( string rootFolder, StageLevel stage, string conflict ) {
      using ( var repository = new Repository( rootFolder ) ) {
        var conflictEntry = repository.Index.Conflicts.FirstOrDefault( c => c.Ours.Path == conflict );

        if ( conflictEntry == null ) {
          throw new InvalidDataException( "Could not find the matching conflict entry for " + conflict );
        }

        var stageBlob =
          repository.Lookup<Blob>( new[] {
            conflictEntry.Ancestor, conflictEntry.Ours, conflictEntry.Theirs
          }.Single( e => e.StageLevel == stage ).Id );

        using ( var reader = new StreamReader( stageBlob.GetContentStream() ) ) {
          return reader.ReadToEnd();
        }
      }
    }

    public static string FindRepoRoot( string folder ) {
      var current = new DirectoryInfo( folder ?? Directory.GetCurrentDirectory() );
      while ( !new DirectoryInfo( Path.Combine( current.FullName, ".git" ) ).Exists ) {
        current = current.Parent;
        if ( current == null ) {
          throw new Exception( "Could not locate \".git\" folder" );
        }
      }
      return current.FullName;
    }
  }
}