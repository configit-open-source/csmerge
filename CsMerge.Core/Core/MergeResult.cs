using System.Collections.Generic;
using System.Linq;

using Integration;

using LibGit2Sharp;

using Project;

namespace CsMerge.Core {

  public static class MergeTypeExtensions {

    public static MergeType GetMergeType<T>( this Conflict<IEnumerable<T>> conflict ) where T : IConflictableItem {

      var baseItems = conflict.Base.OrderBy( i => i.ToString() ).ToList();
      var localItems = conflict.Local.OrderBy( i => i.ToString() ).ToList();
      var incomingItems = conflict.Incoming.OrderBy( i => i.ToString() ).ToList();

      if ( baseItems.IsNullOrEmpty() ) {
        var localChange = localItems.IsNullOrEmpty() ? MergeType.NoChanges : MergeType.LocalAdded;
        var incomingChange = incomingItems.IsNullOrEmpty() ? MergeType.NoChanges : MergeType.IncomingAdded;
        return localChange | incomingChange;
      }
      else {
        var localChange = localItems.IsNullOrEmpty() ? MergeType.LocalDeleted : baseItems.SequenceEqual( localItems ) ? MergeType.NoChanges : MergeType.LocalModified;
        var incomingChange = incomingItems.IsNullOrEmpty() ? MergeType.IncomingDeleted : baseItems.SequenceEqual( incomingItems ) ? MergeType.NoChanges : MergeType.IncomingModified;
        return localChange | incomingChange;
      }
    }
    public static MergeType GetMergeType<T>( this Conflict<T> conflict ) where T : IConflictableItem {

      var baseItem = conflict.Base;
      var localItem = conflict.Local;
      var incomingItem = conflict.Incoming;

      if ( baseItem == null ) {
        var localChange = localItem == null ? MergeType.NoChanges : MergeType.LocalAdded;
        var incomingChange = incomingItem == null ? MergeType.NoChanges : MergeType.IncomingAdded;
        return localChange | incomingChange;
      }
      else {
        var localChange = localItem == null ? MergeType.LocalDeleted : Equals( baseItem, localItem ) ? MergeType.NoChanges : MergeType.LocalModified;
        var incomingChange = incomingItem == null ? MergeType.IncomingDeleted : Equals( baseItem, incomingItem ) ? MergeType.NoChanges : MergeType.IncomingModified;
        return localChange | incomingChange;
      }
    }

    public static string ToString( this ConflictItemType conflictItemType, CurrentOperation operation ) {
      switch ( conflictItemType ) {
        case ConflictItemType.Local:
          return Integration.MergeTypeIntegrationExtensions.Local( operation );
        case ConflictItemType.Incoming:
          return Integration.MergeTypeIntegrationExtensions.Incoming( operation );
        case ConflictItemType.Unknown:
          return "Custom";
        default:
          return conflictItemType.ToString();
      }
    }
  }
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