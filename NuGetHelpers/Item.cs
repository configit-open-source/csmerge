using System;
using System.Xml.Linq;

using CsMerge.Core;

namespace Parsing {
  public abstract class Item: IEquatable<Item>, IConflictableItem {

    public abstract override int GetHashCode();

    public virtual string Action { get { return GetType().Name; } }
    public abstract string Key { get; }
    public bool IsResolveOption { get; set; }

    protected Item() {
      IsResolveOption = true;
    }

    public abstract bool Equals( Item other );

    public static bool operator ==( Item i1, Item i2 ) {
      if ( ReferenceEquals( null, i1 ) ^ ReferenceEquals( null, i2 ) ) {
        return false;
      }
      return ReferenceEquals( null, i1 ) || i1.Equals( i2 );
    }

    public abstract override bool Equals( object obj );

    public static bool operator !=( Item i1, Item i2 ) {
      return !( i1 == i2 );
    }

    public abstract XElement ToElement( XNamespace ns );

    public override string ToString() {
      return Key;
    }
  }
}