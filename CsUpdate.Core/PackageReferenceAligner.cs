using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using CsMerge.Core;
using CsMerge.Core.Parsing;

using CsUpdate.Core;

using NLog;

using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.PackagingCore;
using NuGet.Versioning;

using NuGetHelpers;

using Project;

namespace CsMerge {
  public class PackageReferenceAligner {
    private readonly string _projectFile;

    private readonly TargetPackageIndex _packageIndex;


    public PackageReferenceAligner( string projectFile, TargetPackageIndex packageIndex ) {
      _projectFile = projectFile;
      _packageIndex = packageIndex;
    }

    public void AlignReferences() {
      Logger logger = LogManager.GetCurrentClassLogger();

      var projectFolder = Path.GetDirectoryName( _projectFile );
      var packagesRelativePath = NuGetExtensions.FindRelativePathOfPackagesFolder( projectFolder );

      var oldXml = ProjectPackages.TryLoadPackagesConfig( projectFolder );
      if ( oldXml == null ) {
        return;
      }

      ProjectPackages oldPackagesConfig = new ProjectPackages( projectFolder, packagesRelativePath, oldXml );

      var projectPath = _projectFile;
      ProjectFile project;

      using ( var fs = new FileStream( projectPath, FileMode.Open ) ) {
        project = CsProjParser.Parse( Path.GetFileName( projectPath ), fs );
      }

      if ( !UpgradePackagesConfig( oldPackagesConfig, project ) ) {
        return;
      }

      var newXml = ProjectPackages.TryLoadPackagesConfig( projectFolder );
      if ( newXml == null ) {
        return;
      }

      // reparse packages
      ProjectPackages updatedPackagesConfig = new ProjectPackages( projectFolder, packagesRelativePath, newXml );

      List<ItemGroup> output = new List<ItemGroup>();

      bool changed = false;

      foreach ( var itemGroup in project.ItemGroups ) {
        List<Item> items = new List<Item>();
        foreach ( var item in itemGroup.Items ) {
          if ( !( item is Reference ) ) {
            items.Add( item );
            continue;
          }
          Reference reference = item as Reference;

          if ( !oldPackagesConfig.IsPackageReference( reference.HintPath ) ) {
            items.Add( item ); // we keep any non package references (ie System.Xml)
            continue;
          }

          var referencedInOldPackage = oldPackagesConfig.IsPackageReferenced( reference.HintPath );

          if ( !referencedInOldPackage
               && !updatedPackagesConfig.IsPackageReferenced( reference.HintPath ) ) {
            logger.Info( "Removing " + reference + " as package not listed in packages.config" );
            changed = true;
            continue; // remove reference
          }

          PackageIdentity refPackage = oldPackagesConfig.PackageIdentityFromHintPath( reference.HintPath );

          PackageReference newestPackage = updatedPackagesConfig[refPackage.Id];

          if ( !refPackage.Equals( newestPackage.PackageIdentity ) ) {
            if ( new VersionComparer( VersionComparison.Default ).Compare( refPackage.Version, newestPackage.PackageIdentity.Version ) > 0 ) {
              logger.Warn( "The installed version "
                + newestPackage.PackageIdentity.Version
                + "(that we are aligning to) of " + refPackage.Id + " is lower than the referenced package version " + refPackage.Version );
            }
            var updatedReference = UpdateReference( projectFolder, packagesRelativePath, refPackage, newestPackage, reference );
            if ( updatedReference != null ) {
              reference = updatedReference;
              changed = true;
            }
            else {
              logger.Error( "Could not reconstruct hintpath " + reference.HintPath + " original reference will not be changed" );
            }
          }

          items.Add( reference );
        }
        output.Add( new ItemGroup( items ) );
      }

      if ( !changed ) {
        logger.Info( "No changes made to " + _projectFile );
        return;
      }

      XDocument projectXml = XDocument.Load( _projectFile );

      ProjectFile.DeleteItems( projectXml );
      ProjectFile.AddItems( projectXml, output.SelectMany( ig => ig.Items ).ToArray() );

      using ( var textWriter = new StreamWriter( _projectFile ) ) {
        logger.Info( "Writing " + _projectFile );
        projectXml.WriteXml( textWriter );
      }
    }

    private Reference UpdateReference( string projectFolder, string relativePackageFolder, 
                                       PackageIdentity oldPackage, PackageReference newestPackage, Reference reference ) {
      var packageFolder = new DirectoryInfo(
        Path.GetFullPath( Path.Combine( projectFolder, relativePackageFolder, newestPackage.PackageIdentity.ToFolderName() ) ) );

      if ( !packageFolder.Exists ) {
        throw new Exception( "The package " + newestPackage.PackageIdentity + " failed to install?" );
      }
      var assemblyName = Path.GetFileName( reference.HintPath );

      Tuple<NuGetFramework, Reference> bestUpdate = null;

      foreach ( var file in packageFolder.GetFiles( assemblyName, SearchOption.AllDirectories ) ) {
        var updatedReference = TryUpdateReference( file.FullName, oldPackage, newestPackage, reference );

        if ( updatedReference == null ) {
          continue;
        }

        if ( updatedReference.Item1 == null ) {
          // If we don´t know the target framework we cannot pick the best
          // TODO: Get from assembly file itself?
          bestUpdate = updatedReference;
          break;
        }

        if ( bestUpdate == null || updatedReference.Item1.Equals( newestPackage.TargetFramework ) ) {
          bestUpdate = updatedReference;
        }
        else if ( StringComparer.OrdinalIgnoreCase.Compare(
          bestUpdate.Item1.GetShortFolderName(),
          newestPackage.TargetFramework.GetShortFolderName() ) <= 0 ) {
          bestUpdate = updatedReference;
        }
      }
      if ( bestUpdate != null ) {
        LogManager.GetCurrentClassLogger().Info( "Updated: " + reference + " to: " + bestUpdate.Item2 );
        return bestUpdate.Item2;
      }
      return null;
    }

    public class FrameworkedHintPath {
      public string HintPath { get; set; }
      public NuGetFramework Framework { get; set; }
    }

    public static Tuple<NuGetFramework, Reference> TryUpdateReference(
      string newFile,
      PackageIdentity oldPackageIdentity,
      PackageReference newestPackage,
      Reference reference, Func<string, AssemblyName> assemblyNameResolve = null ) {

      assemblyNameResolve = assemblyNameResolve ?? AssemblyName.GetAssemblyName;

      var newHintPath = ReconstructHintPath( newFile, oldPackageIdentity, newestPackage, reference );

      if ( newHintPath == null ) {
        return null;
      }

      AssemblyName updatedName = assemblyNameResolve( newFile );

      string updatedInclude = IncludeFromAssemblyName( updatedName );

      return new Tuple<NuGetFramework, Reference>(
        newHintPath.Framework,
        new Reference( updatedInclude, reference.SpecificVersion, reference.Private, newHintPath.HintPath ) );
    }

    private static string IncludeFromAssemblyName( AssemblyName updatedName ) {
      // TODO: The VS nuget extension must contain code to do this?
      var token = BitConverter.ToString( updatedName.GetPublicKeyToken() ?? new byte[0] ).Replace( "-", "" );

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

    public static FrameworkedHintPath ReconstructHintPath(
      string assemblyFilePath,
      PackageIdentity oldPackageIdentity,
      PackageReference newestPackage,
      Reference reference ) {

      var parts = assemblyFilePath.Split( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ).ToList();

      var newFramework = parts.Select( NuGetFramework.Parse ).FirstOrDefault( f => !f.IsUnsupported );

      var hintParts = reference.HintPath.Split( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ).ToList();

      var oldFramework = hintParts.Select( NuGetFramework.Parse ).FirstOrDefault( f => !f.IsUnsupported );

      List<string> resultPath = new List<string>();

      for ( int i = hintParts.Count - 1; i >= 0; i-- ) {
        if ( hintParts[i].StartsWith( "$" ) ) {
          resultPath.Add( hintParts[i] );
        }
        else if ( hintParts[i].Equals( oldPackageIdentity.ToFolderName() ) ) {
          resultPath.Add( newestPackage.ToFolderName() );
        }
        else if ( oldFramework != null && newFramework != null && hintParts[i].Equals( oldFramework.GetShortFolderName(), StringComparison.OrdinalIgnoreCase ) ) {
          resultPath.Add( newFramework.GetShortFolderName() );
        }
        else {
          resultPath.Add( hintParts[i] );
        }
      }

      resultPath.Reverse();

      return new FrameworkedHintPath {
        Framework = newFramework,
        HintPath = Path.Combine( resultPath.ToArray() )
      };
    }

    private bool UpgradePackagesConfig(
      ProjectPackages oldPackagesConfig,
      ProjectFile project ) {
      List<PackageReference> updatedPackages = new List<PackageReference>();

      bool changed = false;
      var logger = LogManager.GetCurrentClassLogger();

      foreach ( var package in oldPackagesConfig ) {
        var targetVersion = _packageIndex.GetTargetVersionOf( package.PackageIdentity.Id );

        PackageReference newPackage = new PackageReference(
          new PackageIdentity( package.PackageIdentity.Id, targetVersion ),
          package.TargetFramework,
          package.IsUserInstalled,
          package.IsDevelopmentDependency,
          package.RequireReinstallation,
          package.AllowedVersions );

        updatedPackages.Add( newPackage );

        if ( targetVersion.Equals( package.PackageIdentity.Version ) ) {
          continue;
        }

        changed = true;
        logger.Info( project.Name +
          ": Changing " + package.PackageIdentity.Id +
          " version " + package.PackageIdentity.Version + " to " + targetVersion );

        NuGetExtensions.InstallPackage( Path.GetDirectoryName( _projectFile ),  newPackage );
      }

      if ( !changed ) {
        return false;
      }

      var config = Path.Combine( Path.GetDirectoryName( _projectFile ), "packages.config" );
      logger.Info( "Writing changes to " + config );
      updatedPackages.Write( config );
      return true;
    }
  }
}
