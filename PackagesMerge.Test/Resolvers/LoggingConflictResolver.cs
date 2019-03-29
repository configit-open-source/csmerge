using System.Collections.Generic;
using CsMerge.Core;
using CsMerge.Core.Resolvers;
using Project;

namespace PackagesMerge.Test.Resolvers {

  public class LoggingConflictResolver<T>: IConflictResolver<T> where T : class, IConflictableItem {

    private readonly IConflictResolver<T> _underlyingResolver;

    public LoggingConflictResolver( IConflictResolver<T> underlyingResolver ) {
      _underlyingResolver = underlyingResolver;
      Resolutions = new Dictionary<string, T>();
    }

    public Dictionary<string, T> Resolutions { get; }

    public bool Called { get; set; }

    public MergeResult<T> Resolve( Conflict<T> conflict ) {

      Called = true;

      var result = _underlyingResolver.Resolve( conflict );

      Resolutions.Add( result.Key, result.ResolvedItem );

      return result;
    }
  }
}
