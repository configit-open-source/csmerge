namespace Cpc.CsMerge.Core {
  public abstract class Item {
    public string Action { get { return GetType().Name; } }
    public abstract string Key { get; }
  }
}