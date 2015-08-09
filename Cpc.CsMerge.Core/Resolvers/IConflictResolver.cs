namespace CsMerge.Core.Resolvers {

  public interface IConflictResolver<T> {

    MergeResult<T> Resolve( Conflict<T> conflict );
  }
}
