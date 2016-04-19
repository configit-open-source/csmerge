using System.Xml.Linq;

namespace Project {
  public interface IConflictableItem {
    string Key { get; }
    bool IsResolveOption { get; }
    XElement ToElement( XNamespace ns );
  }
}