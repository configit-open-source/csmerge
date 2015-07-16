using CsMerge.Core;
using CsMerge.Core.Resolvers;
using NUnit.Framework;
using PackagesMerge.Test.Resolvers;

namespace PackagesMerge.Test {

  [TestFixture]
  public class PackageConflictResolverTest {

    private Package _packageV0;
    private Package _packageV1;
    private Package _packageV2;
    private Package _packageV2UserInstalled;
    private const string PackageKey = "MP";

    [TestFixtureSetUp]
    public void TestFixtureSetUp() {
      _packageV0 = new Package( PackageKey, new PackageVersion( 1, 0, 0 ), ".net45" );
      _packageV1 = new Package( PackageKey, new PackageVersion( 1, 0, 1 ), ".net45" );
      _packageV2 = new Package( PackageKey, new PackageVersion( 1, 0, 2 ), ".net45" );
      _packageV2UserInstalled = new Package( PackageKey, new PackageVersion( 1, 0, 2 ), ".net45", userInstalled : true );
    }

    [Test]
    public void AutoMergesHighestVersionWhenNoOtherChanges() {

      PackageConflictResolver resolver = new PackageConflictResolver( new ExceptionResolver<Package>() );

      var conflict = new Conflict<Package>( PackageKey, _packageV0, _packageV1, _packageV2 );

      var result = resolver.Resolve( conflict );

      Assert.That( result, Is.EqualTo( _packageV2 ) );
    }

    [Test]
    public void UsesDefaultResolverIfOtherDifferencesExist() {

      var defaultResolver = new TestConflictResolver<Package>( ConflictItemType.Base );

      PackageConflictResolver resolver = new PackageConflictResolver( defaultResolver );

      var conflict = new Conflict<Package>( PackageKey, _packageV0, _packageV1, _packageV2UserInstalled );

      var result = resolver.Resolve( conflict );

      Assert.That( defaultResolver.Called, Is.EqualTo( true ) );
      Assert.That( result, Is.EqualTo( _packageV0 ) );
    }
  }
}
