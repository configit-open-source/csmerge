using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using Configit.Base.Intervals;

namespace PackagesMerge {
  public class Package {
    protected bool Equals( Package other ) {
      return string.Equals( TargetFramework, other.TargetFramework ) &&
        string.Equals( Id, other.Id ) &&
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
      return Equals( (Package)obj );
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = ( TargetFramework != null ? TargetFramework.GetHashCode() : 0 );
        hashCode = ( hashCode * 397 ) ^ ( Id != null ? Id.GetHashCode() : 0 );
        hashCode = ( hashCode * 397 ) ^ ( AllowedVersions != null ? AllowedVersions.GetHashCode() : 0 );
        hashCode = ( hashCode * 397 ) ^ ( UserInstalled != null ? UserInstalled.GetHashCode() : 0 );
        return hashCode;
      }
    }

    private static readonly Regex AllowedVersionRegex = new Regex(
      @"(?<l>\[|\()?(?<lower>(\d|\.)*)?(,(?<upper>(\d|\.)*))?(?<u>\]|\))?",
      RegexOptions.Compiled |
      RegexOptions.CultureInvariant |
      RegexOptions.ExplicitCapture |
      RegexOptions.IgnorePatternWhitespace | 
      RegexOptions.Singleline );

    public Package( string id, Interval<PackageVersion> allowedVersions, string targetFramework, string userInstalled = null ) {
      Id = id;
      AllowedVersions = allowedVersions;
      TargetFramework = targetFramework;

      bool userInstalledParsed;
      if ( !string.IsNullOrEmpty( userInstalled ) && bool.TryParse( userInstalled, out userInstalledParsed ) ) {
        UserInstalled = userInstalledParsed;
      }
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

    public static IEnumerable<Package> Read( TextReader reader ) {
      var xml = XElement.Load( reader );

      return from packageXml in xml.Elements( "package" )
        let versionAttribute = packageXml.Attribute( "version" )
        let targetFrameworkAttribute = packageXml.Attribute( "targetFramework" )
        let userInstalledAttribute = packageXml.Attribute( "userInstalled" )
        let id = packageXml.Attribute( "id" ).Value
             let versions = versionAttribute != null ? GetAllowedVersion( versionAttribute.Value ) : null
             let targetFramework = targetFrameworkAttribute != null ? targetFrameworkAttribute.Value : null
             let userInstalled = userInstalledAttribute != null ? userInstalledAttribute.Value : null
             select new Package( id, versions, targetFramework, userInstalled );
    }

    public static void Write( IEnumerable<Package> packages, TextWriter writer, XmlWriterSettings settings = null ) {
      XElement element = new XElement( "packages" );
      foreach ( var packageGroup in packages.GroupBy( p => p.Id ) ) {
        if ( packageGroup.Count() > 1 ) {
          throw new NotImplementedException();
        }
        var package = packageGroup.First();

        var packagesElement = new XElement( "package", new XAttribute( "id", package.Id ) );

        if ( package.AllowedVersions != null ) {
          packagesElement.Add( new XAttribute( "version", SemVersionIntervalToString( package.AllowedVersions ) ) );
        }
        if ( !string.IsNullOrEmpty( package.TargetFramework ) ) {
          packagesElement.Add( new XAttribute( "targetFramework", package.TargetFramework ) );
        }

        if ( package.UserInstalled.HasValue ) {
          packagesElement.Add( new XAttribute( "userInstalled", package.UserInstalled.Value ) );
        }

        element.Add( packagesElement );
      }

      var xmlWriterSettings = settings ?? new XmlWriterSettings {
        Encoding = Encoding.UTF8,

        CloseOutput = true,
        NewLineChars = "\n",
        Indent = true
      };

      using ( var xmlWriter = XmlWriter.Create( writer, xmlWriterSettings ) ) {
        element.WriteTo( xmlWriter );
      }
    }

    private static string SemVersionIntervalToString( Interval<PackageVersion> interval ) {
      if ( interval.Lower == interval.Upper ) {
        return string.Format( "[{0}]", interval.Lower );
      }

      if ( interval.Upper == PackageVersion.MaxValue ) {
        // x.y notation shortcut
        return interval.Lower.ToString();
      }

      string lower = interval.Lower == PackageVersion.MinValue ? string.Empty : interval.Lower.ToString();
      string upper = interval.Upper == PackageVersion.MaxValue ? string.Empty : interval.Upper.ToString();

      return string.Format( "{0}{1},{2}{3}", interval.LowerOpen ? "(" : "[", lower, upper, interval.LowerOpen ? ")" : "]" );
    }

    private static Interval<PackageVersion> GetAllowedVersion( string versionString ) {
      if ( string.IsNullOrEmpty( versionString ) ) {
        return null;
      }

      Match matches = AllowedVersionRegex.Match( versionString );

      string lowerStr = matches.Groups["lower"].Value;
      string upperStr = matches.Groups["upper"].Value;

      bool lowerOpen = matches.Groups["l"].Value == "(";
      bool upperOpen = matches.Groups["u"].Value != "]";

      Interval<PackageVersion> allowedVersions;

      if ( string.IsNullOrEmpty( lowerStr ) && !string.IsNullOrEmpty( upperStr ) ) {
        allowedVersions = new Interval<PackageVersion>( PackageVersion.MinValue, upperStr );
      }
      else if ( string.IsNullOrEmpty( upperStr ) && !string.IsNullOrEmpty( lowerStr ) ) {
        allowedVersions = new Interval<PackageVersion>( lowerStr, PackageVersion.MaxValue );
      }
      else {
        allowedVersions = new Interval<PackageVersion>( lowerStr, upperStr );
      }

      allowedVersions.LowerOpen = lowerOpen;
      allowedVersions.UpperOpen = upperOpen;
      return allowedVersions;
    }

    public Interval<PackageVersion> AllowedVersions { get; set; } 
    public string Id { get; set; }

    public string TargetFramework { get; set; }
    public bool? UserInstalled { get; set; }

    public override string ToString() {
      return string.Format( "{0}/{1}/{2}", Id, SemVersionIntervalToString( AllowedVersions ), TargetFramework );
    }
  }

}