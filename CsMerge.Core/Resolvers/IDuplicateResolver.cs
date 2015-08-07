using System.Collections.Generic;

namespace CsMerge.Core.Resolvers {
  public interface IDuplicateResolver<T> {
    T Resolve( Conflict<IEnumerable<T>> conflict );
  }
}
