namespace CsMerge.Core {
  public static class ConflictTypeExtensions {
    public static bool IsSet( this ConflictItemType conflictItemType, ConflictItemType value ) {
      return ( conflictItemType & value ) == value;
    }
  }
}