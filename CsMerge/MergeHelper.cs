using System.Diagnostics;

using Cpc.CsMerge.Core;

namespace CsMerge {
  public class MergeHelper<T> where T: class {

    public static MergeResult<T> Merge( T b, T m, T t,
      ConflictResolver<T> modDeleteResolver,
      ConflictResolver<T> contentResolver = null ) {
      contentResolver = contentResolver ?? modDeleteResolver;

      if ( b != null && m == null && t == null ) {
        return new MergeResult<T>( MergeType.LocalDeleted | MergeType.TheirsDeleted );
      }

      if ( b != null && m == null && !t.Equals( b ) ) {
        // Mine deleted something modified in theirs
        var resolved = modDeleteResolver( new Conflict<T>( b, m, t ) );
        if ( resolved != null ) {
          return new MergeResult<T>( resolved, MergeType.LocalDeleted | MergeType.TheirsModified );
        }
      }

      if ( b != null && t == null && !m.Equals( b ) ) {
        // Theirs deleted something modified in mine
        var resolved = modDeleteResolver( new Conflict<T>( b, m, t ) );
        if ( resolved != null ) {
          return new MergeResult<T>( resolved, MergeType.LocalModified | MergeType.TheirsDeleted );
        }
      }

      if ( t == null && b != null ) {
        Debug.Assert( m == b );
        return new MergeResult<T>( MergeType.TheirsDeleted );
      }

      if ( m == null && b != null ) {
        Debug.Assert( t == b );
        return new MergeResult<T>( MergeType.LocalDeleted );
      }

      if ( b == t && b == m ) {
        return new MergeResult<T>( MergeType.NoChanges );
      }

      var resolveContent = contentResolver( new Conflict<T>( b, m, t ) );

      return b == null ? new MergeResult<T>( resolveContent, MergeType.LocalAdded | MergeType.TheirsAdded ) :
                         new MergeResult<T>( resolveContent, MergeType.LocalModified | MergeType.TheirsModified, false );
    }
  }
}