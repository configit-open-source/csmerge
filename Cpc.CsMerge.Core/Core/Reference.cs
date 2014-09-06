namespace Cpc.CsMerge.Core {
  public class Reference: Item {
    public string ReferenceName { get; private set; }
    public bool? SpecificVersion { get; private set; }
    public string HintPath { get; private set; }

    public override string Key {
      get { return ReferenceName; }
    }

    public Reference( string referenceName, bool? specificVersion, string hintPath ) {
      ReferenceName = referenceName;
      SpecificVersion = specificVersion;
      HintPath = hintPath;
    }
  }
}