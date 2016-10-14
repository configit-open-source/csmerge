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

      if ( HandleConfigureOptions( options ) ) {
        return;
      }

      DirectoryInfo folder = new DirectoryInfo( options.InputFolder ?? Directory.GetCurrentDirectory() );
      var logger = LogManager.GetCurrentClassLogger();

      try {
        var rootFolder = GitHelper.FindRepoRoot( folder.FullName );

        ProcessMerge( logger, folder, rootFolder );
      }
      catch ( UserQuitException ) {
        Console.WriteLine( "The user quit." );
      }
      catch ( Exception exception ) {
        Console.WriteLine( $"An error occurred: {Environment.NewLine}{exception}" );
      }
    }

    private static bool HandleConfigureOptions( CsMergeOptions options ) {
      bool configured = false;

      if ( options.ConfigureGitConfig != null ) {
        ConfigurationLevel level;
        if ( Enum.TryParse( options.ConfigureGitConfig, true, out level ) ) {
          GitHelper.ConfigureGitConfig( level );
        }
        else {
          GitHelper.ConfigureGitConfig( file : options.ConfigureGitConfig );
        }
        configured = true;
      }

      if ( options.ConfigureGitAttributes != null ) {
        GitHelper.ConfigureGitAttrib( options.ConfigureGitAttributes );
        configured = true;
      }
      return configured;
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

      ProcessSolutionFiles( operation, conflictPaths, folder, logger, rootFolder );
      ProcessPackagesConfig( operation, conflictPaths, folder, logger, rootFolder );
      ProcessProjectFiles( operation, conflictPaths, folder, logger, rootFolder );
    }

    private static void ProcessSolutionFiles(
      CurrentOperation operation,
      string[] conflictPaths,
      DirectoryInfo folder,
      Logger logger,
      string rootFolder) {
      foreach (var conflictPath in conflictPaths.Where(cp => cp.ToLowerInvariant().EndsWith(".sln")))
      {
        try
        {
          ProcessSolutionFile(operation, folder, logger, rootFolder, conflictPath);
        }
        catch (Exception e)
        {
          logger.Log(LogLevel.Error, e, "Failed to process solution file " + conflictPath);
        }
      }
    }

    private static void ProcessSolutionFile(
      CurrentOperation operation,
      DirectoryInfo folder,
      Logger logger,
      string rootFolder,
      string conflictPath ) {
      var fullConflictPath = Path.Combine( folder.FullName, conflictPath );
      logger.Info( $"{LogHelper.Header}{Environment.NewLine}Examining concurrent modification for {fullConflictPath}" );

      var localName = MergeTypeIntegrationExtensions.Local( operation );

      var basePath = fullConflictPath + "_CSMERGE_BASE";
      var minePath = fullConflictPath + "_CSMERGE_" + localName;
      var incomingPath = fullConflictPath + "_CSMERGE_" + MergeTypeIntegrationExtensions.Incoming( operation );

      File.WriteAllText( basePath, GitHelper.GetConflictContent( rootFolder, StageLevel.Ancestor, conflictPath ) );
      File.WriteAllText(
        minePath,
        GitHelper.GetConflictContent(
          rootFolder,
          localName == MergeTypeIntegrationExtensions.Mine ? StageLevel.Ours : StageLevel.Theirs,
          conflictPath ) );

      File.WriteAllText(
        incomingPath,
        GitHelper.GetConflictContent(
          rootFolder,
          localName == MergeTypeIntegrationExtensions.Mine ? StageLevel.Theirs : StageLevel.Ours,
          conflictPath ) );

      // Use SlnTools
      // "Four solution files should be provided, in order:\n   SourceBranch.sln\n   DestinationBranch.sln\n   CommonAncestror.sln\n   Result.sln"
      CWDev.SLNTools.MergeSolutionsCommand command = new CWDev.SLNTools.MergeSolutionsCommand();
      command.Run(
        new[] { minePath, incomingPath, basePath, fullConflictPath },
        new CWDev.SLNTools.MessageBoxErrorReporter() );

      if ( command.MergedHandled ) {
        using ( var repository = new Repository( rootFolder ) ) {
          repository.Stage( conflictPath );
        }
      }

      File.Delete( basePath );
      File.Delete( minePath );
      File.Delete( incomingPath );
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
        logger.Info( $"{LogHelper.Header}{Environment.NewLine}Examining concurrent modification for {fullConflictPath}" );

        var baseContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ancestor, conflict );
        var localContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Ours, conflict );
        var incomingContent = GitHelper.GetConflictContent( rootFolder, StageLevel.Theirs, conflict );

        // TODO: Is this correct? if base is not null then we have a deletion of the packages config file
        if ( string.IsNullOrEmpty( localContent ) || string.IsNullOrEmpty( incomingContent ) ) {
          logger.Log( LogLevel.Info, $"Skipping '{conflict}' - no content on one side" );
          continue;
        }

        bool resolved = false;

        try {
          var result = packagesConfigMerger.Merge( conflict,
              baseContent == null ? new ConfigitPackageReference[0]: 
                                   NuGetExtensions.ReadPackageReferences( baseContent ),
              NuGetExtensions.ReadPackageReferences( localContent ),
              NuGetExtensions.ReadPackageReferences( incomingContent ) ).ToArray();

          result.Write( fullConflictPath );

          using ( var repository = new Repository( rootFolder ) ) {
            repository.Stage( conflict );
            resolved = true;
          }

        } catch ( MergeAbortException ) {
          logger.Log( LogLevel.Info, $"Package merge aborted for {conflict}" );
          continue;
        }
        catch ( UserQuitException ) {
          throw;
        } catch ( Exception exception ) {
          logger.Log( LogLevel.Error, exception, $"Package merge failed for {conflict}{Environment.NewLine}{exception}" );
        }

        if ( resolved ) {
          continue;
        }

          string userQuestionText = $"Could not resolve conflict: {conflict}{Environment.NewLine}Would you like to resolve the conflict with the mergetool?";
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

      foreach ( var conflict in conflictPaths.Where( p => p.EndsWith( ".csproj" ) || p.EndsWith( ".fsproj" ) || p.EndsWith( ".xproj" )) ) {

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
        var baseDocument = XDocument.Parse( baseContent ?? "<?xml version=\"1.0\" encoding=\"utf - 8\"?><Project/>" );

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
              SerialisationHelper.WriteXml( resolvedDocument, textWriter );
            }
            using ( var repository = new Repository( rootFolder ) ) {
              repository.Stage( conflict );
            }

            resolved = true;
          }
}       catch ( MergeAbortException ) {
          logger.Log( LogLevel.Info, $"Project merge aborted for {conflict}" );
          continue;
        } catch ( UserQuitException ) {
          throw;
        } catch ( Exception exception ) {
          logger.Log( LogLevel.Error, exception, $"Project merge failed for {conflict}{Environment.NewLine}{exception}" );
        }

        if ( resolved ) {
          continue;
        }

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
}
