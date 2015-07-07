using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using CsMerge.Core;

namespace CsMerge {

  /// <summary>
  /// Information about what packages are installed, and the location of the package folder.
  /// </summary>
  public class ProjectPackages : IEnumerable<Package> {
    private readonly string _packagesPrefix;

    private readonly IDictionary<string, Package> _packages;

    public ProjectPackages( string baseFolder, string packagesPrefix ) {
      _packagesPrefix = packagesPrefix;
      var packages = Package.Read( Path.Combine( baseFolder, "packages.config" ) ).ToArray();

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

    private static readonly Regex PackageNameParser = new Regex(
      @"(^(?<id>\w*)\.(?<version>((\d)\.*)+)$)",

      RegexOptions.ExplicitCapture |
      RegexOptions.Compiled |
      RegexOptions.CultureInvariant );

    public static Package PackageFromFolderName( string packageFolderName ) {
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

      var referencedPackage = new Package( id, PackageVersion.Parse( versionString ), String.Empty );
      return referencedPackage;
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