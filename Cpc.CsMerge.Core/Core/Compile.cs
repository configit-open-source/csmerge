namespace Cpc.CsMerge.Core {
  public class Compile: Item {
    public string Folder { get; private set; }
    public string FileName { get; private set; }

    public override string Key {
      get { return Folder + "\\" + FileName; }
    }

    public Compile( string folder, string fileName ) {
      Folder = folder;
      FileName = fileName;
    }
  }
}