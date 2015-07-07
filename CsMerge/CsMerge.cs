using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using CsMerge.Core;

using LibGit2Sharp;
using NLog;

using Reference = CsMerge.Core.Reference;

namespace CsMerge {

  /// <summary>
  /// See README.md
  /// </summary>
  public class CsMerge {

    /// <summary>
    /// See README.md
    /// </summary>
    public static void Main( string[] args ) {
      if ( args.Length == 0 ) {
        args = new[] { Directory.GetCurrentDirectory() };
      }


      DirectoryInfo folder = new DirectoryInfo( args[0] );
      var logger = LogManager.GetCurrentClassLogger();

      var rootFolder = GitHelper.FindRepoRoot( folder.FullName );

      if ( args.Length >= 2 && args[1] == "--align" ) {
        logger.Info( "Aligning references in " + rootFolder );

        string pattern = args.Length == 4 ? args[2] : null;
        string patternVersion = args.Length == 4 ? args[3] : null;

        var aligner =
          new PackageReferenceAligner( rootFolder, FindRelativePathOfPackagesFolder( rootFolder ), pattern, patternVersion );
        
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
        conflictPaths = repository.Index.Conflicts.Select( c => c.Ours.Path ).ToArray();
        operation = repository.Info.CurrentOperation;
      }

      UserResolvers resolvers = new UserResolvers( operation );

      ProcessPackagesConfig( resolvers, conflictPaths, folder, logger, rootFolder );

      ProcessProjectFiles( resolvers, conflictPaths, folder, logger, rootFolder );
    }

    private static void ProcessPackagesConfig(
      UserResolvers resolvers,
      string[] conflictPaths,
      DirectoryInfo folder,
      Logger logger,
      string rootFolder ) {
      foreach ( var conflict in conflictPaths.Where( p => Path.GetFileName( p ) == "packages.config" ) ) {
        var fullConflictPath = Path.Combine( folder.FullName, conflict );
        logger.Info( "Examining concurrent modification for " + fullConflictPath );

        var baseContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ancestor, conflict );
        var localContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ours, conflict );
        var theirContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Theirs, conflict );

        var result = new PackagesConfigMerger( resolvers.Operation ).Merge(
            Package.Parse( baseContent ),
            Package.Parse( localContent ),
            Package.Parse( theirContent ),
            resolvers.UserResolvePackage ).ToArray();

        Package.Write( result, fullConflictPath );
        using ( var repository = new Repository( rootFolder ) ) {
          repository.Stage( conflict );
        }
      }
    }

    private static void ProcessProjectFiles(
      UserResolvers resolvers,
      string[] conflictPaths,
      DirectoryInfo folder,
      Logger logger,
      string rootFolder ) {
      foreach ( var conflict in conflictPaths.Where( p => p.EndsWith( ".csproj" ) ) ) {
        var fullConflictPath = Path.Combine( folder.FullName, conflict );
        logger.Info( "Examining concurrent modification for " + fullConflictPath );

        var baseContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ancestor, conflict );
        var localContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ours, conflict );
        var theirContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Theirs, conflict );

        var conflictFolder = Path.GetDirectoryName( Path.Combine( rootFolder, conflict ) );

        if ( conflictFolder == null ) {
          throw new Exception( "No conflict folder" );
        }

        XDocument localDocument = XDocument.Parse( localContent );
        XDocument theirDocument = XDocument.Parse( theirContent );
        XDocument baseDocument = XDocument.Parse( baseContent );

        var projFileName = Path.GetFileName( conflict );

        var projectFolder = Path.Combine( folder.FullName, conflictFolder );

        var packageIndex = new ProjectPackages( projectFolder, FindRelativePathOfPackagesFolder( projectFolder ) );

        Item[] items = new ProjectMerger( resolvers.Operation ).Merge(
            projFileName,
            packageIndex,
            baseDocument,
            localDocument,
            theirDocument,
            resolvers.UserResolveReference<Reference>,
            resolvers.UserResolveReference<Item> ).ToArray();

        // Now remove everything we have handled, to check if we are done.
        ProjectFile.DeleteItems( localDocument );
        ProjectFile.DeleteItems( theirDocument );
        ProjectFile.DeleteItems( baseDocument );

        ProjectFile.AddItems( baseDocument, items );
        ProjectFile.AddItems( localDocument, items );
        ProjectFile.AddItems( theirDocument, items );

        if ( localDocument.ToString() == theirDocument.ToString() ) {
          // We handled all the differences
          using ( var textWriter = new StreamWriter( fullConflictPath ) ) {
            Package.WriteXml( textWriter, localDocument );
          }
          using ( var repository = new Repository( rootFolder ) ) {
            repository.Stage( conflict );
          }
        }
        else {
          using ( var repository = new Repository( rootFolder ) ) {
            resolvers.ResolveWithStandardMergetool(
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

    public static string FindRelativePathOfPackagesFolder( string folder = null ) {
      var current = new DirectoryInfo( folder ?? Directory.GetCurrentDirectory() );

      int depth = 0;

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
