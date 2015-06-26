using System;
using System.Collections.Generic;
using System.Linq;

using CsMerge.Core;

using LibGit2Sharp;

using NLog;

namespace CsMerge {
  public static class MergeHelper<T> where T: class, IKeyedEntry {

    private static MergeResult<T> Merge(
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
          return new MergeResult<T>( m, MergeType.LocalAdded | MergeType.IncomingAdded );
        }
        if ( t != null && m != null ) {
          return new MergeResult<T>( contentResolver( new Conflict<T>( b, m, t ) ),
            MergeType.LocalAdded | MergeType.IncomingAdded );
        }
        return new MergeResult<T>( t ?? m, t == null ? MergeType.LocalAdded : MergeType.IncomingAdded );
      }

      if ( m == null && t == null ) {
        return new MergeResult<T>( MergeType.LocalDeleted | MergeType.IncomingDeleted );
      }

      if ( m == null ) {
        if ( t.Equals( b ) ) {
          return new MergeResult<T>( MergeType.LocalDeleted );
        }

        // Local deleted something modified in incoming
        var resolved = modDeleteResolver( new Conflict<T>( b, m, t ) );
        return new MergeResult<T>( resolved, MergeType.LocalDeleted | MergeType.IncomingModified );
      }

      if ( t == null ) {
        if ( Equals( m, b ) ) {
          return new MergeResult<T>( MergeType.IncomingDeleted );
        }

        // Incoming deleted something modified in local
        var resolved = modDeleteResolver( new Conflict<T>( b, m, t ) );
        return new MergeResult<T>( resolved, MergeType.LocalModified | MergeType.IncomingDeleted );
      }

      if ( Equals( m, t ) ) {
        return new MergeResult<T>( m, MergeType.LocalModified | MergeType.IncomingModified );
      }

      return new MergeResult<T>(
        contentResolver( new Conflict<T>( b, m, t ) ),
        MergeType.LocalModified | MergeType.IncomingModified );
    }

    private static T HandleMergeResult( MergeResult<T> mergeResult, T @base, CurrentOperation operation ) {
      var logger = LogManager.GetCurrentClassLogger();

      if ( mergeResult.MergeType == MergeType.NoChanges ) {
        return mergeResult.ResolvedItem;
      }

      if ( mergeResult.ResolvedItem != null ) {
        logger.Info( mergeResult.MergeType.ToString( operation ) + " resolved to\n" + mergeResult.ResolvedItem );
      }
      else {
        logger.Info( mergeResult.MergeType.ToString( operation ) + " resolved to delete of\n" + @base );
      }
      return mergeResult.ResolvedItem;
    }

    public static IEnumerable<T> MergeAll(
      CurrentOperation operation,
      IDictionary<string, T> baseObj,
      IDictionary<string, T> localObj,
      IDictionary<string, T> theirObj,
      ConflictResolver<T> delModResolver,
      ConflictResolver<T> contentResolver = null ) {
      return ( from id in baseObj.Keys.Union( localObj.Keys ).Union( theirObj.Keys ).OrderBy( i => i, StringComparer.OrdinalIgnoreCase )
               let b = GetValue( baseObj, id )
               let m = GetValue( localObj, id )
               let t = GetValue( theirObj, id )
               let mergeResult = Merge( b, m, t, delModResolver, contentResolver )
               orderby mergeResult.ResolvedItem.Key
               select HandleMergeResult( mergeResult, b, operation ) into mergedObj
               where mergedObj != null select mergedObj ).OrderBy( o => o.Key );
    }

    private static T GetValue( IDictionary<string, T> baseObj, string id ) {
      return baseObj.ContainsKey( id ) ? baseObj[id] : null;
    }
  }
}