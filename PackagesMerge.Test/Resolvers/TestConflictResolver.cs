using System;
using System.Collections.Generic;
using CsMerge.Core;
using CsMerge.Core.Resolvers;

namespace PackagesMerge.Test.Resolvers {
  public class TestConflictResolver<T>: IConflictResolver<T> where T: class, IConflictableItem {

    private readonly ConflictItemType _resolveTo;

    public Dictionary<string, T> Resolutions { get; private set; }
    public bool Called { get; set; }

    public TestConflictResolver( ConflictItemType resolveTo ) {
      _resolveTo = resolveTo;
      Resolutions = new Dictionary<string, T>();
    }

    public T Resolve( Conflict<T> conflict ) {

      Called = true;

      var item = GetResolutionItem( conflict );

      Resolutions.Add( GetKey( conflict ), item );

      return item;
    }

    private static string GetKey( Conflict<T> conflict ) {
      return ( conflict.Base ?? conflict.Local ?? conflict.Incoming ).Key;
    }

    private T GetResolutionItem( Conflict<T> conflict ) {

      switch ( _resolveTo ) {
        case ConflictItemType.Base:
          return conflict.Base;
        case ConflictItemType.Local:
          return conflict.Local;
        case ConflictItemType.Incoming:
          return conflict.Incoming;
        default:
          throw new ArgumentException( "Unhandled resolve to type: " + _resolveTo );
      }
    }
  }
}
