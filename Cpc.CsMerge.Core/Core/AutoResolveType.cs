using System;

namespace CsMerge.Core {

  [Flags]
  public enum ConflictItemType {
    Unknown = 0,
    Base = 1,
    Local = 2,
    Incoming = 4,

    All = 7
  }
}
