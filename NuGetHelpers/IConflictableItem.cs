using System.Xml.Linq;

namespace CsMerge.Core {
  public interface IConflictableItem {
    string Key { get; }
    bool IsResolveOption { get; }
    XElement ToElement( XNamespace ns );
  }
}