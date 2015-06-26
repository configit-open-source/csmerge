namespace Cpc.CsMerge.Core {

  public delegate T ConflictResolver<T>(Conflict<T> conflict);

  public interface IConflict<out T> {
    T Base { get; }

    T Local { get; }

    T Incoming { get; }
  }

  public class Conflict<T> : IConflict<T> {
    public Conflict( T @base, T local, T incoming ) {
      Base = @base;
      Local = local;
      Incoming = incoming;
    }

    public T Base {
      get;
      private set;
    }

    public T Local {
      get;
      private set;
    }

    public T Incoming {
      get;
      private set;
    }
  }
}