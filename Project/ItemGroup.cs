using System.Collections.Generic;

namespace Project {
  public class ItemGroup {
    public IReadOnlyCollection<Item> Items { get; private set; }

    public ItemGroup( IReadOnlyCollection<Item> items ) {
      Items = items;
    }
  }
}