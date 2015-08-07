using System;

namespace Integration {

  [Flags]
  public enum MergeType {
    NoChanges = 0,
    LocalDeleted = 1 << 0,
    IncomingDeleted = 1 << 1,
    LocalAdded = 1 << 2,
    IncomingAdded = 1 << 3,
    IncomingModified = 1 << 4,
    LocalModified = 1 << 5
  }
}