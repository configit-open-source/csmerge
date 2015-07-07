using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using CsMerge.Core;

using Ionic.Zip;

namespace CsMerge {

  /// <summary>
  /// Information about what packages are installed, and the location of the package folder.
  /// </summary>
  public class ProjectPackages : IEnumerable<PackageReference> {
    private readonly string _projectFolder;
    private readonly string _packagesPrefix;

    private readonly IDictionary<string, PackageReference> _packages;

    private string _packagesFolder;

    public ProjectPackages( string projectFolder, string packagesPrefix ) {
      _projectFolder = projectFolder;
      _packagesPrefix = packagesPrefix;
      _packagesFolder = Path.Combine( _projectFolder, _packagesPrefix );

      var packages = PackageReference.Read( Path.Combine( projectFolder, "packages.config" ) ).ToArray();

      _packages = packages.ToDictionary( p => p.Id, p => p );
    }

    public bool IsPackageReference( Reference reference ) {
      return reference.HintPath != null && reference.HintPath.StartsWith( _packagesPrefix );
    }

    public bool IsPackageInstalled( Reference reference ) {
      if ( reference.HintPath == null ) {
        return false; // Correctly installed nuget package references have a hintpath
      }

      Debug.Assert( reference.HintPath.StartsWith( _packagesPrefix ) );

      var referencedPackage = PackageFromHintPath( reference );

      if ( !_packages.ContainsKey( referencedPackage.Id ) ) {
        return false;
      }

      PackageReference installedPackage = _packages[referencedPackage.Id];

      return referencedPackage.Version == installedPackage.Version;
    }

    public Package PackageFromHintPath( Reference reference ) {
      string[] directories =
        reference.HintPath.Substring( _packagesPrefix.Length )
          .Split( new[] { "" + Path.DirectorySeparatorChar },
          StringSplitOptions.RemoveEmptyEntries );

      string packageFolderName = directories[0];

      return PackageFromNuPkg( Path.Combine( _packagesFolder, packageFolderName ) );
    }

    public static Package PackageFromNuPkg( string packageFolder ) {
      var zipFile = new ZipFile( new DirectoryInfo( packageFolder ).GetFiles( "*.nupkg" ).Single().FullName );

      var nuspecEntry = zipFile.Entries.Single( e => e.FileName.EndsWith( ".nuspec" ) );

      XDocument xDocument;
      using ( var reader = XmlReader.Create( nuspecEntry.OpenReader() ) ) {
        xDocument = XDocument.Load( reader );
      }

      var root = xDocument.Root;
      if ( root == null ) {
        throw new Exception( "Invalid nuspec file" );
      }

      var ns = root.Name.Namespace;

      var metaData = root.Element( ns.GetName( "package" ) ).Element( "metadata" );
      var id = metaData.Attribute( "id" ).Value;
      var version = metaData.Attribute( "version" ).Value;

      return new Package( id, PackageVersion.Parse( version ) );
    }

    public PackageReference PackageFromFolderName( string packageFolderName ) {

      var packageDirInfo = new DirectoryInfo( Path.Combine( _packagesFolder, packageFolderName) );

      if ( !packageDirInfo.Exists ) {
        return null;
      }

      FileInfo nupkgFileInfo = packageDirInfo.GetFiles( "*.nupkg" ).Single();


      
      int indexOfVersionStart = packageFolderName.ToList().FindIndex(
        c => {
          int val;
          return int.TryParse( c.ToString(), out val );
        } );

      while ( packageFolderName[indexOfVersionStart - 1] != '.' ) {
        indexOfVersionStart++;
      }

      string id = packageFolderName.Substring( 0, indexOfVersionStart - 1 );

      string versionString = packageFolderName.Substring( indexOfVersionStart );

      var referencedPackage = new PackageReference( id, PackageVersion.Parse( versionString ), String.Empty );
      return referencedPackage;
    }

    public PackageReference this[string id] {
      get {
        return _packages[id];
      }
    }

    public IEnumerator<PackageReference> GetEnumerator() {
      return _packages.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }
}