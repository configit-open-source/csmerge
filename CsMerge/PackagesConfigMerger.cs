using System.Collections.Generic;
using System.Linq;

using Cpc.CsMerge.Core;

using LibGit2Sharp;

using NLog;

namespace CsMerge {

  /// <summary>
  /// Merges packages.config files.
  /// </summary>
  public class PackagesConfigMerger {

    public CurrentOperation Operation { get; set; }

    private IDictionary<string, Package> GetIndex( IEnumerable<Package> pc ) {
      return pc.ToDictionary( p => p.Id, p => p );
    }

    public PackagesConfigMerger( CurrentOperation operation ) {
      Operation = operation;
    }

    /// <summary>
    /// Merge prefering newest packages.
    /// </summary>
    public IEnumerable<Package> Merge(
      IEnumerable<Package> @base,
      IEnumerable<Package> mine,
      IEnumerable<Package> theirs,
      ConflictResolver<Package> conflictResolver ) {
      var baseIds = GetIndex( @base );
      var myIds = GetIndex( mine );
      var theirIds = GetIndex( theirs );

      ConflictResolver<Package> contentResolver = c => ResolveContent( c, conflictResolver );

      return MergeHelper<Package>.MergeAll( Operation, baseIds, myIds, theirIds, conflictResolver, contentResolver );
    }

    private Package ResolveContent( Conflict<Package> conflict, ConflictResolver<Package> userResolution ) {
      var localNotComparingOnVersion = new Package( conflict.Local.Id, conflict.Incoming.Version, conflict.Local.TargetFramework, conflict.Local.AllowedVersions, userInstalled : conflict.Local.UserInstalled );

      var logger = LogManager.GetCurrentClassLogger();

      if ( localNotComparingOnVersion == conflict.Incoming ) {
        var mineHigher = conflict.Local.Version.CompareTo( conflict.Incoming.Version ) >= 0;
        var package = ( mineHigher ? conflict.Local : conflict.Incoming );
        logger.Info( "Both modified\n" + conflict.Base + "picking\n" + package + " over\n" + ( mineHigher ? conflict.Incoming : conflict.Local ) );
        return package;
      }
      var resolved = userResolution( conflict );
      return resolved;
    }
  }
}