using System.IO;
using System.Text;
using CsMerge.Core;
using NUnit.Framework;

using NuGet;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.PackagingCore;
using NuGet.Versioning;

using NuGetHelpers;

using Project;

namespace PackagesMerge.Test {

  [TestFixture]
  public class PackageIdentityParseTest {

    public class Utf8StringWriter: StringWriter {
      public override Encoding Encoding {
        get { return Encoding.UTF8; }
      }
    }

    [Test]
    public void PackageNameContainsNumbers() {
      ProjectPackages projectPackages = new ProjectPackages( new[] {
        new PackageReference( new PackageIdentity( "C.1V1", NuGetVersion.Parse( "1.2.3" ) ), NuGetFramework.Parse( "net45" ), false )
      } );

      Assert.That( projectPackages.IsPackageReferenced( new Reference( string.Empty, null, null, "C.1V1.1.2.3" ).HintPath ), Is.True );
      Assert.That( projectPackages.IsPackageReferenced( new Reference( string.Empty, null, null, "C.2V1.1.2.3" ).HintPath ), Is.False );
    }
  }
}