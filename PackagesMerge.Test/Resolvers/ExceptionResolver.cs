using System;

using CsMerge.Core;
using CsMerge.Core.Resolvers;

namespace PackagesMerge.Test.Resolvers {
  public class ExceptionResolver<T>: IConflictResolver<T> {

    public MergeResult<T> Resolve( Conflict<T> conflict ) {
      throw new Exception( "Resolver was called but shouldn't have been." );
    }
  }
}
