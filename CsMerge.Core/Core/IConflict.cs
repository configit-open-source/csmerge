namespace CsMerge.Core {
  public interface IConflict<out T> {
    T Base { get; }

    T Local { get; }

    T Incoming { get; }
  }
}