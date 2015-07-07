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

    private readonly string _baseFolder;

    private readonly string _packagesPrefix;

    private IDictionary<string, Package> _idToNewest;

    public PackageReferenceAligner( string baseFolder, string packagesPrefix, string upgradePrefix = null , string upgradeVersion = null ) {
      UpgradePrefix = upgradePrefix;
      UpgradeVersion = upgradeVersion;
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

        if ( group.Key.StartsWith( UpgradePrefix ) ) {
          _idToNewest[group.Key] = new Package(
            newest.Id, PackageVersion.Parse( UpgradeVersion ), newest.TargetFramework, newest.AllowedVersions, newest.UserInstalled );
        }
      }
    }

    public void AlignReferences( string projectFile ) {
      Logger logger = LogManager.GetCurrentClassLogger();

      var projectFolder = Path.GetDirectoryName( projectFile );
      var packagesRelativePath = CsMerge.FindRelativePathOfPackagesFolder( projectFolder );

      ProjectPackages info = new ProjectPackages(
        projectFolder,
        packagesRelativePath );

      var projectPath = projectFile;
      ProjectFile project;

      using ( var fs = new FileStream( projectPath, FileMode.Open ) ) {
        project = CsProjParser.Parse( Path.GetFileName( projectPath ), fs );
      }

      List<Package> updatedPackages = new List<Package>();

      bool changed = false;

      foreach ( var package in info ) {
        if ( !_idToNewest.ContainsKey( package.Id ) ) {
          logger.Error( "Packages must be restored to align references: No package is installed in "
            + packagesRelativePath + " matching " + package.Id );
          throw new InvalidOperationException();
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

          var nugetExe = Path.Combine( _baseFolder, @".nuget\nuget.exe" );
          if ( !File.Exists( nugetExe ) ) {
            throw new Exception( "cannot find " + nugetExe );
          }

          var processStartInfo = new ProcessStartInfo( nugetExe,
            "install " + package.Id + " -Version " + newestPackage.Version );
          processStartInfo.RedirectStandardOutput = true;
          processStartInfo.WorkingDirectory = Path.Combine( projectFolder, packagesRelativePath );
          processStartInfo.UseShellExecute = false;
          processStartInfo.CreateNoWindow = true;

          var process = Process.Start( processStartInfo );
          logger.Info( process.StandardOutput.ReadToEnd() );

          if ( process.ExitCode != 0 ) {
            throw new Exception( "Failed to execute " + processStartInfo.FileName + " " + processStartInfo.Arguments );
          }
        }
      }

      if ( !changed ) {
        return;
      }

      var config = Path.Combine( projectFolder, "packages.config" );
      logger.Info( "Writing changes to " + config );
      Package.Write( updatedPackages, config );

      List<ItemGroup> output = new List<ItemGroup>();

      foreach ( var itemGroup in project.ItemGroups ) {
        List<Item> items = new List<Item>();
        foreach ( var item in itemGroup.Items ) {
          if ( item is Reference ) {
            Reference reference = item as Reference;

            if ( info.IsPackageReference( reference ) ) {
              var refPackage = info.PackageFromHintPath( reference );

              if ( !_idToNewest.ContainsKey( refPackage.Id ) ) {
                logger.Info( "Removing " + reference + " as package not installed" );
                continue; // remove reference
              }

              var newestPackage = _idToNewest[refPackage.Id];

              if ( refPackage.Version != newestPackage.Version ) {
                var packageFolderName = refPackage.ToPackageFolderName();
                var newPackageFolderName = newestPackage.ToPackageFolderName();

                var newHintPath = reference.HintPath.Replace( packageFolderName, newPackageFolderName );

                var updatedName = AssemblyName.GetAssemblyName( Path.Combine( _baseFolder, newHintPath ) );

                items.Add( new Reference( updatedName.ToString(), reference.SpecificVersion, reference.Private, newHintPath ) );
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
        Package.WriteXml( textWriter, projectXml );
      }
    }
  }
}