using System.Linq;

using CsMerge.Core;

using LibGit2Sharp;

using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.PackagingCore;
using NuGet.Versioning;

using NuGetHelpers;

using NUnit.Framework;

using PackagesMerge.Test.Resolvers;

namespace PackagesMerge.Test {
  [TestFixture]
  public class PackagesMergeTest {

    private ConfigitPackageReference[] _packageV0;
    private ConfigitPackageReference[] _packageV1;
    private ConfigitPackageReference[] _packageV2;
    private ConfigitPackageReference[] _packageV2Net46;
    private ConfigitPackageReference[] _packageEmpty;

    private static ConfigitPackageReference[] CreatePackage( string id, string targetFramework, string allowedVersions, string version ) {
      return new[] {
        (ConfigitPackageReference)
        new PackageReference( new PackageIdentity( id, new NuGetVersion( version )),
          NuGetFramework.Parse(targetFramework), false, false, false, allowedVersions != null ? VersionRange.Parse( allowedVersions ) : null )
      };
    }

    [TestFixtureSetUp]
    public void TestFixtureSetUp() {
      _packageV0 = CreatePackage( "MP", ".net45", null, "1.0.0" );
      _packageV1 = CreatePackage( "MP", ".net45", null, "1.0.1" );
      _packageV2 = CreatePackage( "MP", ".net45", null, "1.0.2" );
      _packageV2Net46 = CreatePackage( "MP", ".net46", null, "1.0.2" );
      _packageEmpty = new ConfigitPackageReference[0];
    }

    [Test]
    public void NoChanges() {

      var resolver = new ExceptionResolver<ConfigitPackageReference>();
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      var result = merger.Merge( "TestFilePath", _packageV0, _packageV0, _packageV0 ).ToList();

      Assert.That( result, Is.EquivalentTo( _packageV0 ) );
    }

    [Test]
    public void TheirsUpdated() {

      var resolver = new ExceptionResolver<ConfigitPackageReference>();
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      var result = merger.Merge( "TestFilePath", _packageV0, _packageV0, _packageV1 ).ToList();

      Assert.That( result, Is.EquivalentTo( _packageV1 ) );
    }

    [Test]
    public void MineUpdated() {

      var resolver = new ExceptionResolver<ConfigitPackageReference>();
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      var result = merger.Merge( "TestFilePath", _packageV0, _packageV1, _packageV0 ).ToList();

      Assert.That( result, Is.EquivalentTo( _packageV1 ) );
    }

    [Test]
    public void BothUpdatedAutoResolved() {

      var resolver = new TestConflictResolver<ConfigitPackageReference>( ConflictItemType.Local );
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      // CurrentOperation.Merge causes local to be set to mine
      var result = merger.Merge( "TestFilePath", _packageV0, _packageV1, _packageV2 ).ToList();

      Assert.That( resolver.Called, Is.EqualTo( false ) );
      Assert.That( result, Is.EquivalentTo( _packageV2 ) );
    }

    [Test]
    public void BothUpdated() {

      var resolver = new TestConflictResolver<ConfigitPackageReference>( ConflictItemType.Local );
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      // CurrentOperation.Merge causes local to be set to mine
      var result = merger.Merge( "TestFilePath", _packageV0, _packageV1, _packageV2Net46 ).ToList();

      Assert.That( resolver.Called, Is.EqualTo( true ) );
      Assert.That( result, Is.EquivalentTo( _packageV1 ) );
    }

    [Test]
    public void TheirsDeletedMineUpdated_ResolveMine() {

      var resolver = new TestConflictResolver<ConfigitPackageReference>( ConflictItemType.Local );
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      var result = merger.Merge( "TestFilePath", _packageV0, _packageV1, _packageEmpty ).ToList();

      Assert.That( resolver.Called, Is.EqualTo( true ) );
      Assert.That( result, Is.EquivalentTo( _packageV1 ) );
    }
    [Test]
    public void TheirsDeletedMineUpdated_ResolveTheirs() {

      var resolver = new TestConflictResolver<ConfigitPackageReference>( ConflictItemType.Incoming );
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      var result = merger.Merge( "TestFilePath", _packageV0, _packageV1, _packageEmpty ).ToList();

      Assert.That( resolver.Called, Is.EqualTo( true ) );
      Assert.That( result, Is.Empty );
    }

    [Test]
    public void MineDeletedTheirsUpdated_ResolveTheirs() {

      var resolver = new TestConflictResolver<ConfigitPackageReference>( ConflictItemType.Incoming );
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      var result = merger.Merge( "TestFilePath", _packageV0, _packageEmpty, _packageV1 ).ToList();

      Assert.That( resolver.Called, Is.EqualTo( true ) );
      Assert.That( result, Is.EquivalentTo( _packageV1 ) );
    }

    [Test]
    public void MineDeletedTheirsUpdated_ResolveMine() {

      var resolver = new TestConflictResolver<ConfigitPackageReference>( ConflictItemType.Local );
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      var result = merger.Merge( "TestFilePath", _packageV0, _packageEmpty, _packageV1 ).ToList();

      Assert.That( resolver.Called, Is.EqualTo( true ) );
      Assert.That( result, Is.Empty );
    }

    [Test]
    public void BothAddedAutoResolved() {
      var resolver = new TestConflictResolver<ConfigitPackageReference>( ConflictItemType.Local );
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      var result = merger.Merge( "TestFilePath", _packageEmpty, _packageV1, _packageV2 ).ToList();

      Assert.That( resolver.Called, Is.EqualTo( false ) );
      Assert.That( result, Is.EquivalentTo( _packageV2 ) ); // Auto resolved to highest version
    }

    [Test]
    public void BothAddedUserResolved() {

      var resolver = new TestConflictResolver<ConfigitPackageReference>( ConflictItemType.Local );
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      var result = merger.Merge( "TestFilePath", _packageEmpty, _packageV1, _packageV2Net46 ).ToList();

      Assert.That( resolver.Called, Is.EqualTo( true ) );
      Assert.That( result, Is.EquivalentTo( _packageV1 ) );
    }

    [Test]
    public void BothDeleted() {

      var resolver = new ExceptionResolver<ConfigitPackageReference>();
      var merger = new PackagesConfigMerger( CurrentOperation.Merge, resolver );

      var result = merger.Merge( "TestFilePath", _packageV1, _packageEmpty, _packageEmpty ).ToList();

      Assert.That( result, Is.Empty );
    }

  }
}
