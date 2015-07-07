using System.Collections.Generic;
using System.Linq;

using CsMerge.Core;

using LibGit2Sharp;

using NLog;

namespace CsMerge {

  /// <summary>
  /// Merges packages.config files.
  /// </summary>
  public class PackagesConfigMerger {

    public CurrentOperation Operation { get; set; }

    private IDictionary<string, PackageReference> GetIndex( IEnumerable<PackageReference> pc ) {
      return pc.ToDictionary( p => p.Id, p => p );
    }

    public PackagesConfigMerger( CurrentOperation operation ) {
      Operation = operation;
    }

    /// <summary>
    /// Merge prefering newest packages.
    /// </summary>
    public IEnumerable<PackageReference> Merge(
      IEnumerable<PackageReference> @base,
      IEnumerable<PackageReference> mine,
      IEnumerable<PackageReference> theirs,
      ConflictResolver<PackageReference> conflictResolver ) {
      var baseIds = GetIndex( @base );
      var myIds = GetIndex( mine );
      var theirIds = GetIndex( theirs );

      ConflictResolver<PackageReference> contentResolver = c => ResolveContent( c, conflictResolver );

      return MergeHelper<PackageReference>.MergeAll( Operation, baseIds, myIds, theirIds, conflictResolver, contentResolver );
    }

    private PackageReference ResolveContent( Conflict<PackageReference> conflict, ConflictResolver<PackageReference> userResolution ) {
      var localNotComparingOnVersion = new PackageReference( conflict.Local.Id, conflict.Incoming.Version, conflict.Local.TargetFramework, conflict.Local.AllowedVersions, userInstalled : conflict.Local.UserInstalled );

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