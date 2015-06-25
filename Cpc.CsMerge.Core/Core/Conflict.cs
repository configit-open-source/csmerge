namespace Cpc.CsMerge.Core {

  public delegate T ConflictResolver<T>(Conflict<T> conflict);
 
  public class Conflict<T> {
    public Conflict( T @base, T local, T patch ) {
      Base = @base;
      Local = local;
      Patch = patch;
    }

    public T Base {
      get;
      private set;
    }

    public T Local {
      get;
      private set;
    }

    public T Patch {
      get;
      private set;
    }
  }
}