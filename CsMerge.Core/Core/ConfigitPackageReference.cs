using System.Text;
using System.Xml.Linq;

using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.PackagingCore;
using NuGet.Versioning;

namespace CsMerge.Core {
  public class ConfigitPackageReference: IConflictableItem {
    private readonly PackageReference _reference;

    public static implicit operator ConfigitPackageReference( PackageReference reference ) {
      return new ConfigitPackageReference( reference );
    }

    public static implicit operator PackageReference( ConfigitPackageReference reference ) {
      return reference == null ? null : reference._reference;
    }

    public string Key { get { return _reference.PackageIdentity.Id; } }

    public bool IsResolveOption {
      get { return true; }
    }

    protected bool Equals( ConfigitPackageReference other ) {
      return Equals( _reference, other._reference );
    }

    public override bool Equals( object obj ) {
      if ( ReferenceEquals( null, obj ) ) {
        return false;
      }
      if ( ReferenceEquals( this, obj ) ) {
        return true;
      }

      return Equals( (ConfigitPackageReference)obj );
    }

    public override int GetHashCode() {
      return _reference != null ? _reference.GetHashCode() : 0;
    }

    public static bool operator ==( ConfigitPackageReference left, ConfigitPackageReference right ) {
      return Equals( left, right );
    }

    public static bool operator !=( ConfigitPackageReference left, ConfigitPackageReference right ) {
      return !Equals( left, right );
    }

    public XElement ToElement( XNamespace ns ) {
      // Code loosely based on nuget source code (PackageReferenceFile.AddEntry)
      var newElement = new XElement( "package",
                            new XAttribute( "id", _reference.PackageIdentity.Id ),
                            new XAttribute( "version", _reference.PackageIdentity.Version.ToString() ) );
      if ( _reference.TargetFramework != null ) {
        newElement.Add( new XAttribute( "targetFramework", _reference.TargetFramework.GetShortFolderName() ) );
      }

      if ( _reference.HasAllowedVersions ) {
        newElement.Add( new XAttribute( "allowedVersions", _reference.AllowedVersions.ToString() ) );
      }

      // Restore the development dependency flag
      if ( _reference.IsDevelopmentDependency ) {
        newElement.Add( new XAttribute( "developmentDependency", _reference.IsDevelopmentDependency ) );
      }

      if ( _reference.RequireReinstallation ) {
        newElement.Add( new XAttribute( "requireReinstallation", "true" ) );
      }

      return newElement;
    }

    public ConfigitPackageReference( string id, string version, string targetFramework = null, string allowedVersion = null, bool userInstalled = false ) {
      _reference = new PackageReference(
        new PackageIdentity( id, new NuGetVersion( version ) ),
        targetFramework == null ? null : new NuGetFramework( targetFramework ),
        userInstalled, false, false, allowedVersion != null ? VersionRange.Parse( allowedVersion ) : null );
    }

    private ConfigitPackageReference( PackageReference reference ) {
      _reference = reference;
    }

    public override string ToString() {
      StringBuilder s = new StringBuilder();
      if ( !string.IsNullOrEmpty( _reference.PackageIdentity.Id ) ) {
        s.AppendLine( "Id: " + _reference.PackageIdentity.Id );
      }
      s.AppendLine( "Version: " + _reference.PackageIdentity.Version );

      if ( _reference.TargetFramework != null ) {
        s.AppendLine( "TargetFramework: " + _reference.TargetFramework.GetShortFolderName() );
      }
      if ( _reference.HasAllowedVersions ) {
        s.AppendLine( "AllowedVersions: " + _reference.AllowedVersions + " " );
      }
      if ( _reference.IsUserInstalled ) {
        s.AppendLine( "UserInstalled: " + _reference.IsUserInstalled + " " );
      }
      if ( _reference.IsDevelopmentDependency ) {
        s.AppendLine( "developmentDependency: " + _reference.IsDevelopmentDependency + " " );
      }
      if ( _reference.RequireReinstallation ) {
        s.AppendLine( "requireReinstallation: " + _reference.RequireReinstallation + " " );
      }
      return s.ToString();
    }
  }
}