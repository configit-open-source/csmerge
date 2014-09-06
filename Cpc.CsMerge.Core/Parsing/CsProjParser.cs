using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Cpc.CsMerge.Core.Parsing {
  public class CsProjParser {

    public ProjectFile Parse( string name, Stream from ) {

      var doc = XDocument.Load( from );

      if ( doc == null || doc.Root == null ) {
        throw new ArgumentException( "Stream did not contain a valid XML document", "from" );
      }

      if ( doc.Root.Name != "Project" ) {
        throw new ArgumentException( "Stream did not contain a project", "from" );
      }

      return new ProjectFile(
        name,
        doc.Root.Elements( "ItemGroup" ).Select( ParseItemGroup ).ToList().AsReadOnly()
      );
    }

    private ItemGroup ParseItemGroup( XElement itemGroupElement ) {
      return new ItemGroup( itemGroupElement.Elements().Select( ParseItem ).ToList().AsReadOnly() );
    }

    private Item ParseItem( XElement itemElement ) {
      var include = itemElement.Attribute( "Include" ).Value;

      switch ( itemElement.Name.LocalName ) {
        case "Compile":
          return new Compile( Path.GetDirectoryName( include ), Path.GetFileName( include ) );
        case "Reference":
          var specificVersionAttribute = itemElement.Element( "SpecificVersion" );
          var hintPathAttribute = itemElement.Element( "HintPath" );
          var specificVersion = specificVersionAttribute == null ? (bool?) null : bool.Parse( specificVersionAttribute.Value );

          return new Reference( include, specificVersion, hintPathAttribute == null ? null : hintPathAttribute.Value );

        case "ProjectReference":

          return new ProjectReference( include, Guid.Parse( itemElement.Element( "Project" ).Value ), itemElement.Element( "Name" ).Value );
        default:
          throw new InvalidDataException( "Unrecognised itemGroup member: " + itemElement.Name.LocalName );
      }
    }
  }
}
