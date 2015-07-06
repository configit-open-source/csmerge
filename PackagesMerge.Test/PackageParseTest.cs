using System.IO;
using System.Text;

using CsMerge;
using CsMerge.Core;

using NUnit.Framework;

namespace PackagesMerge.Test {
  [TestFixture]
  public class PackageParseTest {

    public class Utf8StringWriter: StringWriter {
      public override Encoding Encoding {
        get { return Encoding.UTF8; }
      }
    }

    [Test]
    public void Test() {
      var original = File.ReadAllText( @"..\..\packages.config" ).Replace( "\r", "" );

      StringReader reader = new StringReader( original );

      var packages = Package.Read( reader );

      StringWriter writer = new Utf8StringWriter();

      Package.Write( packages, writer );

      var written = writer.ToString().Replace( "\r", "" );

      Assert.That( written, Is.EqualTo( original ) );
    }

    [Test]
    public void PackageNameContainsNumbers() {
      var package = ProjectPackages.PackageFromFolderName( "C.1V1.1.2.3" );

      Assert.That( package.Key, Is.EqualTo( "C.1V1" ) );
      Assert.That( package.Version.ToString(), Is.EqualTo( "1.2.3" ));
    }
  }
}