using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Project {

  public static class Extensions {

    public static IEnumerable<T> WhereNotNull<T>( this IEnumerable<T> items ) where T: class {
      return items.Where( i => i != null );
    }
    
    public static IEnumerable<string> WhereNotNullOrEmpty( this IEnumerable<string> items ) {
      return items.Where( i => !string.IsNullOrEmpty( i ) );
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

      var text = string.IsNullOrEmpty( propertyName ) 
        ? propertyValue.ToString() 
        : $"{propertyName}: {propertyValue}";

      propertyNames.Add( text );
    }

    public static bool Equals( NuGet.Packaging.PackageReference ref1, NuGet.Packaging.PackageReference ref2 ) {
      var frameworkComparer = new NuGetFrameworkFullComparer();
      var versionRangeComparer = new VersionRangeComparer();

      return ref1.IsDevelopmentDependency == ref2.IsDevelopmentDependency &&
             ref1.RequireReinstallation == ref2.RequireReinstallation &&
             ref1.IsUserInstalled == ref2.IsUserInstalled &&
             frameworkComparer.Equals( ref1.TargetFramework, ref2.TargetFramework ) &&
             ref1.PackageIdentity.Equals( ref2.PackageIdentity ) &&
             versionRangeComparer.Equals( ref1.AllowedVersions, ref2.AllowedVersions );
    }

    public static string ToDelimited( this IEnumerable<string> items, string separator ) {
      return string.Join( separator, items );
    }

    public static bool IsEquivalentTo<T>( this IEnumerable<T> items, IEnumerable<T> other ) {
      var itemsList = items.ToList();
      var otherList = other.ToList();
      return itemsList.Count == otherList.Count && !itemsList.Except( otherList ).Any();
    }
  }
}
