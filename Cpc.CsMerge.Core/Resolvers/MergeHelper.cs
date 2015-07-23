using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using NLog;

namespace CsMerge.Core.Resolvers {

  public class MergeHelper<T> where T: class, IConflictableItem {

    public static MergeResult<T> Resolve( Conflict<T> conflict, IConflictResolver<T> conflictResolver ) {

      var baseItem = conflict.Base;
      var localItem = conflict.Local;
      var incomingItem = conflict.Incoming;

      string key = conflict.Key;

      // No Change
      if ( Equals( baseItem, incomingItem ) && Equals( baseItem, localItem ) ) {
        return new MergeResult<T>( key, baseItem, MergeType.NoChanges );
      }

      // Added on mine, theirs or both
      if ( baseItem == null ) {

        // Added on both but identicle
        if ( Equals( localItem, incomingItem ) ) {
          return new MergeResult<T>( key, localItem, MergeType.LocalAdded | MergeType.IncomingAdded );
        }

        // Added on both but different
        if ( incomingItem != null && localItem != null ) {
          var resolvedItem = conflictResolver.Resolve( conflict );
          return new MergeResult<T>( conflict.Key, resolvedItem, MergeType.LocalAdded | MergeType.IncomingAdded );
        }

        // Added on either mine or theirs
        return new MergeResult<T>( key, incomingItem ?? localItem, incomingItem == null ? MergeType.LocalAdded : MergeType.IncomingAdded );
      }

      // Deleted on mine and theirs
      if ( localItem == null && incomingItem == null ) {
        return new MergeResult<T>( key, MergeType.LocalDeleted | MergeType.IncomingDeleted );
      }

      // Deleted on mine only...
      if ( localItem == null ) {

        // Deleted on mine and unchanged on theirs
        if ( incomingItem.Equals( baseItem ) ) {
          return new MergeResult<T>( key, MergeType.LocalDeleted );
        }

        // Deleted on mine, updated in theirs
        var resolvedItem = conflictResolver.Resolve( conflict );
        return new MergeResult<T>( conflict.Key, resolvedItem, MergeType.LocalDeleted | MergeType.IncomingModified );
      }

      // Deleted on theirs only...
      if ( incomingItem == null ) {

        // Deleted on theirs and unchanged on mine
        if ( Equals( localItem, baseItem ) ) {
          return new MergeResult<T>( key, MergeType.IncomingDeleted );
        }

        // Deleted on theirs, updated in mine
        var resolvedItem = conflictResolver.Resolve( conflict );
        return new MergeResult<T>( conflict.Key, resolvedItem, MergeType.LocalModified | MergeType.IncomingDeleted );
      }

      // Updated to be the same in mine and theirs
      if ( Equals( localItem, incomingItem ) ) {
        return new MergeResult<T>( key, localItem, MergeType.LocalModified | MergeType.IncomingModified );
      }

      // Updated on theirs and unchanged on mine
      if ( Equals( baseItem, localItem ) && !Equals( baseItem, incomingItem ) ) {
        return new MergeResult<T>( key, incomingItem, MergeType.IncomingModified );
      }

      // Updated on mine and unchanged on theirs
      if ( Equals( baseItem, incomingItem ) && !Equals( baseItem, localItem ) ) {
        return new MergeResult<T>( key, localItem, MergeType.LocalModified );
      }

      var validItems = conflict.GetItems().Where( i => i.IsOptionValid() ).ToList();
      var validCount = validItems.Count();

      if ( validCount == 0 ) {
        throw new InvalidResolutonException( key );
      }

      if ( validCount == 1 ) {
        return new MergeResult<T>( conflict.Key, validItems.Single(), MergeType.LocalModified | MergeType.IncomingModified );
      }

      // Updated on both
      var resolved = conflictResolver.Resolve( conflict );
      return new MergeResult<T>( conflict.Key, resolved, MergeType.LocalModified | MergeType.IncomingModified );
    }

    private static MergeResult<T> MergeDuplicates(
      string filePath,
      IEnumerable<T> baseDuplicates,
      IEnumerable<T> localDuplicates,
      IEnumerable<T> incomingDuplicates,
      IConflictResolver<T> conflictResolver,
      IDuplicateResolver<T> duplicateResolver ) {

      var baseDuplicatesList = baseDuplicates.ToList();
      var localDuplicatesList = localDuplicates.ToList();
      var incomingDuplicatesList = incomingDuplicates.ToList();

      string key = KeyHelper.GetKeyFromCollections( baseDuplicatesList, localDuplicatesList, incomingDuplicatesList );

      var distinctBaseItems = baseDuplicatesList.DistinctCount();
      var distinctLocalItems = localDuplicatesList.DistinctCount();
      var distinctIncomingItems = incomingDuplicatesList.DistinctCount();

      // If there are multiple identicle duplicates then we may be able to resolve in the normal way
      if ( distinctBaseItems <= 1 && distinctLocalItems <= 1 && distinctIncomingItems <= 1 ) {
        var conflict = new Conflict<T>(
          filePath,
          key,
          baseDuplicatesList.FirstOrDefault(),
          localDuplicatesList.FirstOrDefault(),
          incomingDuplicatesList.FirstOrDefault() );

        return Resolve( conflict, conflictResolver );
      }

      var resolved = duplicateResolver.Resolve( new Conflict<IEnumerable<T>>( filePath, key, baseDuplicatesList, localDuplicatesList, incomingDuplicatesList ) );
      return new MergeResult<T>( key, resolved, MergeType.LocalModified | MergeType.IncomingModified );
    }

    private static T HandleMergeResult( MergeResult<T> mergeResult, string key, CurrentOperation operation ) {
      var logger = LogManager.GetCurrentClassLogger();

      if ( mergeResult.MergeType == MergeType.NoChanges ) {
        return mergeResult.ResolvedItem;
      }

      if ( mergeResult.ResolvedItem != null ) {
        if ( !mergeResult.ResolvedItem.IsOptionValid() ) {
          throw new InvalidResolutonException( key );
        }
        logger.Info( mergeResult.MergeType.ToString( operation ) + " resolved to\n" + mergeResult.ResolvedItem );
      } else {
        logger.Info( mergeResult.MergeType.ToString( operation ) + " resolved to delete of\n" + key );
      }
      return mergeResult.ResolvedItem;
    }

    public static IEnumerable<T> MergeAll(
      string filePath,
      CurrentOperation operation,
      IDictionary<string, T> baseObj,
      IDictionary<string, T> localObj,
      IDictionary<string, T> incomingObj,
      IConflictResolver<T> conflictResolver ) {

      return ( from id in GetKeys( baseObj.Keys, localObj.Keys, incomingObj.Keys )
               let conflict = new Conflict<T>( filePath, id, GetValue( baseObj, id ), GetValue( localObj, id ), GetValue( incomingObj, id ) )
               let mergeResult = Resolve( conflict, conflictResolver )
               orderby mergeResult.Key
               select HandleMergeResult( mergeResult, id, operation ) into mergedObj
               where mergedObj != null
               select mergedObj ).OrderBy( o => o.Key );
    }

    public static IEnumerable<T> MergeAllDuplicates(
      string filePath,
      CurrentOperation operation,
      IDictionary<string, IEnumerable<T>> baseObj,
      IDictionary<string, IEnumerable<T>> localObj,
      IDictionary<string, IEnumerable<T>> incomingObj,
      IConflictResolver<T> conflictResolver,
      IDuplicateResolver<T> duplicateResolver ) {

      return ( from id in GetKeys( baseObj.Keys, localObj.Keys, incomingObj.Keys )
               let b = GetDuplicates( baseObj, id )
               let m = GetDuplicates( localObj, id )
               let t = GetDuplicates( incomingObj, id )
               let mergeResult = MergeDuplicates( filePath, b, m, t, conflictResolver, duplicateResolver )
               orderby mergeResult.Key
               select HandleMergeResult( mergeResult, id, operation ) into mergedObj
               where mergedObj != null
               select mergedObj ).OrderBy( o => o.Key );
    }

    private static IEnumerable<string> GetKeys( IEnumerable<string> baseItems, IEnumerable<string> localItems, IEnumerable<string> incomingItems ) {
      return baseItems.Union( localItems ).Union( incomingItems ).OrderBy( i => i, StringComparer.OrdinalIgnoreCase );
    }

    private static T GetValue( IDictionary<string, T> baseObj, string id ) {
      return baseObj.ContainsKey( id ) ? baseObj[id] : null;
    }

    private static IEnumerable<T> GetDuplicates( IDictionary<string, IEnumerable<T>> duplicateDictionary, string id ) {
      return duplicateDictionary.ContainsKey( id ) ? duplicateDictionary[id] : null;
    }
  }
}
