using System;

namespace CsMerge.Core {
  public class Package: IEquatable<Package>, IKeyedEntry {
    public PackageVersion Version { get; private set; }

    public string Id { get; private set; }

    public Package(string id, PackageVersion version ) {
      Id = id;
      Version = version;
    }

    public bool Equals( Package other ) {
      if ( ReferenceEquals( null, other ) ) {
        return false;
      }
      if ( ReferenceEquals( this, other ) ) {
        return true;
      }
      return Equals( Version, other.Version ) && string.Equals( Id, other.Id );
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
      return Equals( (Package) obj );
    }

    public override int GetHashCode() {
      unchecked {
        return ( ( Version != null ? Version.GetHashCode() : 0 ) * 397 ) ^ ( Id != null ? Id.GetHashCode() : 0 );
      }
    }

    public static bool operator ==( Package left, Package right ) {
      return Equals( left, right );
    }

    public static bool operator !=( Package left, Package right ) {
      return !Equals( left, right );
    }

    public string Key { get { return Id; } }

    public string ToPackageFolderName() {
      return Id + "." + Version;
    }
  }
}