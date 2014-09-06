using System.Collections.Generic;

namespace Cpc.CsMerge.Core {
  public class ProjectFile {
    public string Name { get; private set; }

    public IReadOnlyCollection<ItemGroup> ItemGroups { get; private set; }

    public ProjectFile( string name, IReadOnlyCollection<ItemGroup> itemGroups ) {
      Name = name;
      ItemGroups = itemGroups;
    }
  }
}