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

    private readonly VersionComparer _versionComparer = new VersionComparer();

    public PackageConflictResolver( IConflictResolver<ConfigitPackageReference> defaultConflictResolver ) {

      if ( defaultConflictResolver == null ) {
        throw new ArgumentNullException( nameof( defaultConflictResolver ) );
      }

      _defaultConflictResolver = defaultConflictResolver;
    }

    public MergeResult<ConfigitPackageReference> Resolve( Conflict<ConfigitPackageReference> conflict ) {

      // If both local and incoming are updated, we may be able to auto resolve to the highest version, but only if there are no other differences.
      var incoming = (PackageReference) conflict.Incoming;

      var local = (PackageReference) conflict.Local;

      if ( local != null && incoming != null ) {

        var localWithIncomingVersion = local.Clone( incoming.PackageIdentity.Version );

        if ( Extensions.Equals(localWithIncomingVersion, incoming ) ) {
          var logger = LogManager.GetCurrentClassLogger();

          var localVersionHigher = _versionComparer.Compare( local.PackageIdentity.Version, incoming.PackageIdentity.Version ) >= 0;

          var resolvedPackage = localVersionHigher ? local : incoming;
          var otherPackage = localVersionHigher ? incoming : local;
          var changeDescription = conflict.Base == null ? "added" : "modified";
          var newLine = Environment.NewLine;
          var resolvedPackageDescription = resolvedPackage.ToString().Replace( newLine, newLine + "  " );
          var otherPackageDescription = otherPackage.ToString().Replace( newLine, newLine + "  " );
          var message = $"{LogHelper.Header}{newLine}Both {changeDescription}: {conflict.Key}{newLine}Picking{newLine}{resolvedPackageDescription}{newLine}over{newLine}{otherPackageDescription}";
          logger.Info( message );

          var mergeType = conflict.Base == null ? MergeType.BothAdded : MergeType.BothModified;
          var resolvedWith = localVersionHigher ? ConflictItemType.Local : ConflictItemType.Incoming;

          return new MergeResult<ConfigitPackageReference>( conflict.Key, resolvedPackage, mergeType, resolvedWith );
        }
      }

      return _defaultConflictResolver.Resolve( conflict );
    }
  }
}
