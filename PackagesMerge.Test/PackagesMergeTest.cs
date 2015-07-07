using System;
using System.Linq;
using System.Linq.Expressions;

using CsMerge;
using CsMerge.Core;

using LibGit2Sharp;

using NUnit.Framework;

namespace PackagesMerge.Test {
  [TestFixture]
  public class PackagesMergeTest {

    [Test]
    public void NoChanges() {
      var allowedVersions = new PackageVersion( 1, 0, 0 );

      var result = new PackagesConfigMerger( CurrentOperation.Merge ).Merge(
        new[] { new Package( "MP", allowedVersions, ".net45" ) },
        new[] { new Package( "MP", allowedVersions, ".net45" ) },
        new[] { new Package( "MP", allowedVersions, ".net45" ) }, pc => pc.Local
      ).ToList();

      Assert.That( result, Is.EquivalentTo( new[] {
        new Package( "MP", allowedVersions, ".net45" )
      } ) );
    }

    [Test]
    public void TheirsUpdated() {
      var result = new PackagesConfigMerger( CurrentOperation.Merge ).Merge(
        new[] { new Package( "MP",  new PackageVersion( 1, 0, 0 ), ".net45" ) },
        new[] { new Package( "MP",  new PackageVersion( 1, 0, 0 ), ".net45" ) },
        new[] { new Package( "MP",  new PackageVersion( 1, 0, 1 ), ".net45" ) },
        pc => { throw new Exception( "Resolver was called but shouldn't have been." ); } // This should be auto resolved without calling the resolver.
      ).ToList();

      Assert.That( result, Is.EquivalentTo( new[] {
        new Package( "MP",  new PackageVersion( 1, 0, 1 ), ".net45" )
      } ));
    }

    [Test]
    public void MineUpdated() {
      var result = new PackagesConfigMerger( CurrentOperation.Merge ).Merge(
        new[] { new Package( "MP", new PackageVersion( 1, 0, 0 ), ".net45" ) },
        new[] { new Package( "MP", new PackageVersion( 1, 0, 1 ), ".net45" ) },
        new[] { new Package( "MP", new PackageVersion( 1, 0, 0 ), ".net45" ) }, 
        pc => { throw new Exception( "Resolver was called but shouldn't have been." ); } // This should be auto resolved without calling the resolver.
      ).ToList();

      Assert.That( result, Is.EquivalentTo( new[] {
        new Package( "MP",  new PackageVersion( 1, 0, 1 ), ".net45" )
      } ) );
    }
    
    [Test]
    public void TheirsAndMineUpdated() {
      var result = new PackagesConfigMerger( CurrentOperation.Merge ).Merge(
        new[] { new Package( "MP", new PackageVersion( 1, 0, 0 ), ".net45" ) },
        new[] { new Package( "MP", new PackageVersion( 1, 0, 1 ), ".net45" ) },
        new[] { new Package( "MP", new PackageVersion( 1, 0, 2 ), ".net45" ) }, pc => pc.Local
      ).ToList();

      Assert.That( result, Is.EquivalentTo( new[] {
        new Package( "MP",  new PackageVersion( 1, 0, 2 ), ".net45" )
      } ) );
    }

    [Test]
    public void TheirsDeletedMineUpdated_ResolveMine() {
      var result = new PackagesConfigMerger( CurrentOperation.Merge ).Merge(
        new[] { new Package( "MP", new PackageVersion( 1, 0, 0 ), ".net45" ) },
        new[] { new Package( "MP", new PackageVersion( 1, 0, 1 ), ".net45" ) },
        new Package[0],
        pc => pc.Local
      ).ToList();

      Assert.That( result, Is.EquivalentTo( new[] {
        new Package( "MP",  new PackageVersion( 1, 0, 1 ), ".net45" )
      } ) );
    }

    [Test]
    public void TheirsDeletedMineUpdated_ResolveTheirs() {
      var result = new PackagesConfigMerger( CurrentOperation.Merge ).Merge(
        new[] { new Package( "MP",  new PackageVersion( 1, 0, 0 ), ".net45" ) },
        new[] { new Package( "MP",  new PackageVersion( 1, 0, 1 ), ".net45" ) },
        new Package[0],
        pc => pc.Incoming
      ).ToList();

      Assert.That( result, Is.Empty );
    }

    [Test]
    public void MineDeletedTheirsUpdated_ResolveTheirs() {
      var result = new PackagesConfigMerger( CurrentOperation.Merge ).Merge(
        new[] { new Package( "MP",  new PackageVersion( 1, 0, 0 ), ".net45" ) },
        new Package[0],
        new[] { new Package( "MP",  new PackageVersion( 1, 0, 1 ), ".net45" ) },
        pc => pc.Incoming
      ).ToList();

      Assert.That( result, Is.EquivalentTo( new[] {
        new Package( "MP",  new PackageVersion( 1, 0, 1 ), ".net45" )
      } ));
    }

    [Test]
    public void MineDeletedTheirsUpdated_ResolveMine() {
      var result = new PackagesConfigMerger( CurrentOperation.Merge ).Merge(
        new[] { new Package( "MP",  new PackageVersion( 1, 0, 0 ), ".net45" ) },
        new Package[0],
        new[] { new Package( "MP",  new PackageVersion( 1, 0, 1 ), ".net45" ) },
        pc => pc.Local
      ).ToList();

      Assert.That( result, Is.Empty );
    }

    [Test]
    public void BothAdded() {
      var result = new PackagesConfigMerger( CurrentOperation.Merge ).Merge(
        new Package[0],
        new[] { new Package( "MP", new PackageVersion( 1, 0, 1 ), ".net45" ) },
        new[] { new Package( "MP", new PackageVersion( 1, 0, 2 ), ".net45" ) },
        pc => pc.Local
      ).ToList();

      Assert.That( result, Is.EquivalentTo(
        new[] { new Package( "MP", new PackageVersion( 1, 0, 2 ), ".net45" ) } ) );
    }

    [Test]
    public void BothDeleted() {
      var result = new PackagesConfigMerger( CurrentOperation.Merge ).Merge(
        new[] { new Package( "MP",  new PackageVersion( 1, 0, 1 ), ".net45" ) },
        new Package[0],
        new Package[0],
        pc => pc.Local
      ).ToList();

      Assert.That( result, Is.Empty );
    }

  }
}
