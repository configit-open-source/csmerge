using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Cpc.CsMerge.Core;

using NLog;
using NLog.LayoutRenderers.Wrappers;

namespace CsMerge {
  public class MergeHelper<T>
    where T: class {
    public static MergeResult<T> Merge(
      T b,
      T m,
      T t,
      ConflictResolver<T> modDeleteResolver,
      ConflictResolver<T> contentResolver = null ) {
      contentResolver = contentResolver ?? modDeleteResolver;

      if ( Equals( b, t ) && Equals( b, m ) ) {
        return new MergeResult<T>( b, MergeType.NoChanges );
      }

      if ( b == null ) {
        if ( Equals( m, t ) ) {
          return new MergeResult<T>( m, MergeType.LocalAdded | MergeType.TheirsAdded );
        }
        if ( t != null && m != null ) {
          return new MergeResult<T>( contentResolver( new Conflict<T>( b, m, t ) ),
            MergeType.LocalAdded | MergeType.TheirsAdded );
        }
        return new MergeResult<T>( t ?? m, t == null ? MergeType.LocalAdded : MergeType.TheirsAdded );
      }

      if ( m == null && t == null ) {
        return new MergeResult<T>( MergeType.LocalDeleted | MergeType.TheirsDeleted );
      }

      if ( m == null ) {
        if ( !t.Equals( b ) ) {
          // Mine deleted something modified in theirs
          var resolved = modDeleteResolver( new Conflict<T>( b, m, t ) );
          if ( resolved != null ) {
            return new MergeResult<T>( resolved, MergeType.LocalDeleted | MergeType.TheirsModified );
          }
        }

        Debug.Assert( t.Equals( b ) );
        return new MergeResult<T>( MergeType.LocalDeleted );
      }

      if ( t == null ) {
        if ( !Equals( m, b ) ) {
          // Theirs deleted something modified in mine
          var resolved = modDeleteResolver( new Conflict<T>( b, m, t ) );
          if ( resolved != null ) {
            return new MergeResult<T>( resolved, MergeType.LocalModified | MergeType.TheirsDeleted );
          }
        }

        Debug.Assert( m.Equals( b ) );
        return new MergeResult<T>( MergeType.TheirsDeleted );
      }

      if ( Equals( m, t ) ) {
        return new MergeResult<T>( m, MergeType.LocalModified | MergeType.TheirsModified );
      }

      return new MergeResult<T>(
        contentResolver( new Conflict<T>( b, m, t ) ),
        MergeType.LocalModified | MergeType.TheirsModified );
    }

    private static T HandleMergeResult( MergeResult<T> mergeResult, T @base ) {
      var logger = LogManager.GetCurrentClassLogger();

      if ( mergeResult.MergeType == MergeType.NoChanges ) {
        return mergeResult.ResolvedItem;
      }

      if ( mergeResult.ResolvedItem != null ) {
        logger.Info( mergeResult.MergeType + " resolved to\n" + mergeResult.ResolvedItem );
      }
      else {
        logger.Info( mergeResult.MergeType + " resolved to delete of\n" + @base );
      }
      return mergeResult.ResolvedItem;
    }

    public static IEnumerable<T> MergeAll(
      IDictionary<string, T> baseObj,
      IDictionary<string, T> localObj,
      IDictionary<string, T> theirObj,
      ConflictResolver<T> delModResolver,
      ConflictResolver<T> contentResolver = null ) {
      return from id in baseObj.Keys.Union( localObj.Keys ).Union( theirObj.Keys ).OrderBy( i => i, StringComparer.OrdinalIgnoreCase )
             let b = GetValue( baseObj, id )
             let m = GetValue( localObj, id )
             let t = GetValue( theirObj, id )
             let mergeResult = Merge( b, m, t, delModResolver, contentResolver )
             select HandleMergeResult( mergeResult, b ) into mergedObj
             where mergedObj != null select mergedObj;
    }

    private static T GetValue( IDictionary<string, T> baseObj, string id ) {
      return baseObj.ContainsKey( id ) ? baseObj[id] : null;
    }
  }
}