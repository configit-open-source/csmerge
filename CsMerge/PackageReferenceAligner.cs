using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

using CsMerge.Core;
using CsMerge.Core.Parsing;

using NLog;

using NuGet;

using PackageReference = CsMerge.Core.PackageReference;

namespace CsMerge {
  public class PackageReferenceAligner {
    public string UpgradePrefix { get; set; }

    public string UpgradeVersion { get; set; }

    public string UpgradeFramework { get; set; }

    private readonly string _baseFolder;

    private readonly string _packagesPrefix;

    private IDictionary<string, PackageVersion> _idToNewestVersion;
    private IDictionary<string, string> _idToTargetFramework;

    private readonly HashSet<string> _nonInstalledPackages = new HashSet<string>();

    public PackageReferenceAligner( string baseFolder, string packagesPrefix, string upgradePrefix = null , string upgradeVersion = null, string upgradeFramework = null ) {
      UpgradePrefix = upgradePrefix;
      UpgradeVersion = upgradeVersion;
      UpgradeFramework = upgradeFramework;
      _baseFolder = baseFolder;
      _packagesPrefix = packagesPrefix;
      IndexNewestPackages();
    }

    public void IndexNewestPackages() {
      _idToNewestVersion = new Dictionary<string, PackageVersion>();
      _idToTargetFramework = new Dictionary<string, string>();

      var path = new DirectoryInfo( Path.Combine( _baseFolder, _packagesPrefix ) );

      var groupedPackages = 
        path.GetFiles( "*.nupkg" ).Select( fi => ProjectPackages.PackageFromNuPkg( fi.DirectoryName ) ).GroupBy( p => p.Id );
      
      foreach ( var group in groupedPackages ) {
        var v = group.Max( g => g.Version );

        var newest = group.First( g => g.Version == v );
        _idToNewestVersion[group.Key] = newest.Version;

        if ( !string.IsNullOrEmpty( UpgradeVersion ) ) {
          var explicitVersion = PackageVersion.Parse( UpgradeVersion );
          if ( group.Key.StartsWith( UpgradePrefix ) && explicitVersion > newest.Version ) {
            _idToNewestVersion[group.Key] = explicitVersion;
            _idToTargetFramework[group.Key] = UpgradeFramework;
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

      if ( !UpgradePackagesConfig( projectFile, oldPackagesConfig, packagesRelativePath, project, packagesFolder, projectFolder ) ) {
        return;
      }

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

              Package refPackage = oldPackagesConfig.PackageFromHintPath( reference );
              PackageReference newestPackage = updatedPackagesConfig[refPackage.Id];

              if ( refPackage.Version != newestPackage.Version ) {
                var newPackageFolderName = newestPackage.ToPackageFolderName();

                var relativePackageFolder = Path.Combine( projectFolder, _packagesPrefix, newPackageFolderName );
                var packageFolder = new DirectoryInfo( Path.GetFullPath( relativePackageFolder ) );

                var assemblyName = Path.GetFileName( reference.HintPath );

                bool located = false;

                foreach ( var file in packageFolder.GetFiles( assemblyName ) ) {
                  var newHintPath = ReconstructHintPath( file, newestPackage, reference );

                  if ( newHintPath == null ) {
                    continue;
                  }

                  AssemblyName updatedName = AssemblyName.GetAssemblyName( file.FullName );

                  string updatedInclude = IncludeFromAssemblyName( updatedName );

                  items.Add( new Reference( updatedInclude, reference.SpecificVersion, reference.Private, newHintPath ) );
                  logger.Info( "Updated: " + reference + " to: " + items.Last() );
                  located = true;
                  break;
                }
                if ( located ) {
                  continue;
                }

                logger.Error( "Could not reconstruct hintpath " + reference.HintPath + " original reference will not be changed" );
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
          PackageReference.WriteXml( textWriter, projectXml );
        }
      }
    }

    private static string IncludeFromAssemblyName( AssemblyName updatedName ) {
      var token = BitConverter.ToString( updatedName.GetPublicKeyToken() ).Replace( "-", "" );

      var includeTokens =
        new[] {
          updatedName.Name, "Version=" + updatedName.Version,
          string.IsNullOrEmpty( updatedName.CultureName ) ? "Culture=neutral" : "Culture=" + updatedName.CultureName,
          string.IsNullOrEmpty( token ) ? string.Empty : "PublicKeyToken=" + token,
          "processorArchitecture=" + updatedName.ProcessorArchitecture
        }.Where( s => !string.IsNullOrEmpty( s ) ).ToArray();

      var updatedInclude = string.Join( ", ", includeTokens );
      return updatedInclude;
    }

    private static string ReconstructHintPath(
      FileInfo file,
      PackageReference newestPackage,
      Reference reference ) {

      var parts = file.FullName.Split( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ).ToList();
      int tfIndex = parts.FindIndex( p => p.Equals( newestPackage.TargetFramework, StringComparison.OrdinalIgnoreCase ) );
      if ( tfIndex < 0 ) {
        return null;
      }

      var hintParts = reference.HintPath.Split( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ).ToList();

      List<string> resultPath = new List<string>();

      for ( int i = hintParts.Count - 1; i >= 0; i-- ) {
        if ( hintParts[i].StartsWith( "$" ) ) {
          resultPath.Add( hintParts[i] );
        }
        else if ( parts[i].Equals( newestPackage.TargetFramework ) ) {
          resultPath.Add( parts[i] );
        }
        else {
          resultPath.Add( hintParts[i] );
        }
      }

      resultPath.Reverse();

      return Path.Combine( resultPath.ToArray() );
    }

    private bool UpgradePackagesConfig(
      string projectFile,
      ProjectPackages oldPackagesConfig,
      string packagesRelativePath,
      ProjectFile project,
      string packagesFolder,
      string projectFolder ) {
      List<PackageReference> updatedPackages = new List<PackageReference>();

      bool changed = false;
      var logger = NLog.LogManager.GetCurrentClassLogger();

      foreach ( var package in oldPackagesConfig ) {
        if ( !_idToNewestVersion.ContainsKey( package.Id ) ) {
          logger.Error(
            "Packages must be restored to align references: No package is installed in " + packagesRelativePath
            + " matching " + package.Id + " as found in " + projectFile
            + ". Perhaps this project is not part of a solution?" );
          return true;
        }
        var targetVersion = _idToNewestVersion[package.Id];

        PackageReference newPackage = new PackageReference(
          package.Id,
          targetVersion,
          package.TargetFramework,
          package.AllowedVersions,
          package.UserInstalled );
        updatedPackages.Add( newPackage );

        if ( targetVersion != package.Version ) {
          changed = true;
          logger.Info( project.Name + ": Upgrading " + package + " to " + targetVersion );

          if ( !_nonInstalledPackages.Contains( package.Id ) ) {
            continue;
          }

          logger.Info( "Install nuget package " + package.Id + " to " + packagesRelativePath );
          var nugetExe = Path.Combine( _baseFolder, @".nuget\nuget.exe" );
          if ( !File.Exists( nugetExe ) ) {
            throw new Exception( "cannot find " + nugetExe );
          }

          var processStartInfo = new ProcessStartInfo( nugetExe, "install " + package.Id + " -Version " + targetVersion );
          processStartInfo.RedirectStandardOutput = true;

          processStartInfo.WorkingDirectory = packagesFolder;
          processStartInfo.UseShellExecute = false;
          processStartInfo.CreateNoWindow = true;

          var process = Process.Start( processStartInfo );
          logger.Info( process.StandardOutput.ReadToEnd() );

          if ( process.ExitCode != 0 ) {
            throw new Exception( "Failed to execute " + processStartInfo.FileName + " " + processStartInfo.Arguments );
          }
          _nonInstalledPackages.Remove( package.Id );
        }
      }

      if ( !changed ) {
        return false;
      }

      var config = Path.Combine( projectFolder, "packages.config" );
      logger.Info( "Writing changes to " + config );
      PackageReference.Write( updatedPackages, config );
      return true;
    }
  }
}