using System;
using NLog;

using NuGet;
using NuGet.Packaging;
using NuGet.Versioning;

using Integration;

using Project;

namespace CsMerge.Core.Resolvers {

  public class PackageConflictResolver: IConflictResolver<ConfigitPackageReference> {

    private readonly IConflictResolver<ConfigitPackageReference> _defaultConflictResolver;

    public PackageConflictResolver( IConflictResolver<ConfigitPackageReference> defaultConflictResolver ) {

      if ( defaultConflictResolver == null ) {
        throw new ArgumentNullException( "defaultConflictResolver" );
      }

      _defaultConflictResolver = defaultConflictResolver;
    }

    public ConfigitPackageReference Resolve( Conflict<ConfigitPackageReference> conflict ) {

      // If both local and incoming are updated, we may be able to auto resolve to the highest version, but only if there are no other differences.
      PackageReference conflictLocal = conflict.Local;
      PackageReference conflictIncoming = conflict.Incoming;

      if ( conflictLocal != null && conflictIncoming != null ) {

        var localWithIncomingVersion = new PackageReference( 
          conflictIncoming.PackageIdentity, 
          conflictLocal.TargetFramework, 
          conflictLocal.IsUserInstalled, 
          conflictLocal.IsDevelopmentDependency, 
          conflictLocal.RequireReinstallation );

        if ( NuGetExtensions.Equals( localWithIncomingVersion, conflictIncoming ) ) {
          var logger = LogManager.GetCurrentClassLogger();

          VersionComparer versionComparer = new VersionComparer();

          var mineHigher = versionComparer.Compare( conflictLocal.PackageIdentity.Version, conflictIncoming.PackageIdentity.Version ) >= 0;

          PackageReference package = mineHigher ? conflictLocal : conflictIncoming;

          logger.Info( "Both modified: " + conflict.Key + "\nPicking\n" + package + " over\n" + ( mineHigher ? conflictIncoming : conflictLocal ) );

          return package;
        }
      }

      return _defaultConflictResolver.Resolve( conflict );
    }
  }
}
