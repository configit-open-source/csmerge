using System.Collections.Generic;

namespace CsMerge.Core {

  public class Conflict<T>: IConflict<T> {

    public Conflict( string filePath, string key, T @base, T local, T incoming ) {
      Base = @base;
      Local = local;
      Incoming = incoming;

      Key = key;
      FilePath = filePath;
    }

    public string Key { get; private set; }
    public string FilePath { get; private set; }

    public T Base { get; private set; }
    public T Local { get; private set; }
    public T Incoming { get; private set; }



    public IEnumerable<T> GetItems( ConflictItemType conflictItemType = ConflictItemType.All ) {

      if ( conflictItemType.IsSet( ConflictItemType.Base ) ) {
        yield return Base;
      }

      if ( conflictItemType.IsSet( ConflictItemType.Local ) ) {
        yield return Local;
      }

      if ( conflictItemType.IsSet( ConflictItemType.Incoming ) ) {
        yield return Incoming;
      }
    }
  }
}