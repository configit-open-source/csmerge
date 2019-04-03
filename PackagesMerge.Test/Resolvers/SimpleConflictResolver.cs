using System;
using CsMerge.Core;
using CsMerge.Core.Resolvers;
using Project;

namespace PackagesMerge.Test.Resolvers {
  
  public class SimpleConflictResolver<T>: IConflictResolver<T> where T: class, IConflictableItem {
    
    private readonly ConflictItemType _resolveTo;

    public SimpleConflictResolver( ConflictItemType resolveTo ) {
      _resolveTo = resolveTo;
    }

    public MergeResult<T> Resolve( Conflict<T> conflict ) {
      var item = GetResolutionItem( conflict );
      return new MergeResult<T>( conflict.Key, item, conflict.GetMergeType(), _resolveTo );
    }

    private T GetResolutionItem( IConflict<T> conflict ) {

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
