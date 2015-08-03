using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

using NuGet.Packaging;
using NuGet.PackagingCore;
using NuGet.Versioning;

namespace CsMerge {
  /// <summary>
  /// Provides information about which version is the newest / targetted version for <see cref="PackageReferenceAligner"/>
  /// </summary>
  public class TargetPackageIndex {

    private Dictionary<string, NuGetVersion> _idToNewestVersion;
    private IDictionary<string, string> _idToTargetFramework;

    public string TargetPrefix { get; set; }

    public string TargetVersion { get; set; }

    public string TargetFramework { get; set; }

    public NuGetVersion GetTargetVersionOf( string packageId ) {
      NuGetVersion result;
      return _idToNewestVersion.TryGetValue( packageId, out result ) ? result : null;
    }

    public string GetTargetFrameworkFor( string packageId ) {
      string result;
      return _idToTargetFramework.TryGetValue( packageId, out result ) ? result : null;
    }

    public TargetPackageIndex( 
      IEnumerable<string> projectFiles, 
      string targetPrefix = null, 
      string targetVersion = null, 
      string targetFramework = null ) {
      TargetPrefix = targetPrefix;
      TargetVersion = targetVersion;
      TargetFramework = targetFramework;
      List<PackageReference> packageReferences = new List<PackageReference>();

      foreach ( var folder in projectFiles.Select( p => new DirectoryInfo( Path.GetDirectoryName( p ) ) ) ) {
        var packagesConfig = folder.GetFiles( "packages.config" ).SingleOrDefault();
        if ( packagesConfig == null ) {
          continue;
        }
        using ( var fs = new FileStream( packagesConfig.FullName, FileMode.Open ) ) {
          PackagesConfigReader reader = new PackagesConfigReader( fs );
          packageReferences.AddRange( reader.GetPackages() );
        }
      }

      // TODO: perhaps look at difference in target framework?
      IndexNewestPackages( packageReferences.Select( pr => pr.PackageIdentity ) );
    }

    public TargetPackageIndex( IEnumerable<PackageIdentity> identities ) {
      IndexNewestPackages( identities );
    }

    private void IndexNewestPackages( IEnumerable<PackageIdentity> packages ) {
      var packagesById = packages.GroupBy( p => p.Id );

      _idToNewestVersion = new Dictionary<string, NuGetVersion>();
      _idToTargetFramework = new Dictionary<string, string>();

      foreach ( var packagesWithSameId in packagesById ) {
        NuGetVersion v = packagesWithSameId.Max( g => g.Version );

        var newest = packagesWithSameId.First( g => g.Version == v );
        _idToNewestVersion[packagesWithSameId.Key] = newest.Version;

        if ( string.IsNullOrEmpty( TargetVersion ) ) {
          continue;
        }

        var explicitVersion = NuGetVersion.Parse( TargetVersion );
        if ( !packagesWithSameId.Key.StartsWith( TargetPrefix ) || explicitVersion <= newest.Version ) {
          continue;
        }

        _idToNewestVersion[packagesWithSameId.Key] = explicitVersion;
        _idToTargetFramework[packagesWithSameId.Key] = TargetFramework;
      }
    }
  }
}