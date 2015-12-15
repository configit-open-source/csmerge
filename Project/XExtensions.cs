using System.Collections.Generic;
using System.Xml.Linq;

namespace Project {
  public static class XExtensions {

    public static XElement SameNsElement( this XElement parent, string name ) {
      return parent.Element( parent.Name.Namespace.GetName( name ) );
    }

    public static IEnumerable<XElement> SameNsElements( this XElement parent, string name ) {
      return parent.Elements( GetSameNsName( parent, name ) );
    }

    public static XName GetSameNsName( this XElement parent, string name ) {
      return parent.Name.Namespace.GetName( name );
    }

    public static string GetValueOrNull( this XElement element ) {
      return element == null ? null : element.Value;
    }

    public static bool? GetBoolOrNull( this XElement element ) {
      return element == null ? (bool?) null : bool.Parse( element.Value );
    }
  }
}