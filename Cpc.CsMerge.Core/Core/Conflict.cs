namespace Cpc.CsMerge.Core {

  public delegate T ConflictResolver<T>(Conflict<T> conflict);

  public interface IConflict<out T> {
    T Base { get; }

    T Local { get; }

    T Patch { get; }
  }

  public class Conflict<T> : IConflict<T> {
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