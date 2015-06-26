using System.IO;
using System.Xml.Linq;

namespace CsMerge.Core {
  public class FileIncludeItem: Item {
    public string Folder { get; set; }

    public string FileName { get; set; }

    private readonly string _action;

    public FileIncludeItem( string action, string folder, string fileName ) {
      Folder = folder;
      FileName = fileName;
      _action = action;
    }

    protected bool Equals( FileIncludeItem other ) {
      return string.Equals( _action, other._action ) &&
             string.Equals( Folder, other.Folder ) &&
             string.Equals( FileName, other.FileName );
    }

    public override bool Equals( object obj ) {
      if ( ReferenceEquals( null, obj ) ) {
        return false;
      }
      if ( ReferenceEquals( this, obj ) ) {
        return true;
      }
      if ( obj.GetType() != this.GetType() ) {
        return false;
      }
      return Equals( (FileIncludeItem)obj );
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = ( _action != null ? _action.GetHashCode() : 0 );
        hashCode = ( hashCode * 397 ) ^ ( Folder != null ? Folder.GetHashCode() : 0 );
        hashCode = ( hashCode * 397 ) ^ ( FileName != null ? FileName.GetHashCode() : 0 );
        return hashCode;
      }
    }

    public override string Action { get { return _action; } }

    public override string Key {
      get { return Action + " " + Folder + "\\" + FileName; }
    }

    public override bool Equals( Item other ) {
      return Equals( (object) other );
    }

    public override XElement ToElement( XNamespace ns ) {
      var e = new XElement( ns.GetName( Action ) );
      e.Add( new XAttribute( "Include", Path.Combine( Folder, FileName ) ) );
      return e;
    }

    public override string ToString() {
      return Key;
    }
  }
}