using System.Collections.Generic;
using System.Linq;

namespace CsMerge.Core {

  public static class Extensions {

    public static IEnumerable<T> WhereNotNull<T>( this IEnumerable<T> items ) where T: class {
      return items.Where( i => i != null );
    }

    public static IDictionary<string, T> ToKeyedDictionary<T>( this IEnumerable<T> items ) where T: IConflictableItem {
      return items.ToDictionary( i => i.Key );
    }

    public static IDictionary<string, IEnumerable<T>> ToDuplicatesDictionary<T>( this IEnumerable<T> items ) where T: IConflictableItem {
      return items
        .GroupBy( i => i.Key )
        .Where( g => g.Count() > 1 )
        .ToDictionary( g => g.Key, g => (IEnumerable<T>) g );
    }

    public static HashSet<T> ToHashSet<T>( this IEnumerable<T> items ) {
      return new HashSet<T>( items );
    }

    public static bool IsNullOrEmpty<T>( this IEnumerable<T> items ) {
      return items == null || !items.Any();
    }

    public static int DistinctCount<T>( this IEnumerable<T> items ) {
      return items == null ? 0 : items.Distinct().Count();
    }

    public static bool IsSet( this ConflictItemType conflictItemType, ConflictItemType value ) {
      return ( conflictItemType & value ) == value;
    }

    public static bool IsOptionValid<T>( this T item ) where T: IConflictableItem {
      return item == null || item.IsResolveOption;
    }

    public static void AddPropertyIfNotNull( this List<string> propertyNames, object propertyValue, string propertyName = null ) {
      if ( propertyValue == null ) {
        return;
      }

      var text = string.IsNullOrEmpty( propertyName ) 
        ? propertyValue.ToString() 
        : $"{propertyName}: {propertyValue}";

      propertyNames.Add( text );
    }

    public static MergeType GetMergeType<T>( this Conflict<T> conflict ) where T: IConflictableItem {
      
      var baseItem = conflict.Base;
      var localItem = conflict.Local;
      var incomingItem = conflict.Incoming;
      
      if ( baseItem == null ) {
        var localChange = localItem == null ? MergeType.NoChanges : MergeType.LocalAdded;
        var incomingChange = incomingItem == null ? MergeType.NoChanges : MergeType.IncomingAdded;
        return localChange | incomingChange;
      } else {
        var localChange = localItem == null ? MergeType.LocalDeleted : Equals( baseItem, localItem ) ? MergeType.NoChanges : MergeType.LocalModified;
        var incomingChange = incomingItem == null ? MergeType.IncomingDeleted : Equals( baseItem, incomingItem ) ? MergeType.NoChanges : MergeType.IncomingModified;
        return localChange | incomingChange;
      }
    }

    public static MergeType GetMergeType<T>( this Conflict<IEnumerable<T>> conflict ) where T : IConflictableItem {

      var baseItems = conflict.Base.OrderBy( i => i.ToString() ).ToList();
      var localItems = conflict.Local.OrderBy( i => i.ToString() ).ToList();
      var incomingItems = conflict.Incoming.OrderBy( i => i.ToString() ).ToList();

      if ( baseItems.IsNullOrEmpty() ) {
        var localChange = localItems.IsNullOrEmpty() ? MergeType.NoChanges : MergeType.LocalAdded;
        var incomingChange = incomingItems.IsNullOrEmpty() ? MergeType.NoChanges : MergeType.IncomingAdded;
        return localChange | incomingChange;
      } else {
        var localChange = localItems.IsNullOrEmpty() ? MergeType.LocalDeleted : baseItems.SequenceEqual( localItems ) ? MergeType.NoChanges : MergeType.LocalModified;
        var incomingChange = incomingItems.IsNullOrEmpty() ? MergeType.IncomingDeleted : baseItems.SequenceEqual( incomingItems ) ? MergeType.NoChanges : MergeType.IncomingModified;
        return localChange | incomingChange;
      }
    }
  }
}
