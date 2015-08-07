using System.Collections.Generic;
using System.Xml.Linq;

using Project;

namespace CsUpdate.Core {

  /// <summary>
  /// Temporary hack wrt. to avoiding reordering when aligning references.
  /// </summary>
  public class XmlComparer: IEqualityComparer<Item> {
    private readonly XNamespace _ns;

    public XmlComparer( XNamespace ns ) {
      _ns = ns;
    }

    public bool Equals( Item x, Item y ) {
      return x.ToElement( _ns ).ToString().Equals( y.ToElement( _ns ).ToString() );
    }

    public int GetHashCode( Item obj ) {
      return obj.ToElement( _ns ).ToString().GetHashCode();
    }
  }
}