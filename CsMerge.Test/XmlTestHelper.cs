using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Cpc.CsMerge.Test {
  public static class XmlTestHelper {
    public static Stream ToStream( this XElement testData ) {
      var memoryStream = new MemoryStream();

      using ( var writer = XmlWriter.Create( memoryStream ) ) {
        testData.WriteTo( writer );
      }
      memoryStream.Seek( 0, SeekOrigin.Begin );
      return memoryStream;
    }
  }
}
