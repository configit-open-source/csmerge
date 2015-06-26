using System;
using System.Security.Policy;
using System.Xml.Linq;

namespace Cpc.CsMerge.Core {
  /// <summary>
  /// Used for items that have no special semantics in the merge.
  /// </summary>
  public class RawItem: Item {
    private readonly string _key;

    public RawItem( XElement element, string key = null ) {
      _key = key;
      Element = new XElement( element );
    }

    public XElement Element { get; set; }

    public override string Key {
      get {
        return _key ?? Element.ToString();
      }
    }

    public override string Action {
      get {
        return Element.Name.LocalName;
      }
    }

    public override bool Equals( Item other ) {
      var xElement = other.ToElement( Element.Name.Namespace );

      return _key == other.Key && ( ReferenceEquals( xElement, Element ) || Element.ToString() == xElement.ToString() );
    }

    public override XElement ToElement( XNamespace ns ) {
      return Element;
    }

    public override string ToString() {
      return Element.ToString();
    }
  }
}