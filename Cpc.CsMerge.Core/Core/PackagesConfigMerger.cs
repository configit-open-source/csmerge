using System.Collections.Generic;
using System.Linq;
using CsMerge.Core.Resolvers;
using LibGit2Sharp;

namespace CsMerge.Core {

  /// <summary>
  /// Merges packages.config files.
  /// </summary>
  public class PackagesConfigMerger {

    private readonly CurrentOperation _operation;
    private readonly IConflictResolver<Package> _packageConflictResolver;

    /// <summary>
    /// Creates a new instance of a PackagesConfigMerger object
    /// </summary>
    /// <param name="operation">The operation</param>
    /// <param name="userConflictResolver">An conflict resolver that will handle conflicts that cannot be auto resolved by asking the user.</param>
    public PackagesConfigMerger( CurrentOperation operation, IConflictResolver<Package> userConflictResolver ) {
      _operation = operation;
      _packageConflictResolver = new PackageConflictResolver( userConflictResolver );
    }

    /// <summary>
    /// Merge prefering newest packages.
    /// </summary>
    public IEnumerable<Package> Merge(
      string filePath,
      IEnumerable<Package> @base,
      IEnumerable<Package> mine,
      IEnumerable<Package> theirs ) {

      var baseIds = GetIndex( @base );
      var myIds = GetIndex( mine );
      var theirIds = GetIndex( theirs );

      return MergeHelper<Package>.MergeAll( filePath, _operation, baseIds, myIds, theirIds, _packageConflictResolver );
    }

    private IDictionary<string, Package> GetIndex( IEnumerable<Package> pc ) {
      return pc.ToDictionary( p => p.Id, p => p );
    }

  }
}