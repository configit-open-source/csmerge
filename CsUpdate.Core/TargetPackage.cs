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
      IsPrefix = IdFilter.Last() == '*';
      if ( IsPrefix ) {
        IdFilter = IdFilter.Substring( 0, IdFilter.Length - 1 );
      }

      if ( splits.Length >= 2 ) {
        TargetVersion = NuGetVersion.Parse( splits[1] );
      }
      if ( splits.Length == 3 ) {
        TargetFramework = NuGetFramework.Parse( splits[2] );
      }
    }

    public bool IsPrefix {
      get;
    }

    public string IdFilter { get; }

    public NuGetVersion TargetVersion { get; }

    public NuGetFramework TargetFramework { get; }

    public PackageReference ReTarget( PackageReference reference ) {
      if ( !IsTargeted( reference.PackageIdentity.Id ) ) {
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
      return IsPrefix ? packageId.StartsWith( IdFilter.Replace( "*", "" ) ) : packageId.Equals( IdFilter );
    }
  }
}