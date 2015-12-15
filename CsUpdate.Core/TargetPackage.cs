using System.Linq;

using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.PackagingCore;
using NuGet.Versioning;

namespace CsUpdate.Core {
  public class TargetPackage {
    public TargetPackage( string cmdLineString ) {
      string[] splits = cmdLineString.Split( ':' );
      IdFilter = splits[0];
      if ( splits.Length >= 2 ) {
        TargetVersion = NuGetVersion.Parse( splits[1] );
      }
      if ( splits.Length == 3 ) {
        TargetFramework = NuGetFramework.Parse( splits[2] );
      }
    }

    public bool IsPrefix {
      get {
        return IdFilter.Last() == '*';
      }
    }

    public string IdFilter { get; private set; }

    public NuGetVersion TargetVersion { get; private set; }

    public NuGetFramework TargetFramework { get; private set; }

    public PackageReference ReTarget( PackageReference reference ) {
      if ( IsTargeted( reference.PackageIdentity.Id ) ) {
        return reference;
      }

      return new PackageReference(
        new PackageIdentity( reference.PackageIdentity.Id, TargetVersion ?? reference.PackageIdentity.Version ),
        TargetFramework ??
        reference.TargetFramework, reference.IsUserInstalled, reference.IsDevelopmentDependency, reference.RequireReinstallation );
    }

    public bool IsTargeted( string packageId ) {
      if ( string.IsNullOrWhiteSpace( packageId ) ) {
        return false;
      }
      return !IsPrefix ? packageId.Equals( IdFilter ) : packageId.StartsWith( IdFilter );
    }
  }
}