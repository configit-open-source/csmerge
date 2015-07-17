using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CsMerge.Core {

  /// <summary>
  /// Information about what packages are installed, and the location of the package folder.
  /// </summary>
  public class ProjectPackages: IEnumerable<Package> {
    private readonly string _packagesPrefix;

    private readonly IDictionary<string, Package> _packages;

    public ProjectPackages( string baseFolder, string packagesPrefix ) {
      _packagesPrefix = packagesPrefix;
      var packages = Package.Read( Path.Combine( baseFolder, "packages.config" ) ).ToArray();

      _packages = packages.ToDictionary( p => p.Id, p => p );
    }

    public bool IsPackageReference( Reference reference ) {
      // Correctly installed nuget package references have a hintpath
      return reference.HintPath != null && reference.HintPath.StartsWith( _packagesPrefix, StringComparison.InvariantCultureIgnoreCase );
    }

    public bool IsPackageInstalled( Reference reference ) {
      if ( !IsPackageReference( reference ) ) {
        return false;
      }

      var referencedPackage = PackageFromHintPath( reference );

      if ( !_packages.ContainsKey( referencedPackage.Id ) ) {
        return false;
      }

      Package installedPackage = _packages[referencedPackage.Id];

      return referencedPackage.Version == installedPackage.Version;
    }

    public Package PackageFromHintPath( Reference reference ) {
      string[] directories =
        reference.HintPath.Substring( _packagesPrefix.Length )
          .Split( new[] { "" + Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries );

      string packageFolderName = directories[0];

      var referencedPackage = PackageFromFolderName( packageFolderName );
      return referencedPackage;
    }

    public static Package PackageFromFolderName( string packageFolderName ) {

      var parts = packageFolderName.Split( '.' ).ToList();

      var versionStartIndex = parts.FindIndex( c => { int val; return int.TryParse( c.ToString(), out val ); } );

      if ( versionStartIndex < 0 ) {
        throw new ArgumentException( "Unknown package folder name format.", packageFolderName );
      }

      var id = string.Join( ".", parts.Take( versionStartIndex ) );
      var versionString = string.Join( ".", parts.Skip( versionStartIndex ) );
      var packageVersion = PackageVersion.Parse( versionString );

      return new Package( id, packageVersion, String.Empty );
    }

    public Package this[string id] {
      get {
        return _packages[id];
      }
    }

    public IEnumerator<Package> GetEnumerator() {
      return _packages.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }
}