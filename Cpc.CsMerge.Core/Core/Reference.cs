using System;
using System.Reflection;
using System.Xml.Linq;

namespace Cpc.CsMerge.Core {
  public class Reference: Item {
    public string ReferenceName { get; private set; }

    public AssemblyName ReferenceAssemblyName { get { return new AssemblyName( ReferenceName ); } }
    public bool? SpecificVersion { get; private set; }
    public string HintPath { get; private set; }
    public bool? Private { get; private set; }

    public override string Key {
      get { return ReferenceName; }
    }

    public override string ToString() {
      return "Reference to " + ReferenceName;
    }

    public override bool Equals( Item other ) {
      return Equals( (object) other );
    }

    public override XElement ToElement( XNamespace ns ) {
      XElement e = new XElement( ns.GetName( Action ) );

      e.Add( new XAttribute( "Include", ReferenceName ) );

      if ( HintPath != null ) {
        e.Add( new XElement( ns.GetName( "HintPath" ) ), HintPath );
      }
      if ( SpecificVersion.HasValue ) {
        e.Add( new XElement( ns.GetName( "SpecificVersion" ), SpecificVersion.Value ) );
      }
      if ( Private.HasValue ) {
        e.Add( new XElement( ns.GetName( "Private" ), Private.Value ) );
      }
      return e;
    }

    public bool Equals( Reference other ) {
      if ( ReferenceEquals( null, other ) ) {
        return false;
      }
      if ( ReferenceEquals( this, other ) ) {
        return true;
      }
      return string.Equals( HintPath, other.HintPath ) &&
        SpecificVersion == other.SpecificVersion &&
        string.Equals( ReferenceName, other.ReferenceName );
    }

    public override bool Equals( object obj ) {
      if ( ReferenceEquals( null, obj ) ) {
        return false;
      }
      if ( ReferenceEquals( this, obj ) ) {
        return true;
      }
      return obj.GetType() == GetType() && Equals( (Reference) obj );
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = ( HintPath != null ? HintPath.GetHashCode() : 0 );
        hashCode = ( hashCode * 397 ) ^ SpecificVersion.GetHashCode();
        hashCode = ( hashCode * 397 ) ^ ( ReferenceName != null ? ReferenceName.GetHashCode() : 0 );
        return hashCode;
      }
    }

    public Reference( string referenceName, bool? specificVersion, bool? @private, string hintPath ) {
      ReferenceName = referenceName;
      SpecificVersion = specificVersion;
      Private = @private;
      HintPath = hintPath;
    }
  }
}