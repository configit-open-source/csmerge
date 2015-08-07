using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using NLog;

using NuGet.Packaging;
using NuGet.PackagingCore;

namespace Integration {

  /// <summary>
  /// Information about what packages are installed, and the location of the package folder.
  /// </summary>
  public class ProjectPackages : IEnumerable<PackageReference> {
    private readonly string _projectFolder;
    private readonly string _packagesRelativePath;

    private readonly IDictionary<string, PackageReference> _packagesById;

    private readonly string _packagesFolder;

    private readonly Dictionary<string, PackageReference> _packagesByIdentityString;

    public ProjectPackages( IEnumerable<PackageReference> packageReferences, string projectFolder = null, string packagesPrefix = null ) {
      _projectFolder = projectFolder ?? string.Empty;
      _packagesRelativePath = packagesPrefix ?? string.Empty;
      _packagesFolder = Path.GetFullPath( Path.Combine( _projectFolder, _packagesRelativePath ) );

      var references = packageReferences.ToArray();

      _packagesById = references.ToDictionary( p => p.PackageIdentity.Id, p => p );

      _packagesByIdentityString =
        references.ToDictionary( p => p.PackageIdentity.Id + "." + p.PackageIdentity.Version, p => p );
    }

    public ProjectPackages( string projectFolder, string packagesPrefix, XDocument packagesConfig ) : 
      this( new PackagesConfigReader( packagesConfig ).GetPackages(), projectFolder, packagesPrefix ) {
    }

    public ProjectPackages( string projectFolder, string packagesPrefix ) :
      this( new PackagesConfigReader( TryLoadPackagesConfig( GetPackagesConfigFilePath( projectFolder ) ) ).GetPackages(), projectFolder, packagesPrefix ) {
    }

    public static string GetPackagesConfigFilePath( string projectFolder ) {
      return Path.GetFullPath( Path.Combine( projectFolder, "packages.config" ) );
    }

    public bool ReferencePathExists( string path ) {
      return File.Exists( Path.Combine( _projectFolder, path ) );
    }

    public bool IsPackageReference( string path ) {
      // Correctly installed nuget package references have a hintpath
      // pointing to the package folder
      return path != null &&
             path.StartsWith( _packagesRelativePath, StringComparison.InvariantCultureIgnoreCase );
    }

    /// <summary>
    /// Returns true if the reference is a package reference, and
    /// the corresponding package is referenced by packages.config.
    /// </summary>
    public bool IsPackageReferenced( string reference ) {
      if ( !IsPackageReference( reference ) ) {
        return false;
      }

      var packageFolder = GetPackageFolderFromHintPath( reference );
      return _packagesByIdentityString.ContainsKey( packageFolder );
    }

    public PackageIdentity PackageIdentityFromDisk( string referenceHintPath ) {
      var packageFolderName = GetPackageFolderFromHintPath( referenceHintPath );

      var path = Path.GetFullPath( Path.Combine( _packagesFolder, packageFolderName ) );

      return NuGetExtensions.PackageFromNuPkg( path );
    }

    public PackageIdentity PackageIdentityFromHintPath( string referenceHintPath ) {
      string assemblyFolder = Path.GetDirectoryName( referenceHintPath );

      var fullAssemblyPath = Path.GetFullPath( Path.Combine( _projectFolder, assemblyFolder ) );

      string folderName = NuGetExtensions.MakeRelativePath( _packagesFolder, fullAssemblyPath ).Split( Path.DirectorySeparatorChar )[0];

      PackageReference package;
      return _packagesByIdentityString.TryGetValue( folderName, out package ) ? package.PackageIdentity : null;
    }

    private string GetPackageFolderFromHintPath( string hintPath ) {
      string[] directories =
        hintPath.Substring( _packagesRelativePath.Length )
                 .Split( new[] { "" + Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries );

      return directories[0];
    }

    public PackageReference this[string id] {
      get {
        return _packagesById[id];
      }
    }

    public IEnumerator<PackageReference> GetEnumerator() {
      return _packagesById.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    public static XDocument TryLoadPackagesConfig( string projectFolder ) {
      var logger = LogManager.GetCurrentClassLogger();

      string packagesConfigFilePath = GetPackagesConfigFilePath( projectFolder );
      if ( !File.Exists( packagesConfigFilePath ) ) {
        logger.Log( LogLevel.Info, "No packages.config exists at " + packagesConfigFilePath );
        return null;
      }

      try {
        return XDocument.Parse( File.ReadAllText( packagesConfigFilePath ) );
      }
      catch ( Exception e ) {
        logger.Log( LogLevel.Warn, "Failed to parse contents of " + packagesConfigFilePath + " will be skipped" );
        return null;
      }
    }
  }
}
