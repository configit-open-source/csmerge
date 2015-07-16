using System;
using NLog;

namespace CsMerge.Core.Resolvers {

  public class PackageConflictResolver: IConflictResolver<Package> {

    private readonly IConflictResolver<Package> _defaultConflictResolver;

    public PackageConflictResolver( IConflictResolver<Package> defaultConflictResolver ) {

      if ( defaultConflictResolver == null ) {
        throw new ArgumentNullException( "defaultConflictResolver" );
      }

      _defaultConflictResolver = defaultConflictResolver;
    }

    public Package Resolve( Conflict<Package> conflict ) {

      // If both local and incoming are updated, we may be able to auto resolve to the highest version, but only if there are no other differences.
      if ( conflict.Local != null && conflict.Incoming != null ) {

        var localWithIncomingVersion = conflict.Local.Clone();
        localWithIncomingVersion.Version = conflict.Incoming.Version;

        if ( localWithIncomingVersion == conflict.Incoming ) {

          var logger = LogManager.GetCurrentClassLogger();

          var mineHigher = conflict.Local.Version.CompareTo( conflict.Incoming.Version ) >= 0;

          Package package = mineHigher ? conflict.Local : conflict.Incoming;

          logger.Info( "Both modified: " + conflict.Key + "\nPicking\n" + package + " over\n" + ( mineHigher ? conflict.Incoming : conflict.Local ) );

          return package;
        }
      }

      return _defaultConflictResolver.Resolve( conflict );
    }
  }
}
