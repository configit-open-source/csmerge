using CsMerge.Core;
using CsMerge.Core.Resolvers;
using NUnit.Framework;

using PackagesMerge.Test.Resolvers;

using Project;

namespace PackagesMerge.Test {

  [TestFixture]
  public class PackageConflictResolverTest {

    private ConfigitPackageReference _packageV0;
    private ConfigitPackageReference _packageV1;
    private ConfigitPackageReference _packageV2;
    private ConfigitPackageReference _packageV2UserInstalled;
    private const string PackageKey = "MP";

    [TestFixtureSetUp]
    public void TestFixtureSetUp() {
      _packageV0 = new ConfigitPackageReference( PackageKey, "1.0.0", ".net45" );
      _packageV1 = new ConfigitPackageReference( PackageKey, "1.0.1", ".net45" );
      _packageV2 = new ConfigitPackageReference( PackageKey, "1.0.2", ".net45" );
      _packageV2UserInstalled = new ConfigitPackageReference( PackageKey, "1.0.2", ".net45", userInstalled : true );
    }

    [Test]
    public void AutoMergesHighestVersionWhenNoOtherChanges() {

      PackageConflictResolver resolver = new PackageConflictResolver( new ExceptionResolver<ConfigitPackageReference>() );

      var conflict = new Conflict<ConfigitPackageReference>( "TestFilePath", PackageKey, _packageV0, _packageV1, _packageV2 );

      ConfigitPackageReference result = resolver.Resolve( conflict );

      Assert.That( result, Is.EqualTo( _packageV2 ) );
    }

    [Test]
    public void UsesDefaultResolverIfOtherDifferencesExist() {

      var defaultResolver = new TestConflictResolver<ConfigitPackageReference>( ConflictItemType.Base );

      PackageConflictResolver resolver = new PackageConflictResolver( defaultResolver );

      var conflict = new Conflict<ConfigitPackageReference>( "TestFilePath", PackageKey, _packageV0, _packageV1, _packageV2UserInstalled );

      var result = resolver.Resolve( conflict );

      Assert.That( defaultResolver.Called, Is.EqualTo( true ) );
      Assert.That( result, Is.EqualTo( _packageV0 ) );
    }
  }
}
