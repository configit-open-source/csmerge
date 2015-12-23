using System.Collections.Generic;
using System.IO;
using System.Linq;

using NuGet.Packaging;
using NuGet.Versioning;

namespace CsUpdate.Core {

  /// <summary>
  /// Provides information about which version is the newest / targetted version for <see cref="PackageReferenceAligner"/>
  /// </summary>
  public class TargetPackageIndex {
    private readonly TargetPackage[] _targets;

    private Dictionary<string, PackageReference> _idToTarget;

    public PackageReference GetTarget( string packageId ) {
      PackageReference result;
      return _idToTarget.TryGetValue( packageId, out result ) ? result : null;
    }

    public TargetPackageIndex( IEnumerable<string> projectFiles, IEnumerable<TargetPackage> targets ) {
      _targets = targets == null ? null : targets.ToArray();

      // TODO: Allow targeting a specific sln file
      var packageReferences = GetAllPackageReferences( projectFiles );

      // TODO: perhaps look at difference in target framework?
      IndexNewestPackages( packageReferences.Select( pr => pr ) );
    }

    private static List<PackageReference> GetAllPackageReferences( IEnumerable<string> projectFiles ) {
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
      return packageReferences;
    }

    private void IndexNewestPackages( IEnumerable<PackageReference> packages ) {
      var packagesById = packages.GroupBy( p => p.PackageIdentity.Id );

      _idToTarget = new Dictionary<string, PackageReference>();

      foreach ( var packagesWithSameId in packagesById ) {
        NuGetVersion v = packagesWithSameId.Max( g => g.PackageIdentity.Version );

        var newest = packagesWithSameId.First( g => g.PackageIdentity.Version == v );
        _idToTarget[packagesWithSameId.Key] = newest;

        if ( _targets == null ) {
          continue;
        }

        foreach ( var target in _targets ) {
          _idToTarget[packagesWithSameId.Key] = target.ReTarget( newest );
        }
      }
    }
  }
}