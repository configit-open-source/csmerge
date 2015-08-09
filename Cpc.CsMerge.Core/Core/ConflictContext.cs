using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace CsMerge.Core {

  public class ConflictContext<T> {

    public string FilePath { get; }

    public CurrentOperation Operation { get; private set; }

    public IDictionary<string, T> BaseItems { get; }

    public IDictionary<string, T> LocalItems { get; }

    public IDictionary<string, T> IncomingItems { get; }

    public ConflictContext( string filePath, CurrentOperation operation, IDictionary<string, T> baseItems, IDictionary<string, T> localItems, IDictionary<string, T> incomingItems ) {
      FilePath = filePath;
      Operation = operation;
      BaseItems = baseItems;
      LocalItems = localItems;
      IncomingItems = incomingItems;
    }

    public IEnumerable<string> GetKeys() {
      return BaseItems.Keys.Union( LocalItems.Keys ).Union( IncomingItems.Keys ).OrderBy( i => i, StringComparer.OrdinalIgnoreCase );
    }

    public Conflict<T> CreateItemConflict( string key ) {
      return new Conflict<T>( FilePath, key, GetValue( BaseItems, key ), GetValue( LocalItems, key ), GetValue( IncomingItems, key ) );
    }

    private static T GetValue( IDictionary<string, T> itemDictionary, string key ) {
      return itemDictionary.ContainsKey( key ) ? itemDictionary[key] : default(T);
    }
  }
}
