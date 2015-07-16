namespace CsMerge.Core.Resolvers {

  public interface IConflictResolver<T> {

    T Resolve( Conflict<T> conflict );
  }
}
