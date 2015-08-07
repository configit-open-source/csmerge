using System.Collections.Generic;
using System.Linq;

using Project;

namespace CsMerge.Core {
  public class KeyHelper {

    public static string GetKey( params IConflictableItem[] items ) {
      return items.First( i => i != null ).Key;
    }

    public static string GetKeyFromCollections( params IEnumerable<IConflictableItem>[] itemCollections ) {
      return GetKey( itemCollections.SelectMany( ic => ic.Select( i => i ) ).ToArray() );
    }
  }
}
