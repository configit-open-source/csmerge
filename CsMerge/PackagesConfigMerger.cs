using System;
using System.Collections.Generic;
using System.Linq;

using Cpc.CsMerge.Core;

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
      var logger = NLog.LogManager.GetCurrentClassLogger();
      var baseIds = GetIndex( @base );
      var myIds = GetIndex( mine );
      var theirIds = GetIndex( theirs );

      foreach (
        string id in
          baseIds.Keys.Union( myIds.Keys ).Union( theirIds.Keys ).OrderBy( i => i, StringComparer.OrdinalIgnoreCase ) ) {
        var b = baseIds.ContainsKey( id ) ? baseIds[id] : null;
        var m = myIds.ContainsKey( id ) ? myIds[id] : null;
        var t = theirIds.ContainsKey( id ) ? theirIds[id] : null;

        var mergeResult = MergeHelper<Package>.Merge( b, m, t, conflictResolver, c => ResolveContent( c, conflictResolver ) );

        if ( mergeResult.ResolvedItem != null ) {
          logger.Info( "Resolved " + mergeResult.ResolvedItem + " after " + mergeResult.MergeType );
          yield return mergeResult.ResolvedItem;
        }
        else {
          logger.Info( "Removed " + b + " because of " + mergeResult.MergeType );
        }
      }
    }

    private static Package ResolveContent( Conflict<Package> conflict, ConflictResolver<Package> userResolution ) {
      var localNotComparingOnVersion = new Package( conflict.Local.Id, conflict.Patch.Version, conflict.Local.TargetFramework, conflict.Local.AllowedVersions, userInstalled: conflict.Local.UserInstalled );

      var logger = NLog.LogManager.GetCurrentClassLogger();

      if ( localNotComparingOnVersion == conflict.Patch ) {
        var mineHigher = conflict.Local.Version.CompareTo( conflict.Patch.Version ) >= 0;
        var package = ( mineHigher ? conflict.Local : conflict.Patch );
        logger.Info( "Both modified, picking " + package + " over " + ( mineHigher ? conflict.Patch : conflict.Local ) );
        return package;
      }
      var resolved = userResolution( conflict);
      return resolved;
    }
  }
}