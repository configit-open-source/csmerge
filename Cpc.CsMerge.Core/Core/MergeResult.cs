namespace CsMerge.Core {

  public class MergeResult<T> {

    public string Key { get; private set; }

    public MergeResult( string key, T resolvedItem, MergeType mergeType, bool isResolved = true ) {
      MergeType = mergeType;
      IsResolved = isResolved;
      ResolvedItem = resolvedItem;
      Key = key;
    }

    public MergeResult( string key, MergeType mergeType, bool isResolved = true ) {
      MergeType = mergeType;
      IsResolved = isResolved;
      Key = key;
    }

    public T ResolvedItem { get; private set; }
    public MergeType MergeType { get; private set; }

    public bool IsResolved { get; set; }
  }
}