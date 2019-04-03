using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using NuGet.Packaging;

using Project;
using PackageReference = NuGet.Packaging.PackageReference;

namespace CsMerge.Core {

  public static class SerialisationHelper {

    public static IConflictableItem ParseAsConflictableItem( this XElement itemElement ) {

      if ( string.Equals( itemElement.Name.LocalName, "package", StringComparison.InvariantCultureIgnoreCase ) ) {
        return (ConfigitPackageReference) itemElement.ParseAsPackage();
      }

      return itemElement.ParseAsItem();
    }

    public static PackageReference ParseAsPackage( this XElement e ) {
      var ns = e.Name.Namespace;

      var document = new XDocument();
      document.Add( new XElement( ns.GetName( "packages" ), e ) );

      var reader = new PackagesConfigReader( document );
      return reader.GetPackages().Single();
    }

    public static Item ParseAsItem( this XElement itemElement ) {

      var includeAttribute = itemElement.Attribute( "Include" );

      if ( includeAttribute == null ) {
        return null;
      }

      var include = includeAttribute.Value;

      var xNamespace = itemElement.Name.Namespace;

      switch ( itemElement.Name.LocalName ) {
        case "Reference":
          return new Reference( itemElement );

        case "ProjectReference":
          return new ProjectReference( include,
                                       Guid.Parse( itemElement.Element( xNamespace.GetName( "Project" ) ).Value ),
                                       itemElement.Element( xNamespace.GetName( "Name" ) ).Value, itemElement );
        
        case "PackageReference":
          return new Project.PackageReference( itemElement );
        
        default:
          return new RawItem( itemElement, include );
      }
    }

    public static void WriteXml( this XNode root, string path ) {
      using ( var textWriter = new StreamWriter( path ) ) {
        root.WriteXml( textWriter );
      }
    }

    public static void WriteXml( this IEnumerable<XNode> nodes, string path, XmlWriterSettings settings = null ) {
      using ( var textWriter = new StreamWriter( path ) ) {
        nodes.WriteXml( textWriter, settings );
      }
    }

    public static void WriteXml( this XNode element, TextWriter writer, XmlWriterSettings settings = null ) {
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

    public static void WriteXml( this IEnumerable<XNode> nodes, TextWriter writer, XmlWriterSettings settings = null ) {

      var xmlWriterSettings = settings ?? DefaultWriterSettings();

      using ( var xmlWriter = XmlWriter.Create( writer, xmlWriterSettings ) ) {
        foreach ( var node in nodes ) {
          node.WriteTo( xmlWriter );
        }
      }
    }

    public static XmlWriterSettings DefaultWriterSettings() {
      return new XmlWriterSettings {
        Encoding = Encoding.UTF8,
        CloseOutput = true,
        NewLineChars = "\n",
        Indent = true,
        NamespaceHandling = NamespaceHandling.OmitDuplicates,
        ConformanceLevel = ConformanceLevel.Document,
      };
    }
  }
}
