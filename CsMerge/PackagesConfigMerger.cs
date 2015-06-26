using System;
using System.Collections.Generic;
using System.Linq;

using Cpc.CsMerge.Core;

using NLog;

namespace CsMerge {

  /// <summary>
  /// Merges packages.config files.
  /// </summary>
  public class PackagesConfigMerger {

    private static IDictionary<string, Package> GetIndex( IEnumerable<Package> pc ) {
      return pc.ToDictionary( p => p.Id, p => p );
    }

    /// <summary>
    /// Merge prefering newest packages.
    /// </summary>
    public static IEnumerable<Package> Merge(
      IEnumerable<Package> @base,
      IEnumerable<Package> mine,
      IEnumerable<Package> theirs,
      ConflictResolver<Package> conflictResolver ) {
      var baseIds = GetIndex( @base );
      var myIds = GetIndex( mine );
      var theirIds = GetIndex( theirs );

      ConflictResolver<Package> contentResolver = c => ResolveContent( c, conflictResolver );

      return MergeHelper<Package>.MergeAll( baseIds, myIds, theirIds, conflictResolver, contentResolver );
    }

    private static Package ResolveContent( Conflict<Package> conflict, ConflictResolver<Package> userResolution ) {
      var localNotComparingOnVersion = new Package( conflict.Local.Id, conflict.Patch.Version, conflict.Local.TargetFramework, conflict.Local.AllowedVersions, userInstalled : conflict.Local.UserInstalled );

      var logger = LogManager.GetCurrentClassLogger();

      if ( localNotComparingOnVersion == conflict.Patch ) {
        var mineHigher = conflict.Local.Version.CompareTo( conflict.Patch.Version ) >= 0;
        var package = ( mineHigher ? conflict.Local : conflict.Patch );
        logger.Info( "Both modified\n" + conflict.Base + "picking\n" + package + " over\n" + ( mineHigher ? conflict.Patch : conflict.Local ) );
        return package;
      }
      var resolved = userResolution( conflict );
      return resolved;
    }
  }
}