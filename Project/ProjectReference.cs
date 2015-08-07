using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Project {
  public class ProjectReference: RawItem {
    public string CsProjPath { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }

    public override string Key {
      get { return ProjectId.ToString(); }
    }

    public override bool Equals( Item other ) {
      return Equals( (object) other );
    }

    public override XElement ToElement( XNamespace ns ) {
      if ( Element != null ) {
        return Element;
      }

      var e = new XElement( ns.GetName( Action ) );
      e.Add( new XAttribute( "Include", CsProjPath ) );
      e.Add( new XElement( ns.GetName( "Project" ), "{" + ProjectId + "}" ) );
      e.Add( new XElement( ns.GetName( "Name" ), Name ) );
      return e;
    }

    public bool Equals( ProjectReference other ) {
      if ( ReferenceEquals( null, other ) ) {
        return false;
      }
      if ( ReferenceEquals( this, other ) ) {
        return true;
      }
      return string.Equals( CsProjPath, other.CsProjPath ) && ProjectId.Equals( other.ProjectId ) && string.Equals( Name, other.Name );
    }

    public override bool Equals( object obj ) {
      if ( ReferenceEquals( null, obj ) ) {
        return false;
      }
      if ( ReferenceEquals( this, obj ) ) {
        return true;
      }
      if ( obj.GetType() != GetType() ) {
        return false;
      }
      return Equals( (ProjectReference) obj );
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = CsProjPath != null ? CsProjPath.GetHashCode() : 0;
        hashCode = ( hashCode * 397 ) ^ ProjectId.GetHashCode();
        hashCode = ( hashCode * 397 ) ^ ( Name != null ? Name.GetHashCode() : 0 );
        return hashCode;
      }
    }

    public ProjectReference( string csProjPath, Guid project, string name, XElement element ) : base( element ) {
      Name = name;
      ProjectId = project;
      CsProjPath = csProjPath;
    }

    public override string ToString() {

      var propertyNames = new List<string>();

      propertyNames.AddPropertyIfNotNull( Name, "Name" );
      propertyNames.AddPropertyIfNotNull( Key, "Key" );
      propertyNames.AddPropertyIfNotNull( CsProjPath, "Path" );

      return string.Join( Environment.NewLine, propertyNames );
    }
  }
}