using Integration;

namespace CsMerge.Core {

  public class MergeResult<T> {

    public string Key { get; private set; }

    public MergeResult( string key, T resolvedItem, MergeType mergeType, ConflictItemType resolvedWith, bool isResolved = true ) {
      MergeType = mergeType;
      IsResolved = isResolved;
      ResolvedItem = resolvedItem;
      Key = key;
      ResolvedWith = resolvedWith;
    }

    public MergeResult( string key, MergeType mergeType, ConflictItemType resolvedWith, bool isResolved = true ) {
      MergeType = mergeType;
      IsResolved = isResolved;
      Key = key;
      ResolvedWith = resolvedWith;
    }

    public T ResolvedItem { get; private set; }

    public MergeType MergeType { get; private set; }

    public ConflictItemType ResolvedWith { get; private set; }

    public bool IsResolved { get; set; }
  }
}