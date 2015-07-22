using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CsMerge.Core {
  public class Package: IEquatable<Package>, IConflictableItem {

    public bool IsResolveOption {
      get { return true; }
    }

    public bool Equals( Package other ) {
      if ( ReferenceEquals( other, null ) ) {
        return false;
      }
      return string.Equals( TargetFramework, other.TargetFramework ) &&
        string.Equals( Id, other.Id ) &&
        string.Equals( Version, other.Version ) &&
        Equals( AllowedVersions, other.AllowedVersions ) &&
        Equals( UserInstalled, other.UserInstalled );
    }

    public static bool operator ==( Package left, Package right ) {
      if ( ReferenceEquals( left, null ) && ReferenceEquals( right, null ) ) {
        return true;
      }
      if ( ReferenceEquals( left, null ) || ReferenceEquals( right, null ) ) {
        return false;
      }
      return left.Equals( right );
    }

    public static bool operator !=( Package left, Package right ) {
      return !( left == right );
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
      return Equals( (Package) obj );
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = TargetFramework != null ? TargetFramework.GetHashCode() : 0;
        hashCode = ( hashCode * 397 ) ^ ( Id != null ? Id.GetHashCode() : 0 );
        hashCode = ( hashCode * 397 ) ^ ( AllowedVersions != null ? AllowedVersions.GetHashCode() : 0 );
        hashCode = ( hashCode * 397 ) ^ ( UserInstalled != null ? UserInstalled.GetHashCode() : 0 );
        hashCode = ( hashCode * 397 ) ^ ( Version != null ? Version.GetHashCode() : 0 );
        return hashCode;
      }
    }

    public Package( string id,
      PackageVersion version,
      string targetFramework,
      string allowedVersions = null,
      bool? userInstalled = null ) {
      Id = id;
      Version = version;
      AllowedVersions = allowedVersions;
      TargetFramework = targetFramework;

      UserInstalled = userInstalled;
    }

    public Package( XElement packageXml ) {
      var versionAttribute = packageXml.Attribute( "version" );

      var allowedVersionAttribute = packageXml.Attribute( "allowedVersions" );
      var targetFrameworkAttribute = packageXml.Attribute( "targetFramework" );
      var userInstalledAttribute = packageXml.Attribute( "userInstalled" );

      var userInstalled = userInstalledAttribute != null ? userInstalledAttribute.Value : null;

      Id = packageXml.Attribute( "id" ).Value;
      Version = versionAttribute != null ? PackageVersion.Parse( versionAttribute.Value ) : null;
      TargetFramework = targetFrameworkAttribute != null ? targetFrameworkAttribute.Value : null;
      AllowedVersions = allowedVersionAttribute != null ? allowedVersionAttribute.Value : null;
      UserInstalled = ParseNullableBool( userInstalled );
    }

    public Package Clone() {
      return new Package(
        Id,
        Version,
        TargetFramework,
        AllowedVersions,
        UserInstalled );
    }

    public static IEnumerable<Package> Read( string path ) {
      if ( !File.Exists( path ) ) {
        return new Package[0];
      }
      var content = File.ReadAllText( path );

      if ( string.IsNullOrEmpty( content ) ) {
        return new Package[0];
      }
      return Read( new StringReader( content ) );
    }

    public static IEnumerable<Package> Parse( string content ) {
      if ( string.IsNullOrEmpty( content ) ) {
        return new Package[0];
      }
      using ( var stringReader = new StringReader( content ) ) {
        return Read( stringReader );
      }
    }

    public static IEnumerable<Package> Read( TextReader reader ) {
      var xml = XElement.Load( reader );

      return xml.Elements( "package" ).Select( e => new Package( e ) );
    }

    private static bool? ParseNullableBool( string s ) {
      bool parsed;
      if ( !string.IsNullOrEmpty( s ) && bool.TryParse( s, out parsed ) ) {
        return parsed;
      }
      return null;
    }

    public string AllowedVersions { get; set; }

    public PackageVersion Version { get; set; }

    public string Id { get; set; }

    public string TargetFramework { get; set; }
    public bool? UserInstalled { get; set; }

    public override string ToString() {

      var propertyNames = new List<string>();

      propertyNames.AddPropertyIfNotNull( Id, "Id" );
      propertyNames.AddPropertyIfNotNull( Version, "Version" );
      propertyNames.AddPropertyIfNotNull( TargetFramework, "TargetFramework" );
      propertyNames.AddPropertyIfNotNull( AllowedVersions, "AllowedVersions" );

      if ( UserInstalled.HasValue ) {
        propertyNames.AddPropertyIfNotNull( UserInstalled.Value, "UserInstalled" );
      }

      return string.Join( Environment.NewLine, propertyNames );
    }

    public string ToPackageFolderName() {
      return Id + "." + Version;
    }

    public string Key {
      get { return Id; }
    }

    public XElement ToElement( XNamespace ns ) {
      var packageElement = new XElement( "package", new XAttribute( "id", Id ) );

      if ( Version != null ) {
        packageElement.Add( new XAttribute( "version", Version ) );
      }

      if ( AllowedVersions != null ) {
        packageElement.Add( new XAttribute( "allowedVersions", AllowedVersions ) );
      }

      if ( !string.IsNullOrEmpty( TargetFramework ) ) {
        packageElement.Add( new XAttribute( "targetFramework", TargetFramework ) );
      }

      if ( UserInstalled.HasValue ) {
        packageElement.Add( new XAttribute( "userInstalled", UserInstalled.Value ) );
      }

      return packageElement;
    }

    public static void Write( IEnumerable<Package> packages, string path ) {
      using ( var fw = new StreamWriter( path ) ) {
        Write( packages, fw );
      }
    }

    public static void Write( IEnumerable<Package> packages, TextWriter writer, XmlWriterSettings settings = null ) {

      var element = new XElement( "packages" );

      foreach ( var package in packages ) {
        element.Add( package.ToElement( "" ) );
      }

      element.WriteXml( writer, settings );
    }
  }
}