using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LibGit2Sharp;
using NLog;

namespace CsMerge.Core {

  public class GitHelper {

    public static string GetMergeCmdLine( Repository repository ) {
      ConfigurationEntry<string> mergeToolName = repository.Config.Get<string>( "merge.tool" );
      var cmd = repository.Config.Get<string>( "mergetool." + mergeToolName.Value + ".cmd" );
      return cmd.Value;
    }

    private static int RunStandardMergetool( Repository repository, string @base, string local, string incoming, string resolved ) {
      var logger = LogManager.GetCurrentClassLogger();

      var currentOperation = repository.Info.CurrentOperation;

      string cmdLine =
        GetMergeCmdLine( repository )
          .Replace( "$BASE", @base )
          .Replace( "$LOCAL", MergeTypeExtensions.Local( currentOperation ) == MergeTypeExtensions.Mine ? local : incoming )
          .Replace( "$MERGED", resolved )
          .Replace( "$REMOTE", MergeTypeExtensions.Local( currentOperation ) == MergeTypeExtensions.Mine ? incoming : local );

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
        var conflictEntry = repository.Index.Conflicts.FirstOrDefault( c => c.GetPath() == conflict );

        if ( conflictEntry == null ) {
          throw new InvalidDataException( "Could not find the matching conflict entry for " + conflict );
        }

        IndexEntry entry = conflictEntry.GetEntry( stage );

        if ( entry == null ) {
          return null;
        }

        Blob stageBlob = repository.Lookup<Blob>( entry.Id );

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

    public static void ResolveWithStandardMergetool(
     Repository repository,
     string fullConflictPath,
     XDocument baseContent,
     XDocument localContent,
     XDocument theirContent,
     Logger logger,
     string conflict ) {

      // Run the standard mergetool to deal with any remaining issues.
      var basePath = fullConflictPath + "_base";
      var localPath = fullConflictPath + "_local";
      var incomingPath = fullConflictPath + "_theirs";

      Package.WriteXml( basePath, baseContent );
      Package.WriteXml( localPath, localContent );
      Package.WriteXml( incomingPath, theirContent );

      if ( RunStandardMergetool( repository, basePath, localPath, incomingPath, fullConflictPath ) == 0 ) {
        // The merge tool reports that the conflict was resolved
        logger.Info( "Resolved " + fullConflictPath + " using standad merge tool" );
        File.Delete( fullConflictPath );
        File.Move( localPath, fullConflictPath );

        repository.Stage( conflict );
      } else {
        logger.Info( "Did not resolve " + fullConflictPath );
        throw new OperationCanceledException();
      }

      File.Delete( basePath );
      File.Delete( incomingPath );
    }

  }
}