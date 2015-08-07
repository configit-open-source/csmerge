using System.Collections.Generic;
using System.Linq;

namespace Project {

  public static class Extensions {

    public static IEnumerable<T> WhereNotNull<T>( this IEnumerable<T> items ) where T: class {
      return items.Where( i => i != null );
    }

    public static IDictionary<string, T> ToKeyedDictionary<T>( this IEnumerable<T> items ) where T: IConflictableItem {
      return items.ToDictionary( i => i.Key, r => r );
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

    public static bool IsOptionValid<T>( this T item ) where T: IConflictableItem {
      return item == null || item.IsResolveOption;
    }

    public static void AddPropertyIfNotNull( this List<string> propertyNames, object propertyValue, string propertyName = null ) {
      if ( propertyValue == null ) {
        return;
      }

      var text = string.IsNullOrEmpty( propertyName ) ?
        propertyValue.ToString() :
        string.Format( "{0}: {1}", propertyName, propertyValue );

      propertyNames.Add( text );
    }
  }
}
