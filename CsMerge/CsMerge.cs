using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CsMerge.Core;
using CsMerge.Core.Exceptions;
using CsMerge.Core.Resolvers;
using CsMerge.Properties;
using CsMerge.Resolvers;
using CsMerge.UserQuestion;
using LibGit2Sharp;
using NLog;

using LogLevel = NLog.LogLevel;
using Reference = CsMerge.Core.Reference;

namespace CsMerge {

  /// <summary>
  /// See README.md
  /// </summary>
  public class CsMerge {

    private const string PackageNotInstalledText = "Package not installed";

    /// <summary>
    /// See README.md
    /// </summary>
    public static void Main( string[] args ) {

      if ( Settings.Default.Debug ) {
        Debugger.Launch();
      }

      try {
        if ( args.Length == 0 ) {
          args = new[] { Directory.GetCurrentDirectory() };
        }

        DirectoryInfo folder = new DirectoryInfo( args[0] );
        var logger = LogManager.GetCurrentClassLogger();

        var rootFolder = GitHelper.FindRepoRoot( folder.FullName );

        if ( args.Length >= 2 && args[1] == "--upgrade" ) {
          logger.Info( "Aligning references in " + rootFolder );

          string pattern = args.Length == 5 ? args[2] : null;
          string patternVersion = args.Length == 5 ? args[3] : null;
          string framework = args.Length == 5 ? args[4] : null;

          var aligner =
            new PackageReferenceAligner( rootFolder,
            FindRelativePathOfPackagesFolder( rootFolder ), pattern, patternVersion, framework );

          foreach ( var projectFile in folder.GetFiles( "*.csproj", SearchOption.AllDirectories ) ) {
            aligner.AlignReferences( projectFile.FullName );
          }
          return;
        }

        logger.Info( "Looking for things to merge in " + folder );

        string[] conflictPaths;
        CurrentOperation operation;

        using ( var repository = new Repository( rootFolder ) ) {
          if ( repository.Index.IsFullyMerged ) {
            logger.Info( "Nothing to do, already fully merged" );
            return;
          }
          conflictPaths = repository.Index.Conflicts.Select( c => c.GetPath() ).ToArray();
          operation = repository.Info.CurrentOperation;
        }

        ProcessPackagesConfig( operation, conflictPaths, folder, logger, rootFolder );

        ProcessProjectFiles( operation, conflictPaths, folder, logger, rootFolder );
      } catch ( UserQuitException ) {
        Console.WriteLine( "The user quit." );
      } catch ( Exception exception ) {
        Console.WriteLine( $"An error occured: {Environment.NewLine}{exception}" );
      }
    }

    private static void ProcessPackagesConfig(
      CurrentOperation operation,
      string[] conflictPaths,
      DirectoryInfo folder,
      Logger logger,
      string rootFolder ) {

      var packagesConfigMerger = new PackagesConfigMerger(
        operation,
        new UserConflictResolver<Package>( operation, repositoryRootDirectory: rootFolder ) );

      foreach ( var conflict in conflictPaths.Where( p => Path.GetFileName( p ) == "packages.config" ) ) {

        var fullConflictPath = Path.Combine( folder.FullName, conflict );
        logger.Info( $"{LogHelper.Header}{Environment.NewLine}Examining concurrent modification for {fullConflictPath}" );

        var baseContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ancestor, conflict );
        var localContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ours, conflict );
        var incomingContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Theirs, conflict );

        if ( string.IsNullOrEmpty( localContent ) || string.IsNullOrEmpty( incomingContent ) ) {
          logger.Log( LogLevel.Info, $"Skipping '{conflict}' - no content on one side" );
          continue;
        }

        bool resolved = false;

        try {
          var result = packagesConfigMerger.Merge(
              conflict,
              Package.Parse( baseContent ),
              Package.Parse( localContent ),
              Package.Parse( incomingContent ) ).ToArray();

          Package.Write( result, fullConflictPath );

          using ( var repository = new Repository( rootFolder ) ) {
            repository.Stage( conflict );
            resolved = true;
          }

        } catch ( MergeAbortException ) {
          logger.Log( LogLevel.Info, $"Package merge aborted for {conflict}" );
          continue;
        } catch ( UserQuitException ) {
          throw;
        } catch ( Exception exception ) {
          logger.Log( LogLevel.Error, exception, $"Package merge failed for {conflict}{Environment.NewLine}{exception}" );
        }

        if ( !resolved ) {

          string userQuestionText = $"Could not resolve conflict: {conflict}{Environment.NewLine}Would you like to resolve the conflict with the mergetool?";
          var userQuestion = new UserQuestion<bool>( userQuestionText, UserQuestion<bool>.YesNoOptions() );

          if ( userQuestion.Resolve() ) {

            var localDocument = XDocument.Parse( localContent );
            var theirDocument = XDocument.Parse( incomingContent );
            var baseDocument = XDocument.Parse( baseContent );

            using ( var repository = new Repository( rootFolder ) ) {
              GitHelper.ResolveWithStandardMergetool(
                repository,
                fullConflictPath,
                baseDocument,
                localDocument,
                theirDocument,
                logger,
                conflict );
            }
          }
        }
      }
    }

    private static void ProcessProjectFiles(
      CurrentOperation operation,
      string[] conflictPaths,
      DirectoryInfo folder,
      Logger logger,
      string rootFolder ) {

      var merger = new ProjectMerger(
        operation,
        new UserConflictResolver<ProjectReference>( operation, repositoryRootDirectory: rootFolder ),
        new ReferenceConflictResolver( new UserConflictResolver<Reference>( operation, notResolveOptionText: PackageNotInstalledText, repositoryRootDirectory: rootFolder ) ),
        new UserConflictResolver<RawItem>( operation, repositoryRootDirectory: rootFolder ),
        new UserDuplicateResolver<Reference>( operation, notResolveOptionText: PackageNotInstalledText, repositoryRootDirectory: rootFolder ) );

      foreach ( var conflict in conflictPaths.Where( p => p.EndsWith( ".csproj" ) ) ) {

        var fullConflictPath = Path.Combine( folder.FullName, conflict );
        logger.Info( $"{LogHelper.Header}{Environment.NewLine}Examining concurrent modification for {fullConflictPath}" );
        
        var baseContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ancestor, conflict );
        var localContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ours, conflict );
        var incomingContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Theirs, conflict );

        var conflictFolder = Path.GetDirectoryName( Path.Combine( rootFolder, conflict ) );

        if ( conflictFolder == null ) {
          throw new Exception( "No conflict folder" );
        }

        if ( string.IsNullOrEmpty( localContent ) || string.IsNullOrEmpty( incomingContent ) ) {
          logger.Log( LogLevel.Info, $"Skipping '{conflict}' - no content on one side" );
          continue;
        }

        var localDocument = XDocument.Parse( localContent );
        var incomingDocument = XDocument.Parse( incomingContent );
        var baseDocument = XDocument.Parse( baseContent );

        var resolved = false;

        try {
          var projectFolder = Path.Combine( folder.FullName, conflictFolder );

          var packageIndex = new ProjectPackages( projectFolder, FindRelativePathOfPackagesFolder( projectFolder ) );

          var items = merger.Merge(
              conflict,
              packageIndex,
              baseDocument,
              localDocument,
              incomingDocument ).ToArray();

          // Now remove everything we have handled, to check if we are done.
          ProjectFile.DeleteItems( localDocument );
          ProjectFile.DeleteItems( incomingDocument );
          ProjectFile.DeleteItems( baseDocument );

          ProjectFile.AddItems( baseDocument, items );
          ProjectFile.AddItems( localDocument, items );
          ProjectFile.AddItems( incomingDocument, items );

          XDocument resolvedDocument = null;

          var localXml = localDocument.ToString();
          var incomingXml = incomingDocument.ToString();
          var baseXml = baseDocument.ToString();

          // Check for any project file changes outside of the references and items. 
          if ( localXml == incomingXml ) {
            resolvedDocument = localDocument;
          } else if ( baseXml == localXml ) {
            resolvedDocument = incomingDocument;
          } else if ( baseXml == incomingXml ) {
            resolvedDocument = localDocument;
          }

          if ( resolvedDocument != null ) {
            // We handled all the differences
            using ( var textWriter = new StreamWriter( fullConflictPath ) ) {
              resolvedDocument.WriteXml( textWriter );
            }
            using ( var repository = new Repository( rootFolder ) ) {
              repository.Stage( conflict );
            }

            resolved = true;
          }

        } catch ( MergeAbortException ) {
          logger.Log( LogLevel.Info, $"Project merge aborted for {conflict}" );
          continue;
        } catch ( UserQuitException ) {
          throw;
        } catch ( Exception exception ) {
          logger.Log( LogLevel.Error, exception, $"Project merge failed for {conflict}{Environment.NewLine}{exception}" );
        }

        if ( !resolved ) {

          string userQuestionText = $"Could not resolve conflict: {conflict}{Environment.NewLine}Would you like to resolve the conflict with the mergetool?";
          var userQuestion = new UserQuestion<bool>( userQuestionText, UserQuestion<bool>.YesNoOptions() );

          if ( userQuestion.Resolve() ) {

            using ( var repository = new Repository( rootFolder ) ) {
              GitHelper.ResolveWithStandardMergetool(
                repository,
                fullConflictPath,
                baseDocument,
                localDocument,
                incomingDocument,
                logger,
                conflict );
            }
          }
        }
      }
    }

    /// <summary>
    /// Gets the relative path of the packages folder.
    /// </summary>
    /// <param name="folder">The folder that the returned path should be relative to. If null then the current directory is used.</param>
    public static string FindRelativePathOfPackagesFolder( string folder = null ) {
      var current = new DirectoryInfo( folder ?? Directory.GetCurrentDirectory() );

      var depth = 0;

      while ( !new DirectoryInfo( Path.Combine( current.FullName, ".git" ) ).Exists ) {
        depth++;
        current = current.Parent;
        if ( current == null ) {
          throw new Exception( "Could not locate \".git\" folder" );
        }
      }

      return Enumerable.Repeat( "..", depth ).Aggregate( "packages", ( current1, e ) => Path.Combine( e, current1 ) );
    }
  }
}
