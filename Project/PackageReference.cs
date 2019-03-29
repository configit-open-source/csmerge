using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Project {

  public class PackageReference: Item {

    private const string IncludeAttributeName = "Include";
    private const string ConditionAttributeName = "Condition";
    private const string VersionAttributeName = "Version";
    private const string IncludeAssetsElementName = "IncludeAssets";
    private const string ExcludeAssetsElementName = "ExcludeAssets";
    private const string PrivateAssetsElementName = "PrivateAssets";

    public PackageReference( XElement originalItemElement ) {

      if ( originalItemElement == null ) {
        throw new ArgumentNullException( nameof(originalItemElement) );
      }

      Include = originalItemElement.Attribute( IncludeAttributeName )?.Value;
      Condition = originalItemElement.Attribute( ConditionAttributeName )?.Value;
      Version = originalItemElement.Attribute( VersionAttributeName )?.Value ?? GetElement( originalItemElement, VersionAttributeName )?.Value;

      if ( string.IsNullOrEmpty( Include ) ) {
        throw new ArgumentException( "Include attribute was not specified" );
      }
      
      if ( string.IsNullOrEmpty( Version ) ) {
        throw new ArgumentException( "Version attribute was not specified" );
      }

      System.Version.TryParse( Version, out var parsedVersion );
      ParsedVersion = parsedVersion;

      IncludeAssets = ParseDelimitedCollection( originalItemElement, IncludeAssetsElementName );
      ExcludeAssets = ParseDelimitedCollection( originalItemElement, ExcludeAssetsElementName );
      PrivateAssets = ParseDelimitedCollection( originalItemElement, PrivateAssetsElementName );

      var hasUnsupportedAttributes = originalItemElement.Attributes().Select( a => a.Name.LocalName ).Except( new[] { IncludeAttributeName, ConditionAttributeName, VersionAttributeName } ).Any();
      var hasUnsupportedElements = originalItemElement.Elements().Select( a => a.Name.LocalName ).Except( new[] { VersionAttributeName, IncludeAssetsElementName, ExcludeAssetsElementName, PrivateAssetsElementName } ).Any();

      if ( hasUnsupportedAttributes || hasUnsupportedElements ) {
        HasUnsupportedContent = true;
      }
    }
    
    public string Include { get; }
    
    public string Condition { get; }

    public string Version { get; }

    public Version ParsedVersion { get; }

    public IReadOnlyCollection<string> IncludeAssets { get; }

    public IReadOnlyCollection<string> ExcludeAssets { get; }

    public IReadOnlyCollection<string> PrivateAssets { get; }
    
    public bool HasUnsupportedContent { get; }

    private static XElement GetElement( XElement rootElement, string childElementName ) {
      return rootElement.Element( XName.Get( childElementName, rootElement.GetDefaultNamespace().NamespaceName ) );
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = Include?.GetHashCode() ?? 0;
        hashCode = ( hashCode * 397 ) ^ Version.GetHashCode();
        return hashCode;
      }
    }

    public override string Key => Include;

    public override bool Equals( Item other ) {
      
      if ( ReferenceEquals( null, other ) ) {
        return false;
      }
      
      if ( ReferenceEquals( this, other ) ) {
        return true;
      }

      return other.GetType() == GetType() && Equals( (PackageReference) other );
    }

    public override bool Equals( object obj ) {
      
      if ( ReferenceEquals( null, obj ) ) {
        return false;
      }
      
      if ( ReferenceEquals( this, obj ) ) {
        return true;
      }

      return obj.GetType() == GetType() && Equals( (PackageReference) obj );
    }

    public bool Equals( PackageReference other ) {
      return Equals( other, false );
    }
    
    private bool Equals( PackageReference other, bool ignoreVersion ) {
      
      if ( ReferenceEquals( null, other ) ) {
        return false;
      }

      if ( ReferenceEquals( this, other ) ) {
        return true;
      }

      return string.Equals( Include, other.Include )
             && string.Equals( Condition, other.Condition )
             && ( ignoreVersion || string.Equals( Version, other.Version ) )
             && IncludeAssets.IsEquivalentTo( other.IncludeAssets )
             && ExcludeAssets.IsEquivalentTo( other.ExcludeAssets )
             && PrivateAssets.IsEquivalentTo( other.PrivateAssets );
    }

    public override XElement ToElement( XNamespace ns ) {

      var e = new XElement( ns.GetName( Action ) );

      e.Add( new XAttribute( IncludeAttributeName, Include ) );
      e.Add( new XAttribute( VersionAttributeName, Version ) );

      if ( !string.IsNullOrEmpty( Condition ) ) {
        e.Add( new XAttribute( ConditionAttributeName, Condition ) );
      }
      
      AddCollectionElement( e, IncludeAssetsElementName, IncludeAssets );
      AddCollectionElement( e, ExcludeAssetsElementName, ExcludeAssets );
      AddCollectionElement( e, PrivateAssetsElementName, PrivateAssets );
      
      return e;
    }

    private static void AddCollectionElement( XElement rootElement, string elementName, IReadOnlyCollection<string> items ) {
      if ( !items.Any() ) {
        return;
      }

      rootElement.AddElement( elementName, items.ToDelimited( ";" ) );
    }

    public bool EqualsIgnoringVersion( PackageReference other ) {
      return Equals( other, true );
    }

    private static IReadOnlyCollection<string> ParseDelimitedCollection( XElement rootElement, string elementName ) {
      var childElement = GetElement( rootElement, elementName );

      if ( childElement == null ) {
        return new List<string>();
      }

      return childElement.Value.Split( ';' ).Select( s => s.Trim() ).WhereNotNullOrEmpty().ToList();
    }
  }
}
