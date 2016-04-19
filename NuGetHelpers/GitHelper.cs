using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using LibGit2Sharp;

using NLog;

using Project;

namespace Integration {
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
          .Replace( "$LOCAL", MergeTypeIntegrationExtensions.Local( currentOperation ) == MergeTypeIntegrationExtensions.Mine ? local : incoming )
          .Replace( "$MERGED", resolved )
          .Replace( "$REMOTE", MergeTypeIntegrationExtensions.Local( currentOperation ) == MergeTypeIntegrationExtensions.Mine ? incoming : local );

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

    public static void ConfigureGitConfig( 
        ConfigurationLevel level = default(ConfigurationLevel), 
        string file = null ) {
      var config = new Configuration( file, file, file );

      LogManager.GetCurrentClassLogger().Info( "Installing failmerge driver to " + level );
      config.Set( "merge.failmerge.name", "fail merge driver", level );
      config.Set( "merge.failmerge.driver", "false", level );
      config.Set( "merge.failmerge.recursive", "binary", level );
    }

    public static void ConfigureGitAttrib( string attribFile ) {
      // TODO: How to set this via git api?
      
      string attrib = File.ReadAllText( attribFile );

      string[] patterns = {
        "**/packages.config failmerge",
        "**/*.csproj failmerge",
        "**/*.fsproj failmerge",
        "**/*.xproj failmerge"
      };

      foreach ( var pattern in patterns.Where( pattern => !attrib.Contains( pattern ) ) ) {
        LogManager.GetCurrentClassLogger().Info( "Setting failmerge attribute for " + pattern + " in " + attribFile );
        File.AppendAllText( attribFile, pattern + "\n" );
      }
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
          throw new Exception( $"The path {folder} must contain a .git folder in order to for CsMerge to run" );
        }
      }
      return current.FullName;
    }

    public static void ResolveWithStandardMergetool(
     Repository repository,
     string fullConflictPath,
     XDocument baseContent,
     XDocument localContent,
     XDocument incomingContent,
     Logger logger,
     string conflict ) {

      // Run the standard mergetool to deal with any remaining issues.
      var basePath = fullConflictPath + "_base";
      var localPath = fullConflictPath + "_local";
      var incomingPath = fullConflictPath + "_incoming";

      baseContent.WriteXml( basePath );
      localContent.WriteXml( localPath );
      incomingContent.WriteXml( incomingPath );

      if ( RunStandardMergetool( repository, basePath, localPath, incomingPath, fullConflictPath ) == 0 ) {
        // The merge tool reports that the conflict was resolved
        logger.Info( "Resolved " + fullConflictPath + " using standard merge tool" );
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

    public static XElement ResolveWithStandardMergetool(
     string repositoryRootDirectory,
     string key,
     IEnumerable<XElement> baseElements,
     IEnumerable<XElement> localElements,
     IEnumerable<XElement> incomingElements,
     Logger logger = null ) {

      // Run the standard mergetool to merge an item.

      var rootPath = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString(), key );

      Directory.CreateDirectory( rootPath );

      var basePath = Path.Combine( rootPath, "Base.tmp" );
      var localPath = Path.Combine( rootPath, "Local.tmp" );
      var incomingPath = Path.Combine( rootPath, "Incoming.tmp" );
      var resolvedPath = Path.Combine( rootPath, "resolved.tmp" );

      var settings = SerialisationHelper.DefaultWriterSettings();
      settings.OmitXmlDeclaration = true;
      settings.ConformanceLevel = ConformanceLevel.Fragment;

      baseElements.WriteXml( basePath, settings );
      localElements.WriteXml( localPath, settings );
      incomingElements.WriteXml( incomingPath, settings );

      try {

        int returnCode;

        using ( var repository = new Repository( repositoryRootDirectory ) ) {
          returnCode = RunStandardMergetool( repository, basePath, localPath, incomingPath, resolvedPath );
        }

        if ( returnCode == 0 ) {
          // The merge tool reports that the conflict was resolved
          if ( logger != null ) {
            logger.Info( "Resolved " + key + " using standad merge tool" );
          }

          var xml = File.ReadAllText( resolvedPath );

          if ( string.IsNullOrWhiteSpace( xml ) ) {
            return null;
          }

          var resolvedElement = XElement.Parse( xml );

          return resolvedElement;

        } else {
          if ( logger != null ) {
            logger.Info( "Did not resolve " + key );
          }
          throw new OperationCanceledException();
        }
      } finally {
        File.Delete( basePath );
        File.Delete( incomingPath );
        File.Delete( localPath );
        File.Delete( resolvedPath );
        Directory.Delete( rootPath );
      }
    }

  }
}