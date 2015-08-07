using System;

namespace CsMerge.Core {

  [Flags]
  public enum ConflictItemType {
    Base = 1,
    Local = 2,
    Incoming = 4,

    All = 7
  }
  public static class ConflictItemTypeExtensions {
    public static bool IsSet( this ConflictItemType conflictItemType, ConflictItemType value ) {
      return ( conflictItemType & value ) == value;
    }
  }
}
