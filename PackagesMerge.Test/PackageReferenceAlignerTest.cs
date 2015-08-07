using System.Reflection;

using CsMerge;

using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.PackagingCore;
using NuGet.Versioning;

using NUnit.Framework;

using Project;

namespace PackagesMerge.Test {

  [TestFixture]
  public class PackageReferenceAlignerTest {

    [Test]
    public void ReconstructsRelativePath() {
      var fixedReference = PackageReferenceAligner.TryUpdateReference(
        @"..\..\packages\pid.2.0.0.0\lib\net45\pid.dll",
        new PackageIdentity( "pid", NuGetVersion.Parse("1.0.0.0") ), 
        new PackageReference(
          new PackageIdentity( "pid", NuGetVersion.Parse( "2.0.0.0" ) ), NuGetFramework.Parse( "net45" ) ),
        new Reference( string.Empty, null, null, @"..\..\packages\pid.1.0.0.0\lib\net45\pid.dll" ),
        p => new AssemblyName( "Pid, Version=0.21.0.176, Culture=neutral, processorArchitecture=MSIL" ) );

      Assert.That( fixedReference.Item2.HintPath, Is.EqualTo( @"..\..\packages\pid.2.0.0.0\lib\net45\pid.dll" ) );
    }

    [Test]
    public void RestoresConfiguration() {
      var fixedReference = PackageReferenceAligner.TryUpdateReference(
        @"..\..\packages\pid.2.0.0.0\lib\Release\net45\pid.dll",
        new PackageIdentity( "pid", NuGetVersion.Parse( "1.0.0.0" ) ), 
        new PackageReference(
          new PackageIdentity( "pid", NuGetVersion.Parse( "2.0.0.0" ) ), NuGetFramework.Parse( "net45" ) ),
        new Reference( string.Empty, null, null, @"..\..\packages\pid.1.0.0.0\$(Configuration)\lib\net45\pid.dll" ),
        p => new AssemblyName( "Pid, Version=0.21.0.176, Culture=neutral, processorArchitecture=MSIL" ) );

      Assert.That( fixedReference.Item2.HintPath, Is.EqualTo( @"..\..\packages\pid.2.0.0.0\$(Configuration)\lib\net45\pid.dll" ) );
    }
  }
}