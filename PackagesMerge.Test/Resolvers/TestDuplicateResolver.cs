﻿using System;
using System.Collections.Generic;
using System.Linq;

using CsMerge.Core;
using CsMerge.Core.Resolvers;

using Project;

namespace PackagesMerge.Test.Resolvers {

  public class TestDuplicateResolver<T>: IDuplicateResolver<T> where T: class, IConflictableItem {

    private readonly ConflictItemType _conflictItemType;
    private readonly int _autoResolveItemIndex;

    public Dictionary<string, T> Resolutions { get; private set; }

    public TestDuplicateResolver( ConflictItemType conflictItemType, int autoResolveItemIndex = 0 ) {
      _conflictItemType = conflictItemType;
      _autoResolveItemIndex = autoResolveItemIndex;

      Resolutions = new Dictionary<string, T>();
    }

    public MergeResult<T> Resolve( Conflict<IEnumerable<T>> conflict ) {
      var items = GetCollection( conflict ).Where( i => i.IsOptionValid() ).ToList();

      var itemIndex = Math.Min( _autoResolveItemIndex, items.Count - 1 );

      var resolution = itemIndex < 0 ? default( T ) : items[itemIndex];

      Resolutions.Add( GetKey( conflict ), resolution );

      return new MergeResult<T>( conflict.Key, resolution, conflict.GetMergeType(), _conflictItemType );
    }

    private static string GetKey( Conflict<IEnumerable<T>> conflict ) {
      return ( conflict.Base.FirstOrDefault() ?? conflict.Local.FirstOrDefault() ?? conflict.Incoming.First() ).Key;
    }

    private IEnumerable<T> GetCollection( Conflict<IEnumerable<T>> conflict ) {
      switch ( _conflictItemType ) {
        case ConflictItemType.Base:
          return conflict.Base;
        case ConflictItemType.Local:
          return conflict.Local;
        case ConflictItemType.Incoming:
          return conflict.Incoming;
        default:
          throw new Exception( "Unhandled AutoResolveType: " + _conflictItemType );
      }
    }
  }
}
