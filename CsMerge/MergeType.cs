using System;

namespace CsMerge {
  [Flags]
  public enum MergeType {
    NoChanges = 0,
    LocalDeleted = 1 << 0,
    TheirsDeleted = 1 << 1,
    LocalAdded = 1 << 2,
    TheirsAdded = 1 << 3,
    TheirsModified = 1 << 4,
    LocalModified = 1 << 5
  }
}