using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Project {

  public class ProjectReference: RawItem, IEquatable<ProjectReference> {
    
    private readonly bool _hasName;

    public string CsProjPath { get; }

    public Guid? ProjectId { get; }

    public string Name { get; }

    public override bool Equals( Item other ) {
      return Equals( (object) other );
    }

    public override XElement ToElement( XNamespace ns ) {
      if ( Element != null ) {
        return Element;
      }

      var e = new XElement( ns.GetName( Action ) );
      e.Add( new XAttribute( "Include", CsProjPath ) );

      if ( ProjectId.HasValue ) {
        e.Add( new XElement( ns.GetName( "Project" ), "{" + ProjectId.Value + "}" ) );
      }

      if ( _hasName ) {
        e.Add( new XElement( ns.GetName( "Name" ), Name ) );
      }

      return e;
    }

    public bool Equals( ProjectReference other ) {

      if ( ReferenceEquals( null, other ) ) {
        return false;
      }

      if ( ReferenceEquals( this, other ) ) {
        return true;
      }

      return string.Equals( CsProjPath, other.CsProjPath )
             && string.Equals( Name, other.Name )
             && ProjectId == other.ProjectId;
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

    public ProjectReference( string csProjPath, Guid? project, string name, XElement element )
      : base( element, project?.ToString() ?? csProjPath ) {
      
      if ( string.IsNullOrEmpty( name ) ) {
        Name = CsProjPath.Contains( "\\" ) ? csProjPath.Substring( csProjPath.LastIndexOf( '\\' ) + 1 ) : csProjPath;
      }
      else {
        Name = name;
        _hasName = true;
      }
      
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

    public static ProjectReference Deserialize( XElement element ) {

      var includeAttribute = element.Attribute( "Include" );

      if ( includeAttribute == null ) {
        return null;
      }

      var include = includeAttribute.Value;

      var xNamespace = element.Name.Namespace;

      var projectElement = element.Element( xNamespace.GetName( "Project" ) )?.Value;
      var project = projectElement == null ? (Guid?) null : Guid.Parse( projectElement );
      var name = element.Element( xNamespace.GetName( "Name" ) )?.Value;

      return new ProjectReference( include, project, name, element );
    }
  }
}