namespace Cpc.CsMerge.Core {

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