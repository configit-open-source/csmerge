using System.Collections.Generic;
using System.Linq;
using CsMerge.Core.Resolvers;
using LibGit2Sharp;

using NuGet;

using Project;

namespace CsMerge.Core {

  /// <summary>
  /// Merges packages.config files.
  /// </summary>
  public class PackagesConfigMerger {

    private readonly CurrentOperation _operation;
    private readonly IConflictResolver<ConfigitPackageReference> _packageConflictResolver;

    /// <summary>
    /// Creates a new instance of a PackagesConfigMerger object
    /// </summary>
    /// <param name="operation">The operation</param>
    /// <param name="userConflictResolver">An conflict resolver that will handle conflicts that cannot be auto resolved by asking the user.</param>
    public PackagesConfigMerger( CurrentOperation operation, IConflictResolver<ConfigitPackageReference> userConflictResolver ) {
      _operation = operation;
      _packageConflictResolver = new PackageConflictResolver( userConflictResolver );
    }

    /// <summary>
    /// Merge prefering newest packages.
    /// </summary>
    public IEnumerable<ConfigitPackageReference> Merge(
      string filePath,
      IEnumerable<ConfigitPackageReference> @base,
      IEnumerable<ConfigitPackageReference> mine,
      IEnumerable<ConfigitPackageReference> theirs ) {

      var baseIds = GetIndex( @base );
      var myIds = GetIndex( mine );
      var theirIds = GetIndex( theirs );

      return MergeHelper<ConfigitPackageReference>.MergeAll( filePath, _operation, baseIds, myIds, theirIds, _packageConflictResolver );
    }

    private IDictionary<string, ConfigitPackageReference> GetIndex( IEnumerable<ConfigitPackageReference> pc ) {
      return pc.ToDictionary( p => p.Key, p => p );
    }

  }
}