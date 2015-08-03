using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using CsMerge.Core;

using LibGit2Sharp;

using Integration;

using NUnit.Framework;
using PackagesMerge.Test.Resolvers;

using Project;

using Version = System.Version;
using Reference = Project.Reference;

namespace PackagesMerge.Test {

  [TestFixture]
  public class ProjectMergerTest {

    [Test]
    public void Test() {

      var referenceResolver = new TestConflictResolver<Reference>( ConflictItemType.Local );
      var projectReferenceResolver = new TestConflictResolver<ProjectReference>( ConflictItemType.Local );
      var itemResolver = new TestConflictResolver<RawItem>( ConflictItemType.Local );
      var duplicateResolver = new TestDuplicateResolver<Reference>( ConflictItemType.Local );

      var projectMerger = new ProjectMerger( CurrentOperation.Merge, projectReferenceResolver, referenceResolver, itemResolver, duplicateResolver );

      string packagesConfigPath = Path.GetFullPath( @"..\..\TestFiles\src\Project" );

      var projectPackages = new ProjectPackages( packagesConfigPath, @"..\..\Packages" );

      var baseDocument = XDocument.Load( Path.Combine( packagesConfigPath, "Base.csproj" ) );
      var localDocument = XDocument.Load( Path.Combine( packagesConfigPath, "Local.csproj" ) );
      var incomingDocument = XDocument.Load( Path.Combine( packagesConfigPath, "Incoming.csproj" ) );

      var items = projectMerger.Merge( "Test.csproj", projectPackages, baseDocument, localDocument, incomingDocument ).ToList();

      var itemKeys = items.OrderBy( i => i.Action ).ThenBy( i => i.Key ).Select( i => i.Key ).ToList();

      var duplicates = itemKeys.GroupBy( i => i ).Where( g => g.Count() > 1 ).ToList();

      Assert.That( duplicates, Is.Empty );

      var expectedItems = new[] {

        // Project references
        "00000000-0000-0000-0000-000000000001", // UnChanged - auto resolved
        // "00000000-0000-0000-0000-000000000002", // DeleteInBoth - auto resolved to delete
        // "00000000-0000-0000-0000-000000000003", // DeleteInLocal - auto resolved to delete
        // "00000000-0000-0000-0000-000000000004", // DeleteInIncoming - auto resolved to delete
        "00000000-0000-0000-0000-000000000005", // DeleteIncomingUpdateLocal - resolved by projectReferenceResolver to local (updated)
        // "00000000-0000-0000-0000-000000000006", // DeleteLocalUpdateIncoming - resolved by projectReferenceResolver to local (Deleted)
        "00000000-0000-0000-0000-000000000007", // UpdateLocal - auto resolved
        "00000000-0000-0000-0000-000000000008", // UpdateIncoming - auto resolved
        "00000000-0000-0000-0000-000000000009", // UpdateIncomingAndLocal.Identical - auto resolved
        "00000000-0000-0000-0000-000000000010", // UpdateIncomingAndLocal.Different
        "00000000-0000-0000-0000-000000000011", // Duplicate.Identical - auto resolved
        //"00000000-0000-0000-0000-000000000012", // Duplicate.Different - currently not supported and commented out
        "00000000-0000-0000-0000-000000000020", // AddedInLocal - auto resolved
        "00000000-0000-0000-0000-000000000021", // AddedInIncoming - auto resolved
        "00000000-0000-0000-0000-000000000022", // AddedInBoth.Identical - auto resolved
        "00000000-0000-0000-0000-000000000023", // AddedInBoth.Different - resolved by projectReferenceResolver to local

        // Items (these only have a key so there is no way to update (as keys wont be the same anymore and will be considered different items)
        "NoChange.cs",
        //"DeleteInBoth.cs",
        //"DeleteInLocal.cs",
        //"DeleteInIncoming.cs",
        "Duplicate.Identical.cs",
        //"Duplicate.Different.cs", //currently not supported
        "AddedInLocal.cs",
        "AddedInIncoming.cs",
        "AddedInBoth\\Identical.cs",
        "Properties\\AssemblyInfo.cs",
        "app.config",
        "packages.config",
        
        // References
        "CsMerge.Packages.NoChanges", 
        // "CsMerge.Packages.DeletedInBoth" - should not be in result as auto resolved to deleted.
        // "CsMerge.Packages.DeletedInLocal" - should not be in result as auto resolved to deleted.
        // "CsMerge.Packages.DeletedInIncoming" - should not be in result as auto resolved to deleted.
        // "CsMerge.Packages.DeletedInLocalUpdatedInIncoming" - should not be in result as referenceResolver will resolve to local (deleted).
        "CsMerge.Packages.DeletedInIncomingUpdatedInLocal", // Resolved by referenceResolver to local
        "CsMerge.Packages.UpdatedInLocal", // auto resolved
        "CsMerge.Packages.UpdatedInIncoming", // auto resolved
        "CsMerge.Packages.UpdatedInBoth.Identical", // auto resolved
        "CsMerge.Packages.UpdatedInBoth.Different.VersionChangeOnly", // auto resolved to highest version
        "CsMerge.Packages.UpdatedInBoth.Different.OtherChanges", // Resolved by referenceResolver to local
        "CsMerge.Packages.Duplicate.Identical", // Appears only once. Auto resolved
        "CsMerge.Packages.Duplicate.Different.VersionChangeOnly", // Appears only once. Resolved by duplicateResolver to local[0]
        "CsMerge.Packages.Duplicate.Different.OtherChanges" // Appears only once. Resolved by duplicateResolver to local[0]
      };

      Assert.That( itemKeys, Is.EquivalentTo( expectedItems ) );

      AssertReference( items, "CsMerge.Packages.NoChanges", false, true, Version( 1 ) );
      AssertReference( items, "CsMerge.Packages.DeletedInIncomingUpdatedInLocal", false, true, Version( 1, 0, 0, 1 ) );
      AssertReference( items, "CsMerge.Packages.UpdatedInLocal", false, true, Version( 1, 0, 0, 1 ) );
      AssertReference( items, "CsMerge.Packages.UpdatedInIncoming", false, true, Version( 1, 0, 0, 2 ) );
      AssertReference( items, "CsMerge.Packages.UpdatedInBoth.Identical", false, true, Version( 1, 0, 0, 1 ) );
      AssertReference( items, "CsMerge.Packages.UpdatedInBoth.Different.VersionChangeOnly", false, true, Version( 1, 0, 0, 2 ) );
      AssertReference( items, "CsMerge.Packages.UpdatedInBoth.Different.OtherChanges", false, null, Version( 1, 0, 0, 2 ) ); //Resolved to local
      AssertReference( items, "CsMerge.Packages.Duplicate.Identical", false, true, Version( 1 ) );
      AssertReference( items, "CsMerge.Packages.Duplicate.Different.VersionChangeOnly", false, true, Version( 1, 0, 0, 1 ) ); // Resolved by test resolver to local[0]
      AssertReference( items, "CsMerge.Packages.Duplicate.Different.OtherChanges", false, null, Version( 1, 0, 0, 2 ) ); // Resolved by test resolver to local - first valid option.

      AssertProjectReference( items, "00000000-0000-0000-0000-000000000001", "CsMerge.OtherProject.UnChanged", @"..\..\OtherProjects\CsMerge.OtherProject.UnChanged.csproj" );
      AssertProjectReference( items, "00000000-0000-0000-0000-000000000005", "CsMerge.OtherProject.DeleteIncomingUpdateLocal.Local", @"..\..\OtherProjects\CsMerge.OtherProject.DeleteIncomingUpdateLocal.csproj" );
      AssertProjectReference( items, "00000000-0000-0000-0000-000000000007", "CsMerge.OtherProject.UpdateLocal.Local", @"..\..\OtherProjects\CsMerge.OtherProject.UpdateLocal.csproj" );
      AssertProjectReference( items, "00000000-0000-0000-0000-000000000008", "CsMerge.OtherProject.UpdateIncoming.Incoming", @"..\..\OtherProjects\CsMerge.OtherProject.UpdateIncoming.csproj" );
      AssertProjectReference( items, "00000000-0000-0000-0000-000000000009", "CsMerge.OtherProject.UpdateIncomingAndLocal.Identical.Updated", @"..\..\OtherProjects\CsMerge.OtherProject.UpdateIncomingAndLocal.Identical.csproj" );
      AssertProjectReference( items, "00000000-0000-0000-0000-000000000010", "CsMerge.OtherProject.UpdateIncomingAndLocal.Different.Local", @"..\..\OtherProjects\CsMerge.OtherProject.UpdateIncomingAndLocal.Different.csproj" );
      AssertProjectReference( items, "00000000-0000-0000-0000-000000000011", "CsMerge.OtherProject.Duplicate.Identical", @"..\..\OtherProjects\CsMerge.OtherProject.Duplicate.Identical.csproj" );
      AssertProjectReference( items, "00000000-0000-0000-0000-000000000020", "CsMerge.OtherProject.AddedInLocal", @"..\..\OtherProjects\CsMerge.OtherProject.AddedInLocal.csproj" );
      AssertProjectReference( items, "00000000-0000-0000-0000-000000000021", "CsMerge.OtherProject.AddedInIncoming", @"..\..\OtherProjects\CsMerge.OtherProject.AddedInIncoming.csproj" );
      AssertProjectReference( items, "00000000-0000-0000-0000-000000000022", "CsMerge.OtherProject.AddedInLocal.Identical", @"..\..\OtherProjects\CsMerge.OtherProject.AddedInBoth.Identical.csproj" );
      AssertProjectReference( items, "00000000-0000-0000-0000-000000000023", "CsMerge.OtherProject.AddedInLocal.Identical.Local", @"..\..\OtherProjects\CsMerge.OtherProject.AddedInBoth.Different.csproj" );

      Assert.That( projectReferenceResolver.Resolutions.Keys, Is.EquivalentTo( new[] {
        "00000000-0000-0000-0000-000000000005", // DeleteIncomingUpdateLocal
        "00000000-0000-0000-0000-000000000006", // DeleteLocalUpdateIncoming
        "00000000-0000-0000-0000-000000000010", // UpdateIncomingAndLocal.Different
        "00000000-0000-0000-0000-000000000023"  // AddedInBoth.Different

      } ) );

      Assert.That( referenceResolver.Resolutions.Keys, Is.EquivalentTo( new[] {
        "CsMerge.Packages.UpdatedInBoth.Different.OtherChanges",
        "CsMerge.Packages.DeletedInIncomingUpdatedInLocal",
        "CsMerge.Packages.DeletedInLocalUpdatedInIncoming"
      } ) );

      Assert.That( duplicateResolver.Resolutions.Keys, Is.EquivalentTo( new[] {
        //"CsMerge.Packages.Duplicate.Identical" - Auto resolved as duplicates are identical.
        "CsMerge.Packages.Duplicate.Different.VersionChangeOnly",
        "CsMerge.Packages.Duplicate.Different.OtherChanges"
      } ) );
    }

    private static void AssertProjectReference( List<Item> items, string key, string expectedName, string expectedPath ) {
      var reference = GetItem<ProjectReference>( items, key );

      Assert.That( reference.Name, Is.EqualTo( expectedName ) );
      Assert.That( reference.CsProjPath, Is.EqualTo( expectedPath ) );
    }

    private static Version Version( int major, int minor = 0, int build = 0, int revision = 0 ) {
      return new Version( major, minor, build, revision );
    }

    private static void AssertReference( IEnumerable<Item> items, string key, bool? specificVersion, bool? isPrivate, Version version ) {
      var reference = GetItem<Reference>( items, key );

      Assert.That( reference.SpecificVersion, Is.EqualTo( specificVersion ), "Unexpected SpecificVersion value for " + key );
      Assert.That( reference.ReferenceAssemblyVersion, Is.EqualTo( version ), "Unexpected Version value for " + key );
      Assert.That( reference.Private, Is.EqualTo( isPrivate ), "Unexpected Private value for " + key );
    }

    private static T GetItem<T>( IEnumerable<Item> items, string key ) where T: Item {
      return items.OfType<T>().Single( i => i.Key == key );
    }
  }
}