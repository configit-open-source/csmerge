namespace CsMerge {
  public class MergeResult<T>
    where T: class {
    public MergeResult( T resolvedItem, MergeType mergeType, bool isResolved = true ) {
      MergeType = mergeType;
      IsResolved = isResolved;
      ResolvedItem = resolvedItem;
    }

    public MergeResult( MergeType mergeType, bool isResolved = true ) {
      MergeType = mergeType;
      IsResolved = isResolved;
    }

    public T ResolvedItem { get; private set; }
    public MergeType MergeType { get; private set; }

    public bool IsResolved { get; set; }
  }
}