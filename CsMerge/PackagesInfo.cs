using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using Cpc.CsMerge.Core;

namespace CsMerge {

  public class PackagesInfo {
    private readonly string _packagesPrefix;

    private readonly IDictionary<string, Package> _packages;

    public PackagesInfo( string baseFolder, string packagesPrefix ) {
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

      string[] directories = reference.HintPath.Substring( _packagesPrefix.Length ).Split( new[] { "" + Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries );

      string packageFolderName = directories[0];

      int indexOfVersionStart = packageFolderName.ToList().FindIndex( c => { int val; return int.TryParse( c.ToString(), out val ); } );

      string id = packageFolderName.Substring( 0, indexOfVersionStart - 1);

      if ( !_packages.ContainsKey( id ) ) {
        return false;
      }

      Package installedPackage = _packages[id];

      string versionString = packageFolderName.Substring( indexOfVersionStart );

      var version = PackageVersion.Parse( versionString );

      return version == installedPackage.Version;
    }
  }
}