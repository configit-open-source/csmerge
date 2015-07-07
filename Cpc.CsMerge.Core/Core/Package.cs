using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CsMerge.Core {
  public class Package: IEquatable<Package>, IKeyedEntry {

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
        var hashCode = ( TargetFramework != null ? TargetFramework.GetHashCode() : 0 );
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

    public static void Write( IEnumerable<Package> packages, string path ) {
      using ( var fw = new StreamWriter( path ) ) {
        Write( packages, fw );
      }
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

      return from packageXml in xml.Elements( "package" )
             let versionAttribute = packageXml.Attribute( "version" )
             let allowedVersionAttribute = packageXml.Attribute( "allowedVersions" )
             let targetFrameworkAttribute = packageXml.Attribute( "targetFramework" )
             let userInstalledAttribute = packageXml.Attribute( "userInstalled" )
             let id = packageXml.Attribute( "id" ).Value
             let versions = versionAttribute != null ? PackageVersion.Parse( versionAttribute.Value ) : null
             let allowedVersions = allowedVersionAttribute != null ? allowedVersionAttribute.Value : null
             let targetFramework = targetFrameworkAttribute != null ? targetFrameworkAttribute.Value : null
             let userInstalled = userInstalledAttribute != null ? userInstalledAttribute.Value : null
             select new Package( id, versions, targetFramework, allowedVersions, ParseNullableBool( userInstalled ) );
    }

    private static bool? ParseNullableBool( string s ) {
      bool parsed;
      if ( !string.IsNullOrEmpty( s ) && bool.TryParse( s, out parsed ) ) {
        return parsed;
      }
      return null;
    }

    public static void Write( IEnumerable<Package> packages, TextWriter writer, XmlWriterSettings settings = null ) {
      XElement element = new XElement( "packages" );
      foreach ( var package in packages ) {
        var packagesElement = new XElement( "package", new XAttribute( "id", package.Id ) );

        if ( package.Version != null ) {
          packagesElement.Add( new XAttribute( "version", package.Version ) );
        }

        if ( package.AllowedVersions != null ) {
          packagesElement.Add( new XAttribute( "allowedVersions", package.AllowedVersions ) );
        }

        if ( !string.IsNullOrEmpty( package.TargetFramework ) ) {
          packagesElement.Add( new XAttribute( "targetFramework", package.TargetFramework ) );
        }

        if ( package.UserInstalled.HasValue ) {
          packagesElement.Add( new XAttribute( "userInstalled", package.UserInstalled.Value ) );
        }

        element.Add( packagesElement );
      }

      WriteXml( writer, element, settings );
    }

    public static void WriteXml( string path, XNode root ) {
      using ( var textWriter = new StreamWriter( path ) ) {
        WriteXml( textWriter, root );
      }
    }

    public static void WriteXml( TextWriter writer, XNode element, XmlWriterSettings settings = null ) {
      var xmlWriterSettings = settings
                              ?? new XmlWriterSettings {
                                Encoding = Encoding.UTF8,
                                CloseOutput = true,
                                NewLineChars = "\n",
                                Indent = true,
                                NamespaceHandling = NamespaceHandling.OmitDuplicates,
                                ConformanceLevel = ConformanceLevel.Document,
                              };
      using ( var xmlWriter = XmlWriter.Create( writer, xmlWriterSettings ) ) {
        element.WriteTo( xmlWriter );
      }
    }

    public string AllowedVersions { get; set; }

    public PackageVersion Version { get; set; }

    public string Id { get; set; }

    public string TargetFramework { get; set; }
    public bool? UserInstalled { get; set; }

    public override string ToString() {
      StringBuilder s = new StringBuilder();
      if ( !string.IsNullOrEmpty( Id ) ) {
        s.AppendLine( "Id: " + Id  );
      }
      if ( Version != null ) {
        s.AppendLine( "Version:" + Version + " " );
      }
      if ( !string.IsNullOrEmpty( TargetFramework ) ) {
        s.AppendLine( "TargetFramework: " + TargetFramework );
      }
      if ( !string.IsNullOrEmpty( AllowedVersions ) ) {
        s.AppendLine( "AllowedVersions: " + AllowedVersions + " " );
      }

      if ( UserInstalled.HasValue ) {
        s.AppendLine( "UserInstalled: " + UserInstalled.Value + " " );
      }
      return s.ToString();
    }

    public string ToPackageFolderName() {
      return Id + "." + Version;
    }

    public string Key {
      get { return Id; }
    }
  }
}