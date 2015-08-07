using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

using CsMerge.Core;
using CsMerge.Core.Exceptions;
using CsMerge.Core.Resolvers;
using CsMerge.Properties;
using CsMerge.Resolvers;
using CsMerge.UserQuestion;

using LibGit2Sharp;
using NLog;

using Integration;

using Project;

using LogLevel = NLog.LogLevel;
using Reference = Project.Reference;
using SerialisationHelper = CsMerge.Core.SerialisationHelper;

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

      var options = new CsMergeOptions();
      if ( !CommandLine.Parser.Default.ParseArguments( args, options ) ) {
        return;
      }

      DirectoryInfo folder = new DirectoryInfo( options.InputFolder ?? Directory.GetCurrentDirectory() );
      var logger = LogManager.GetCurrentClassLogger();

      try {
        var rootFolder = GitHelper.FindRepoRoot( folder.FullName );

        if ( options.Mode == Mode.Align ) {
          ProcessAlign( logger, rootFolder, options, folder );
        }
        else if ( options.Mode == Mode.Merge ) {
          ProcessMerge( logger, folder, rootFolder );
        }
      }
      catch ( UserQuitException ) {
        Console.WriteLine( "The user quit." );
      }
      catch ( Exception exception ) {
        Console.Write( "An error occured: " + Environment.NewLine + exception );
      }
    }

    private static void ProcessAlign( Logger logger, string rootFolder, CsMergeOptions options, DirectoryInfo folder ) {
      logger.Info( "Updating/aligning references in " + rootFolder );

      string pattern = options.UpgradePrefix;
      string patternVersion = options.UpgradeVersion;
      string framework = options.UpgradeFramework;

      // TODO: Check specifically for known VS extensions only
      var projectFiles = folder.GetFiles( "*.*sproj", SearchOption.AllDirectories ).Select( f => f.FullName ).ToArray();

      // Restore packages now
      NuGetExtensions.RestorePackages( rootFolder );

      TargetPackageIndex targetPackageIndex = new TargetPackageIndex( projectFiles, pattern, patternVersion, framework );

      foreach ( var projectFile in projectFiles ) {
        new PackageReferenceAligner( projectFile, targetPackageIndex ).AlignReferences();
      }
    }

    private static void ProcessMerge( Logger logger, DirectoryInfo folder, string rootFolder ) {
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
    }

    private static void ProcessPackagesConfig(
      CurrentOperation operation,
      string[] conflictPaths,
      DirectoryInfo folder,
      Logger logger,
      string rootFolder ) {

      var packagesConfigMerger = new PackagesConfigMerger(
        operation,
        new UserConflictResolver<ConfigitPackageReference>( operation, repositoryRootDirectory: rootFolder ) );

      foreach ( var conflict in conflictPaths.Where( p => Path.GetFileName( p ) == "packages.config" ) ) {

        var fullConflictPath = Path.Combine( folder.FullName, conflict );
        logger.Info( "Examining concurrent modification for " + fullConflictPath );

        var baseContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ancestor, conflict );
        var localContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ours, conflict );
        var incomingContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Theirs, conflict );

        // TODO: Is this correct? if base is not null then we have a deletion of the packages config file
        if ( string.IsNullOrEmpty( localContent ) || string.IsNullOrEmpty( incomingContent ) ) {
          logger.Log( LogLevel.Info, "Skipping '{0}' - no content on one side", conflict );
          continue;
        }

        bool resolved = false;

        try {
          var result = packagesConfigMerger.Merge(
              conflict,
              NuGetExtensions.ReadPackageReferences( baseContent ),
              NuGetExtensions.ReadPackageReferences( localContent ),
              NuGetExtensions.ReadPackageReferences( incomingContent ) ).ToArray();

          result.Write( fullConflictPath );

          using ( var repository = new Repository( rootFolder ) ) {
            repository.Stage( conflict );
            resolved = true;
          }

        }
        catch ( MergeAbortException ) {
          logger.Log( LogLevel.Info, "Package merge aborted for {0}", conflict );
          continue;
        }
        catch ( UserQuitException ) {
          throw;
        }
        catch ( Exception exception ) {
          logger.Log( LogLevel.Error, exception, "Package merge failed for {0}", conflict );
        }

        if ( resolved ) {
          continue;
        }

        string userQuestionText = string.Format( "Could not resolve conflict: {0}{1}Would you like to resolve the conflict with the mergetool?", conflict, Environment.NewLine );
        var userQuestion = new UserQuestion<bool>( userQuestionText, UserQuestion<bool>.YesNoOptions() );

        if ( !userQuestion.Resolve() ) {
          continue;
        }

        XDocument localDocument = XDocument.Parse( localContent );
        XDocument theirDocument = XDocument.Parse( incomingContent );
        XDocument baseDocument = baseContent == null ? new XDocument() : XDocument.Parse( baseContent );

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
        logger.Info( "Examining concurrent modification for " + fullConflictPath );

        var baseContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ancestor, conflict );
        var localContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ours, conflict );
        var incomingContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Theirs, conflict );

        var conflictFolder = Path.GetDirectoryName( Path.Combine( rootFolder, conflict ) );

        if ( conflictFolder == null ) {
          throw new Exception( "No conflict folder" );
        }

        if ( string.IsNullOrEmpty( localContent ) || string.IsNullOrEmpty( incomingContent ) ) {
          logger.Log( LogLevel.Info, "Skipping '{0}' - no content on one side", conflict );
          continue;
        }

        XDocument localDocument = XDocument.Parse( localContent );
        XDocument theirDocument = XDocument.Parse( incomingContent );
        XDocument baseDocument = XDocument.Parse( baseContent );

        var resolved = false;

        try {
          var projectFolder = Path.Combine( folder.FullName, conflictFolder );

          var packagesConfigFilePath = ProjectPackages.GetPackagesConfigFilePath( projectFolder );

          var packagesConfig = ProjectPackages.TryLoadPackagesConfig( packagesConfigFilePath );
          if ( packagesConfig == null ) {
            continue;
          }

          var packageIndex = new ProjectPackages( projectFolder, NuGetExtensions.FindRelativePathOfPackagesFolder( projectFolder ), packagesConfig );

          Item[] items = merger.Merge(
              conflict,
            packageIndex,
            baseDocument,
            localDocument,
              theirDocument ).ToArray();

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
              SerialisationHelper.WriteXml( localDocument, textWriter );
            }
            using ( var repository = new Repository( rootFolder ) ) {
              repository.Stage( conflict );
            }

            resolved = true;
          }

        }
        catch ( FileNotFoundException e ) {
        }
        catch ( MergeAbortException ) {
          logger.Log( LogLevel.Info, "Project merge aborted for {0}", conflict );
          continue;
        }
        catch ( UserQuitException ) {
          throw;
        }
        catch ( Exception exception ) {
          logger.Log( LogLevel.Error, exception, "Project merge failed for {0}", conflict );
        }

        if ( resolved ) {
          continue;
        }

        string userQuestionText = string.Format( "Could not resolve conflict: {0}{1}Would you like to resolve the conflict with the mergetool?", conflict, Environment.NewLine );
        var userQuestion = new UserQuestion<bool>( userQuestionText, UserQuestion<bool>.YesNoOptions() );

        if ( userQuestion.Resolve() ) {

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
}