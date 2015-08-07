using System;
using System.Xml.Linq;

using Parsing;

namespace CsMerge.Core {
  /// <summary>
  /// Used for items that have no special semantics in the merge.
  /// </summary>
  public class RawItem: Item, IEquatable<RawItem> {
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

    public override bool Equals( Item other ) {
      return Equals( (object) other );
    }

    public override string Action {
      get {
        return Element.Name.LocalName;
      }
    }

    public override bool Equals( object obj ) {
      if ( ReferenceEquals( null, obj ) ) {
        return false;
      }
      if ( ReferenceEquals( this, obj ) ) {
        return true;
      }
      return obj.GetType() == GetType() && Equals( (RawItem) obj );
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = string.IsNullOrEmpty( Action ) ? 0 : Action.GetHashCode();
        hashCode = ( hashCode * 397 ) ^ ( string.IsNullOrEmpty( Key ) ? 0 : Key.GetHashCode() );
        return hashCode;
      }
    }

    public bool Equals( RawItem other ) {
      if ( ReferenceEquals( null, other ) ) {
        return false;
      }
      if ( ReferenceEquals( this, other ) ) {
        return true;
      }
      var xElement = other.ToElement( Element.Name.Namespace );

      return _key == other.Key && ( ReferenceEquals( xElement, Element ) ||
        Element.ToString( SaveOptions.DisableFormatting ) == xElement.ToString( SaveOptions.DisableFormatting ) );
    }

    public override XElement ToElement( XNamespace ns ) {
      return Element;
    }

    public override string ToString() {
      return Element.ToString( SaveOptions.OmitDuplicateNamespaces );
    }
  }
}