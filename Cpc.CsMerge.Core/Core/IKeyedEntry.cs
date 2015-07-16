namespace CsMerge.Core {
  public interface IConflictableItem {
    string Key { get; }
    bool IsResolveOption { get; }
  }
}