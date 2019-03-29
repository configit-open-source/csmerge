using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using CsMerge.Core.Resolvers;
using LibGit2Sharp;

using Integration;

using Project;

using Reference = Project.Reference;

namespace CsMerge.Core {

  /// <summary>
  /// Merges project files
  /// </summary>
  public class ProjectMerger {

    private readonly CurrentOperation _operation;
    private readonly IConflictResolver<Reference> _referenceResolver;
    private readonly IConflictResolver<PackageReference> _packageReferenceResolver;
    private readonly IConflictResolver<RawItem> _itemResolver;
    private readonly IDuplicateResolver<Reference> _duplicateReferenceResolver;
    private readonly IDuplicateResolver<PackageReference> _duplicatePackageReferenceResolver;
    private readonly IConflictResolver<ProjectReference> _projectReferenceResolver;

    /// <summary>
    /// Creates a new instance of a ProjectMerger class
    /// </summary>
    /// <param name="operation">The operation being performed</param>
    /// <param name="referenceResolver">An object to resolve reference conflicts that cannot be auto resolved.</param>
    /// <param name="projectReferenceResolver">An object to resolve project reference conflicts that cannot be auto resolved.</param>
    /// <param name="packageReferenceResolver">An object to resolve package reference conflicts that cannot be auto resolved.</param>
    /// <param name="itemResolver">An object to resolve item conflicts that cannot be auto resolved.</param>
    /// <param name="duplicateReferenceResolver">An object to resolve duplicate references.</param>
    /// <param name="duplicatePackageReferenceResolver">An object to resolve duplicate package references.</param>
    public ProjectMerger(
      CurrentOperation operation,
      IConflictResolver<ProjectReference> projectReferenceResolver,
      IConflictResolver<Reference> referenceResolver,
      IConflictResolver<PackageReference> packageReferenceResolver,
      IConflictResolver<RawItem> itemResolver,
      IDuplicateResolver<Reference> duplicateReferenceResolver,
      IDuplicateResolver<PackageReference> duplicatePackageReferenceResolver ) {

      _operation = operation;
      _referenceResolver = referenceResolver;
      _packageReferenceResolver = packageReferenceResolver;
      _itemResolver = itemResolver;
      _duplicateReferenceResolver = duplicateReferenceResolver;
      _duplicatePackageReferenceResolver = duplicatePackageReferenceResolver;
      _projectReferenceResolver = projectReferenceResolver;
    }

    private static IEnumerable<T> MergeReferences<T>(
      CurrentOperation operation,
      string filePath, 
      IEnumerable<T> baseReferences, 
      IEnumerable<T> localReferences, 
      IEnumerable<T> incomingReferences,
      IConflictResolver<T> conflictResolver,
      IDuplicateResolver<T> duplicateResolver ) 
      where T: Item {

      var baseRefsList = baseReferences.Distinct().ToList();
      var localRefsList = localReferences.Distinct().ToList();
      var incomingRefsList = incomingReferences.Distinct().ToList();

      // Identify with duplicates
      var baseDuplicates = baseRefsList.ToDuplicatesDictionary();
      var localDuplicates = localRefsList.ToDuplicatesDictionary();
      var incomingDuplicates = incomingRefsList.ToDuplicatesDictionary();

      var duplicateKeys = baseDuplicates.Keys.Union( localDuplicates.Keys ).Union( incomingDuplicates.Keys ).ToHashSet();

      // We must ensure the duplicate dictionaries have items even if the duplicate was in another source. 
      // For example, if local has duplicates, we must populate base and incoming with those references even if those dont have duplicates.
      foreach ( var duplicateKey in duplicateKeys ) {
        EnsureDuplicatesPopulated( duplicateKey, baseDuplicates, baseRefsList );
        EnsureDuplicatesPopulated( duplicateKey, localDuplicates, localRefsList );
        EnsureDuplicatesPopulated( duplicateKey, incomingDuplicates, incomingRefsList );
      }

      var mergedDuplicates = duplicateKeys.Any() ?
        MergeHelper<T>.MergeAllDuplicates( filePath, operation, baseDuplicates, localDuplicates, incomingDuplicates, conflictResolver, duplicateResolver ) :
        new List<T>();

      // Discard packages that are no longer installed 
      var baseByName = baseRefsList.Where( r => !duplicateKeys.Contains( r.Key ) ).ToKeyedDictionary();
      var localByName = localRefsList.Where( r => !duplicateKeys.Contains( r.Key ) ).ToKeyedDictionary();
      var theirByName = incomingRefsList.Where( r => !duplicateKeys.Contains( r.Key ) ).ToKeyedDictionary();

      var mergedNonDuplicates = MergeHelper<T>.MergeAll( filePath, operation, baseByName, localByName, theirByName, conflictResolver );

      return mergedNonDuplicates.Union( mergedDuplicates );
    }

    private static void EnsureDuplicatesPopulated<T>( string duplicateKey, IDictionary<string, IEnumerable<T>> duplicatesDictionary, IEnumerable<T> allReferences ) 
    where T: Item {
      if ( duplicatesDictionary.ContainsKey( duplicateKey ) ) {
        return;
      }

      duplicatesDictionary.Add( duplicateKey, allReferences.Where( r => r.Key == duplicateKey ) );
    }

    /// <summary>
    /// Merges project files and references
    /// </summary>
    /// <param name="projectPackages">Nuget package information</param>
    /// <param name="baseDocument">The base project xml</param>
    /// <param name="localDocument">The local project xml</param>
    /// <param name="incomingDocument">The incoming project xml</param>
    /// <param name="filePath">The file containing the conflicts being merged</param>
    /// <returns>Returns the merged items</returns>
    public IEnumerable<Item> Merge(
      string filePath,
      ProjectPackages projectPackages,
      XDocument baseDocument,
      XDocument localDocument,
      XDocument incomingDocument ) {

      var projFileName = Path.GetFileName( filePath );

      var localProj = CsProjParser.Parse( projFileName, localDocument );
      var theirProj = CsProjParser.Parse( projFileName, incomingDocument );
      var baseProj = CsProjParser.Parse( projFileName, baseDocument );
      
      var localRefs = localProj.GetItems<Reference>().ToList();
      var theirRefs = theirProj.GetItems<Reference>().ToList();
      var baseRefs = baseProj.GetItems<Reference>().ToList();
      
      var localPackageReferences = localProj.GetItems<PackageReference>().ToList();
      var theirPackageReferences = theirProj.GetItems<PackageReference>().ToList();
      var basePackageReferences = baseProj.GetItems<PackageReference>().ToList();

      localRefs.ForEach( r => r.ApplyIsResolveOption( projectPackages ) );
      theirRefs.ForEach( r => r.ApplyIsResolveOption( projectPackages ) );
      baseRefs.ForEach( r => r.ApplyIsResolveOption( projectPackages ) );

      var localProjectRefs = localProj.GetItemsDictionary<ProjectReference>();
      var theirProjectRefs = theirProj.GetItemsDictionary<ProjectReference>();
      var baseProjectRefs = baseProj.GetItemsDictionary<ProjectReference>();

      var localItems = localProj.GetItemsDictionary<RawItem>();
      var theirItems = theirProj.GetItemsDictionary<RawItem>();
      var baseItems = baseProj.GetItemsDictionary<RawItem>();

      var resolvedItems = MergeHelper<RawItem>.MergeAll( filePath, _operation, baseItems, localItems, theirItems, _itemResolver );

      var resolvedReferences = MergeReferences( _operation, filePath, baseRefs, localRefs, theirRefs, _referenceResolver, _duplicateReferenceResolver );

      var resolvedPackageReferences = MergeReferences( _operation, filePath, basePackageReferences, localPackageReferences, theirPackageReferences, _packageReferenceResolver, _duplicatePackageReferenceResolver );

      var resolvedProjectReferences = MergeHelper<ProjectReference>.MergeAll(
        filePath, _operation, baseProjectRefs, localProjectRefs, theirProjectRefs, _projectReferenceResolver );

      return resolvedItems.Cast<Item>().Concat( resolvedReferences ).Concat( resolvedPackageReferences ).Concat( resolvedProjectReferences );
    }
  }
}