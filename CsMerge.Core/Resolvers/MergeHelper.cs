using System;
using System.Collections.Generic;
using System.Linq;

using CsMerge.Core.Exceptions;

using LibGit2Sharp;
using NLog;

using Integration;

using Project;

namespace CsMerge.Core.Resolvers {

  public class MergeHelper<T> where T : class, IConflictableItem {

    private static MergeResult<T> Resolve( Conflict<T> conflict, IConflictResolver<T> conflictResolver ) {

      var baseItem = conflict.Base;
      var localItem = conflict.Local;
      var incomingItem = conflict.Incoming;

      string key = conflict.Key;

      // No Change
      if ( Equals( baseItem, incomingItem ) && Equals( baseItem, localItem ) ) {
        return AttemptToResolveWith( conflict, baseItem, MergeType.NoChanges, ConflictItemType.Base, conflictResolver );
      }

      // Added on mine, theirs or both
      if ( baseItem == null ) {

        // Added on both but identicle
        if ( Equals( localItem, incomingItem ) ) {
          return AttemptToResolveWith( conflict, localItem, MergeType.BothAdded, ConflictItemType.Local, conflictResolver );
        }

        // Added on both but different
        if ( incomingItem != null && localItem != null ) {
          return conflictResolver.Resolve( conflict );
        }

        // Added on either mine or theirs
        return AttemptToResolveWith(
          conflict,
          incomingItem ?? localItem,
          incomingItem == null ? MergeType.LocalAdded : MergeType.IncomingAdded,
          incomingItem == null ? ConflictItemType.Local : ConflictItemType.Incoming,
          conflictResolver );
      }

      // Deleted on mine and theirs
      if ( localItem == null && incomingItem == null ) {
        return AttemptToResolveWith( conflict, null, MergeType.BothDeleted, ConflictItemType.Local, conflictResolver );
      }

      // Deleted on mine only...
      if ( localItem == null ) {

        // Deleted on mine and unchanged on theirs
        if ( incomingItem.Equals( baseItem ) ) {
          return AttemptToResolveWith( conflict, null, MergeType.LocalDeleted, ConflictItemType.Local, conflictResolver );
        }

        // Deleted on mine, updated in theirs
        return conflictResolver.Resolve( conflict );
      }

      // Deleted on theirs only...
      if ( incomingItem == null ) {

        // Deleted on theirs and unchanged on mine
        if ( Equals( localItem, baseItem ) ) {
          return AttemptToResolveWith( conflict, null, MergeType.IncomingDeleted, ConflictItemType.Incoming, conflictResolver );
        }

        // Deleted on theirs, updated in mine
        return conflictResolver.Resolve( conflict );
      }

      // Updated to be the same in mine and theirs
      if ( Equals( localItem, incomingItem ) ) {
        return AttemptToResolveWith( conflict, localItem, MergeType.BothModified, ConflictItemType.Local, conflictResolver );
      }

      // Updated on theirs and unchanged on mine
      if ( Equals( baseItem, localItem ) && !Equals( baseItem, incomingItem ) ) {
        return AttemptToResolveWith( conflict, incomingItem, MergeType.IncomingModified, ConflictItemType.Incoming, conflictResolver );
      }

      // Updated on mine and unchanged on theirs
      if ( Equals( baseItem, incomingItem ) && !Equals( baseItem, localItem ) ) {
        return AttemptToResolveWith( conflict, localItem, MergeType.LocalModified, ConflictItemType.Local, conflictResolver );
      }

      // If we got this far, then the conflict involve changes on both local and incoming and they were not the same.

      var validItems = new List<Tuple<ConflictItemType, T>> {
          new Tuple<ConflictItemType, T>( ConflictItemType.Local, conflict.Local),
          new Tuple<ConflictItemType, T>( ConflictItemType.Incoming, conflict.Incoming)
        }.Where( r => r.Item2.IsOptionValid() ).ToList();

      var validCount = validItems.Count();

      if ( validCount == 0 ) {
        throw new InvalidResolutonException( key );
      }

      if ( validCount == 1 ) {
        var resolvedItem = validItems.Single();
        return AttemptToResolveWith( conflict, resolvedItem.Item2, MergeType.BothModified, resolvedItem.Item1, conflictResolver );
      }

      // Updated on both
      return conflictResolver.Resolve( conflict );
    }

    private static MergeResult<T> AttemptToResolveWith( Conflict<T> conflict, T resolveWithItem, MergeType mergeType, ConflictItemType resolvedWith, IConflictResolver<T> conflictResolver ) {
      if ( resolveWithItem.IsOptionValid() ) {
        return new MergeResult<T>( conflict.Key, resolveWithItem, mergeType, resolvedWith );
      }

      return conflictResolver.Resolve( conflict );
    }

    private static T Resolve( ConflictContext<T> context, string key, IConflictResolver<T> conflictResolver ) {

      var conflict = context.CreateItemConflict( key );

      var mergeResult = Resolve( conflict, conflictResolver );

      return HandleMergeResult( mergeResult, context.FilePath, key, context.Operation );
    }

    private static T Resolve( ConflictContext<IEnumerable<T>> context, string key, IConflictResolver<T> conflictResolver, IDuplicateResolver<T> duplicateResolver ) {

      var conflict = context.CreateItemConflict( key );

      var mergeResult = MergeDuplicates( context.FilePath, conflict, conflictResolver, duplicateResolver );

      return HandleMergeResult( mergeResult, context.FilePath, key, context.Operation );
    }

    private static MergeResult<T> MergeDuplicates(
      string filePath,
      IConflict<IEnumerable<T>> conflict,
      IConflictResolver<T> conflictResolver,
      IDuplicateResolver<T> duplicateResolver ) {

      var distinctBaseItems = conflict.Base.Distinct().ToList();
      var distinctLocalItems = conflict.Local.Distinct().ToList();
      var distinctIncomingItems = conflict.Incoming.Distinct().ToList();

      var key = KeyHelper.GetKeyFromCollections( distinctBaseItems, distinctLocalItems, distinctIncomingItems );

      // If there are multiple identicle duplicates then we may be able to resolve in the normal way
      if ( distinctBaseItems.Count <= 1 && distinctLocalItems.Count <= 1 && distinctIncomingItems.Count <= 1 ) {
        var itemConflict = new Conflict<T>(
          filePath,
          key,
          distinctBaseItems.FirstOrDefault(),
          distinctLocalItems.FirstOrDefault(),
          distinctIncomingItems.FirstOrDefault() );

        return Resolve( itemConflict, conflictResolver );
      }

      // If duplicates in base, but deleted on both incoming and local, then auto resolve to deleted.
      if ( distinctIncomingItems.Count == 0 && distinctLocalItems.Count == 0 ) {
        return new MergeResult<T>( key, null, MergeType.BothDeleted, ConflictItemType.Local );
      }

      // If duplicates in base, but modified in both incoming and local, and incoming and local are identical, then auto resolve to to the modified item.
      if ( distinctIncomingItems.Count == 1 && distinctLocalItems.Count == 1 && distinctIncomingItems.First() == distinctLocalItems.First() && distinctLocalItems.First().IsOptionValid() ) {
        var mergeType = distinctBaseItems.Any() ? MergeType.BothModified : MergeType.BothAdded;
        return new MergeResult<T>( key, distinctLocalItems.First(), mergeType, ConflictItemType.Local );
      }

      return duplicateResolver.Resolve( new Conflict<IEnumerable<T>>( filePath, key, distinctBaseItems, distinctLocalItems, distinctIncomingItems ) );
    }

    private static T HandleMergeResult( MergeResult<T> mergeResult, string filePath, string key, CurrentOperation operation ) {
      var logger = LogManager.GetCurrentClassLogger();

      if ( mergeResult.MergeType == MergeType.NoChanges ) {
        return mergeResult.ResolvedItem;
      }

      if ( !mergeResult.ResolvedItem.IsOptionValid() ) {
        throw new InvalidResolutonException( key );
      }

      var resolutionSummary = GetResolutionSummary( filePath, key, mergeResult, operation );

      logger.Info( resolutionSummary );

      return mergeResult.ResolvedItem;
    }

    private static string GetResolutionSummary( string filePath, string key, MergeResult<T> mergeResult, CurrentOperation operation ) {

      var newLine = Environment.NewLine;
      var changesDescription = mergeResult.MergeType.ToString( operation );
      var newLineWithIndent = newLine + "  ";
      var resolutionDescription = mergeResult.ResolvedItem == null ? "Deleted" : ( newLineWithIndent + mergeResult.ResolvedItem.ToString().Replace( Environment.NewLine, newLineWithIndent ) );
      var resolvedWithDescription = mergeResult.ResolvedWith == ConflictItemType.Unknown ? "Custom" : mergeResult.ResolvedWith.ToString( operation );

      return $"{LogHelper.Header}{newLine}Resolution Summary{newLine}File: {filePath}{newLine}Key: {key}{newLine}Changes: {changesDescription}{newLine}Resolution: {resolvedWithDescription}{resolutionDescription}";
    }

    public static IEnumerable<T> MergeAll(
      string filePath,
      CurrentOperation operation,
      IDictionary<string, T> baseObj,
      IDictionary<string, T> localObj,
      IDictionary<string, T> incomingObj,
      IConflictResolver<T> conflictResolver ) {

      var context = new ConflictContext<T>( filePath, operation, baseObj, localObj, incomingObj );

      return MergeAll( context, conflictResolver );
    }

    public static IEnumerable<T> MergeAll( ConflictContext<T> context, IConflictResolver<T> conflictResolver ) {

      return context.GetKeys()
        .Select( key => Resolve( context, key, conflictResolver ) )
        .WhereNotNull()
        .ToList();
    }

    public static IEnumerable<T> MergeAllDuplicates(
      string filePath,
      CurrentOperation operation,
      IDictionary<string, IEnumerable<T>> baseObj,
      IDictionary<string, IEnumerable<T>> localObj,
      IDictionary<string, IEnumerable<T>> incomingObj,
      IConflictResolver<T> conflictResolver,
      IDuplicateResolver<T> duplicateResolver ) {

      var context = new ConflictContext<IEnumerable<T>>( filePath, operation, baseObj, localObj, incomingObj );

      return MergeAllDuplicates( context, conflictResolver, duplicateResolver );
    }

    public static IEnumerable<T> MergeAllDuplicates( ConflictContext<IEnumerable<T>> context,
      IConflictResolver<T> conflictResolver,
      IDuplicateResolver<T> duplicateResolver ) {

      return context.GetKeys()
        .Select( key => Resolve( context, key, conflictResolver, duplicateResolver ) )
        .WhereNotNull();
    }
  }
}
