using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace PackagesMerge.Test {
  public static class ResourceHelper {

    public static XDocument LoadXml( string key ) {
      string xml = Load( key );

      return XDocument.Parse( xml );
    }

    public static string Load( string key ) {
      var resourceName = "PackagesMerge.Test." + key;
      using ( var s = Assembly.GetExecutingAssembly().GetManifestResourceStream( resourceName ) ) {
        if ( s == null ) {
          throw new InvalidOperationException( "Resource not found: " + resourceName );
        }

        var ms = new MemoryStream();
        s.CopyTo( ms );
        ms.Seek( 0, SeekOrigin.Begin );
        return new StreamReader( ms ).ReadToEnd();
      }
    }
  }
}
