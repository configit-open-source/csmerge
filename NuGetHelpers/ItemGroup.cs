using System.Collections.Generic;

using Parsing;

namespace CsMerge.Core {
  public class ItemGroup {
    public IReadOnlyCollection<Item> Items { get; private set; }

    public ItemGroup( IReadOnlyCollection<Item> items ) {
      Items = items;
    }
  }
}