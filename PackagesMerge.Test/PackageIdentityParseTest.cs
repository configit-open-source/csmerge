using NUnit.Framework;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Integration;

using Project;
using PackageReference = NuGet.Packaging.PackageReference;

namespace PackagesMerge.Test {

  [TestFixture]
  public class PackageIdentityParseTest {
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