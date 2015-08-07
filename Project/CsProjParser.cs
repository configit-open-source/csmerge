using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Project;

namespace CsMerge.Core.Parsing {
  public class CsProjParser {

    public static ProjectFile Parse( string name, Stream from ) {
      var doc = XDocument.Load( from );
      return Parse( name, doc );
    }

    public static ProjectFile Parse( string name, XDocument doc ) {
      if ( doc == null || doc.Root == null ) {
        throw new ArgumentException( "Stream did not contain a valid XML document", "doc" );
      }

      if ( doc.Root.Name.LocalName != "Project" ) {
        throw new ArgumentException( "Stream did not contain a project", "doc" );
      }

      return new ProjectFile(
        name,
        doc.Root.Elements( doc.Root.Name.Namespace.GetName( "ItemGroup" ) ).Select( ParseItemGroup ).ToList().AsReadOnly()
      );
    }

    private static ItemGroup ParseItemGroup( XElement itemGroupElement ) {
      return new ItemGroup(
        itemGroupElement
          .Elements()
          .Select( e => e.ParseAsItem() )
          .WhereNotNull()
          .ToList()
          .AsReadOnly() );
    }
  }
}