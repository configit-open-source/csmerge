using System;

namespace Cpc.CsMerge.Core {
  public class ProjectReference: Item {
    public string CsProjPath { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }

    public override string Key {
      get { return ProjectId.ToString(); }
    }

    public ProjectReference( string csProjPath, Guid project, string name ) {
      Name = name;
      ProjectId = project;
      CsProjPath = csProjPath;
    }
  }
}