using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using CsMerge.Core;
using CsMerge.Core.Parsing;
using NLog;

namespace CsMerge {
  public class PackageReferenceAligner {
    public string UpgradePrefix { get; set; }

    public string UpgradeVersion { get; set; }

    public string UpgradeFramework { get; set; }

    private readonly string _baseFolder;

    private readonly string _packagesPrefix;

    private IDictionary<string, Package> _idToNewest;

    private readonly HashSet<string> _nonInstalledPackages = new HashSet<string>();

    public PackageReferenceAligner( string baseFolder, string packagesPrefix, string upgradePrefix = null, string upgradeVersion = null, string upgradeFramework = null ) {
      UpgradePrefix = upgradePrefix;
      UpgradeVersion = upgradeVersion;
      UpgradeFramework = upgradeFramework;
      _baseFolder = baseFolder;
      _packagesPrefix = packagesPrefix;
      IndexNewestPackages();
    }

    public void IndexNewestPackages() {
      _idToNewest = new Dictionary<string, Package>();
      var path = new DirectoryInfo( Path.Combine( _baseFolder, _packagesPrefix ) );

      var groupedPackages = (
        from packageFolder in path.GetDirectories()
        where packageFolder.GetFiles( "*.nupkg" ).Any()
        select ProjectPackages.PackageFromFolderName( packageFolder.Name ) ).GroupBy( p => p.Id );

      foreach ( var group in groupedPackages ) {
        var v = group.Max( g => g.Version );

        var newest = group.First( g => g.Version == v );
        _idToNewest[group.Key] = newest;

        if ( !string.IsNullOrEmpty( UpgradeVersion ) ) {
          var explicitVersion = PackageVersion.Parse( UpgradeVersion );
          if ( group.Key.StartsWith( UpgradePrefix ) && explicitVersion > newest.Version ) {
            _idToNewest[group.Key] = new Package(
              newest.Id, explicitVersion, UpgradeFramework ?? newest.TargetFramework, newest.AllowedVersions, newest.UserInstalled );
            _nonInstalledPackages.Add( group.Key );
          }
        }
      }
    }

    public void AlignReferences( string projectFile ) {
      Logger logger = LogManager.GetCurrentClassLogger();

      var projectFolder = Path.GetDirectoryName( projectFile );
      var packagesRelativePath = CsMerge.FindRelativePathOfPackagesFolder( projectFolder );

      ProjectPackages oldPackagesConfig = new ProjectPackages(
        projectFolder,
        packagesRelativePath );

      var projectPath = projectFile;
      ProjectFile project;

      using ( var fs = new FileStream( projectPath, FileMode.Open ) ) {
        project = CsProjParser.Parse( Path.GetFileName( projectPath ), fs );
      }

      var packagesFolder = Path.GetFullPath( Path.Combine( projectFolder, packagesRelativePath ) );

      List<Package> updatedPackages = new List<Package>();

      bool changed = false;

      foreach ( var package in oldPackagesConfig ) {
        if ( !_idToNewest.ContainsKey( package.Id ) ) {
          logger.Error( "Packages must be restored to align references: No package is installed in "
            + packagesRelativePath + " matching " + package.Id + " as found in " + projectFile +
            ". Perhaps this project is not part of a solution?" );
          return;
        }
        var newestPackage = _idToNewest[package.Id];
        Package newPackage = new Package( package.Id,
  newestPackage.Version,
  package.TargetFramework,
  package.AllowedVersions,
  package.UserInstalled );
        updatedPackages.Add( newPackage );

        if ( newestPackage.Version != package.Version ) {
          changed = true;
          logger.Info( project.Name + ": Upgrading " + package + " to " + newestPackage.Version );

          if ( !_nonInstalledPackages.Contains( newestPackage.Id ) ) {
            continue;
          }

          logger.Info( "Install nuget package " + package.Id + " to " + packagesRelativePath );
          var nugetExe = Path.Combine( _baseFolder, @".nuget\nuget.exe" );
          if ( !File.Exists( nugetExe ) ) {
            throw new Exception( "cannot find " + nugetExe );
          }

          var processStartInfo = new ProcessStartInfo( nugetExe,
            "install " + package.Id + " -Version " + newestPackage.Version );
          processStartInfo.RedirectStandardOutput = true;

          processStartInfo.WorkingDirectory = packagesFolder;
          processStartInfo.UseShellExecute = false;
          processStartInfo.CreateNoWindow = true;

          var process = Process.Start( processStartInfo );
          logger.Info( process.StandardOutput.ReadToEnd() );

          if ( process.ExitCode != 0 ) {
            throw new Exception( "Failed to execute " + processStartInfo.FileName + " " + processStartInfo.Arguments );
          }
          _nonInstalledPackages.Remove( newestPackage.Id );
        }
      }

      if ( !changed ) {
        return;
      }

      var config = Path.Combine( projectFolder, "packages.config" );
      logger.Info( "Writing changes to " + config );
      Package.Write( updatedPackages, config );

      // reparse packages
      ProjectPackages updatedPackagesConfig = new ProjectPackages( projectFolder, packagesRelativePath );

      List<ItemGroup> output = new List<ItemGroup>();

      foreach ( var itemGroup in project.ItemGroups ) {
        List<Item> items = new List<Item>();
        foreach ( var item in itemGroup.Items ) {
          if ( item is Reference ) {
            Reference reference = item as Reference;

            if ( oldPackagesConfig.IsPackageReference( reference ) ) {

              if ( !oldPackagesConfig.IsPackageInstalled( reference ) &&
                   !updatedPackagesConfig.IsPackageInstalled( reference ) ) {
                logger.Info( "Removing " + reference + " as package not listed in packages.config" );
                continue; // remove reference
              }

              var refPackage = oldPackagesConfig.PackageFromHintPath( reference );
              var newestPackage = updatedPackagesConfig[refPackage.Id];
              var oldPackage = oldPackagesConfig[refPackage.Id];

              if ( oldPackage.Version != newestPackage.Version ) {
                var packageFolderName = refPackage.ToPackageFolderName();
                var newPackageFolderName = newestPackage.ToPackageFolderName();

                var newHintPath = reference.HintPath
                  .Replace( packageFolderName, newPackageFolderName )
                  .Replace( oldPackage.TargetFramework, newestPackage.TargetFramework );

                var referencePath = Path.GetFullPath( Path.Combine( projectFolder, newHintPath ) );

                if ( !File.Exists( referencePath ) ) {
                  logger.Warn( "The target framework seems to be incorrect in the old HintPath:" + reference.HintPath );
                  // messed up reference, look in lib folder
                  var libFolder = Path.GetDirectoryName( Path.GetDirectoryName( newHintPath ) );
                  var assemblyFileName = Path.GetFileName( newHintPath );

                  var relativeCandidate = Path.Combine( libFolder, newestPackage.TargetFramework, assemblyFileName );

                  var candidate = Path.GetFullPath( Path.Combine( projectFolder, relativeCandidate ) );

                  if ( File.Exists( candidate ) ) {
                    newHintPath = candidate;
                    referencePath = candidate;
                    logger.Warn( "Found new location: " + reference.HintPath + " -> " + newHintPath );
                  } else {
                    throw new FileLoadException( "Could not find " + assemblyFileName );
                  }
                }

                var updatedName = AssemblyName.GetAssemblyName( referencePath );

                var token = BitConverter.ToString( updatedName.GetPublicKeyToken() ).Replace( "-", "" );

                var includeTokens = new[] {
                  updatedName.Name,
                  "Version=" + updatedName.Version,
                  string.IsNullOrEmpty( updatedName.CultureName ) ? "Culture=neutral" : "Culture=" + updatedName.CultureName,
                  string.IsNullOrEmpty( token ) ? string.Empty : "PublicKeyToken="+token,
                  "processorArchitecture=" + updatedName.ProcessorArchitecture}.Where( s => !string.IsNullOrEmpty( s ) ).ToArray();

                var updatedInclude = string.Join( ", ", includeTokens );

                items.Add( new Reference( updatedInclude, reference.SpecificVersion, reference.Private, newHintPath ) );
                logger.Info( "Updated: " + reference + " to: " + items.Last() );
                continue;
              }
            }
          }
          items.Add( item );
        }
        output.Add( new ItemGroup( items ) );
      }

      XDocument projectXml = XDocument.Load( projectFile );

      ProjectFile.DeleteItems( projectXml );
      ProjectFile.AddItems( projectXml, output.SelectMany( ig => ig.Items ).ToArray() );

      using ( var textWriter = new StreamWriter( projectFile ) ) {
        logger.Info( "Writing " + projectFile );
        projectXml.WriteXml( textWriter );
      }
    }
  }
}