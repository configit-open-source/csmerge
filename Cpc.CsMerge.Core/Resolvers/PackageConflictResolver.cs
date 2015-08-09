using System;
using NLog;

namespace CsMerge.Core.Resolvers {

  public class PackageConflictResolver: IConflictResolver<Package> {

    private readonly IConflictResolver<Package> _defaultConflictResolver;

    public PackageConflictResolver( IConflictResolver<Package> defaultConflictResolver ) {

      if ( defaultConflictResolver == null ) {
        throw new ArgumentNullException( nameof( defaultConflictResolver ) );
      }

      _defaultConflictResolver = defaultConflictResolver;
    }

    public MergeResult<Package> Resolve( Conflict<Package> conflict ) {

      // If both local and incoming are updated, we may be able to auto resolve to the highest version, but only if there are no other differences.
      if ( conflict.Local != null && conflict.Incoming != null ) {

        var localWithIncomingVersion = conflict.Local.Clone();
        localWithIncomingVersion.Version = conflict.Incoming.Version;

        if ( localWithIncomingVersion == conflict.Incoming ) {

          var logger = LogManager.GetCurrentClassLogger();

          var localVersionHigher = conflict.Local.Version.CompareTo( conflict.Incoming.Version ) >= 0;

          var resolvedPackage = localVersionHigher ? conflict.Local : conflict.Incoming;
          var otherPackage = localVersionHigher ? conflict.Incoming : conflict.Local;
          var changeDescription = conflict.Base == null ? "added" : "modified";
          var newLine = Environment.NewLine;
          var resolvedPackageDescription = resolvedPackage.ToString().Replace( newLine, newLine + "  " );
          var otherPackageDescription = otherPackage.ToString().Replace( newLine, newLine + "  " );
          var message = $"{LogHelper.Header}{newLine}Both {changeDescription}: {conflict.Key}{newLine}Picking{newLine}{resolvedPackageDescription}{newLine}over{newLine}{otherPackageDescription}";
          logger.Info( message );

          var mergeType = conflict.Base == null ? MergeType.BothAdded : MergeType.BothModified;
          var resolvedWith = localVersionHigher ? ConflictItemType.Local : ConflictItemType.Incoming;

          return new MergeResult<Package>( conflict.Key, resolvedPackage, mergeType, resolvedWith );
        }
      }

      return _defaultConflictResolver.Resolve( conflict );
    }
  }
}
