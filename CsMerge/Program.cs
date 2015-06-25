using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Cpc.CsMerge.Core;

using LibGit2Sharp;

using NLog;

using PackagesMerge;

using Repository = GitSharp.Repository;

namespace CsMerge {
  public class Program {

    static void Main( string[] args ) {
      if ( args.Length != 1 ) {
        args = new[] { Directory.GetCurrentDirectory() };
      }

      DirectoryInfo folder = new DirectoryInfo( args[0] );
      var logger = LogManager.GetCurrentClassLogger();
      logger.Debug( "Scanning " + folder );

      Repository gitRepo = new Repository( folder.FullName );

      var rootFolder = FindRepoRoot( folder.FullName );

      //using ( var repository = new LibGit2Sharp.Repository( rootFolder ) ) {
      //  //var status = repository.RetrieveStatus( new StatusOptions { Show = StatusShowOption.IndexAndWorkDir } );
      //  foreach(var conflict in repository.Index.Conflicts ) {
      //    Console.WriteLine( conflict.Ours.Path );
      //  }
      //}

      foreach ( var conflict in gitRepo.Status.MergeConflict.Where( p => Path.GetFileName( p ) == "packages.config" ) ) {
        var fullConflictPath = Path.Combine( folder.FullName, conflict );
        logger.Info( "Examining conflict for " + fullConflictPath );

        var baseContent = GitHelper.GetContent( 1, conflict, folder.FullName );
        var localContent = GitHelper.GetContent( 2, conflict, folder.FullName );
        var theirContent = GitHelper.GetContent( 3, conflict, folder.FullName );

        try {
          var result = PackagesConfigMerger.Merge(
          Package.Parse( baseContent ),
          Package.Parse( localContent ),
          Package.Parse( theirContent ),
          UserResolvers.UserResolvePackage ).ToArray();

          Package.Write( result, fullConflictPath );
          using ( var repository = new LibGit2Sharp.Repository( rootFolder ) ) {
            repository.Stage( conflict );
          }
        }
        catch ( OperationAbortedException ) {
          return;
        }
      }

      foreach ( var conflict in gitRepo.Status.MergeConflict.Where( p => p.EndsWith( ".csproj" ) ) ) {
        var fullConflictPath = Path.Combine( folder.FullName, conflict );
        logger.Info( "Examining conflict for " + fullConflictPath );

        var baseContent = GitHelper.GetContent( 1, conflict, folder.FullName );
        var localContent = GitHelper.GetContent( 2, conflict, folder.FullName );
        var theirContent = GitHelper.GetContent( 3, conflict, folder.FullName );

        var conflictFolder = Path.GetDirectoryName( conflict );

        XDocument localDocument = XDocument.Parse( localContent );
        XDocument theirDocument = XDocument.Parse( theirContent );
        XDocument baseDocument = XDocument.Parse( baseContent );

        var projFileName = Path.GetFileName( conflict );

        var projectFolder = Path.Combine( folder.FullName, conflictFolder );

        var packageIndex = new PackagesInfo( projectFolder, FindRelativePathOfPackagesFolder( projectFolder ) );

        Item[] items = ProjectMerger.Merge( projFileName,
          packageIndex,
          baseDocument,
          localDocument,
          theirDocument,
          UserResolvers.UserResolveReference ).ToArray();

        DeleteItemsWithAction( localDocument, ProjectMerger.HandledItems );
        DeleteItemsWithAction( theirDocument, ProjectMerger.HandledItems );
        DeleteItemsWithAction( baseDocument, ProjectMerger.HandledItems );

        AddItems( baseDocument, items );
        AddItems( localDocument, items );
        AddItems( theirDocument, items );

        if ( localDocument.ToString() == theirDocument.ToString() ) {
          // We handled all the differences
          using ( var textWriter = new StreamWriter( fullConflictPath ) ) {
            Package.WriteXml( textWriter, localDocument );
          }
          using ( var repository = new LibGit2Sharp.Repository( rootFolder ) ) {
            repository.Stage( conflict );
          }
        }
        else {
          using ( var repository = new LibGit2Sharp.Repository( rootFolder ) ) {
            GitHelper.ResolveWithStandardMergetool( repository, fullConflictPath, baseDocument, localDocument, theirDocument, logger, conflict );
          }
        }
      }
    }

    private static string FindRelativePathOfPackagesFolder( string folder = null ) {
      var current = new DirectoryInfo( folder ?? Directory.GetCurrentDirectory() );

      int depth = 0;

      while ( !new DirectoryInfo( Path.Combine( current.FullName, ".git" ) ).Exists ) {
        depth++;
        current = current.Parent;
        if ( current == null ) {
          throw new Exception( "Could not locate \".git\" folder" );
        }
      }

      return Enumerable.Repeat( "..", depth ).Aggregate( "packages", ( current1, e ) => Path.Combine( e, current1 ) );
    }

    private static string FindRepoRoot( string folder ) {
      var current = new DirectoryInfo( folder ?? Directory.GetCurrentDirectory() );
      while ( !new DirectoryInfo( Path.Combine( current.FullName, ".git" ) ).Exists ) {
        current = current.Parent;
        if ( current == null ) {
          throw new Exception( "Could not locate \".git\" folder" );
        }
      }
      return current.FullName;
    }

    private static void AddItems( XDocument doc, Item[] items ) {
      var root = doc.Root;
      var itemGroupName = root.Name.Namespace.GetName( "ItemGroup" );

      foreach ( var itemGroup in items.GroupBy( r => r.Action ).OrderBy( g => g.Key ) ) {

        var newGroup = new XElement( itemGroupName );
        
        foreach ( var item in itemGroup ) {
          newGroup.Add( item.ToElement( root.Name.Namespace ) );
        }
        root.Add( newGroup );
      }
    }

    private static void DeleteItemsWithAction( XDocument document, params string[] actions ) {
      // TODO: If we completely parse and re-write the project file we dont need this
      var root = document.Root;
      var xNamespace = root.Name.Namespace;

      Debug.Assert( root != null );

      var elementsToDelete = actions.SelectMany( a => root.Descendants( xNamespace.GetName( a ) ) ).ToArray();

      foreach ( XElement e in elementsToDelete ) {
        var parent = e.Parent;
        e.Remove();
        if ( parent != null && parent.IsEmpty ) {
          parent.Remove();
        }
      }
    }

    //public static IEnumerable<XElement> CombineElementChanges(
    //  IEnumerable<XElement> baseElements,
    //  IEnumerable<XElement> localElements,
    //  IEnumerable<XElement> theirElements ) {

    //  var logger = LogManager.GetCurrentClassLogger();

    //  var baseIndex = baseElements.ToDictionary( e => e.ToString(), e => e );

    //  var localIndex = localElements.ToDictionary( e => e.ToString(), e => e );

    //  var theirIndex = theirElements.ToDictionary( e => e.ToString(), e => e );

    //  foreach ( var elementKey in baseIndex.Keys.Union( localIndex.Keys ).Union( theirIndex.Keys ) ) {
    //    bool inBase = baseIndex.ContainsKey( elementKey );
    //    bool inLocal = localIndex.ContainsKey( elementKey );
    //    bool inTheirs = theirIndex.ContainsKey( elementKey );

    //    if ( !inLocal && !inTheirs ) {
    //      logger.Info( "Discarding " + baseIndex[elementKey] );
    //      continue; // agree on deleted
    //    }

    //    if ( inBase && !inTheirs ) {
    //      logger.Info( "Discarding " + baseIndex[elementKey] );
    //      continue; // was in base and theirs deleted
    //    }
    //    if ( inBase && !inLocal ) {
    //      logger.Info( "Discarding " + baseIndex[elementKey] );
    //      continue; // was in base and local deleted
    //    } 
    //    if ( inTheirs ) {
    //      yield return theirIndex[elementKey];
    //    }
    //    else {
    //      yield return localIndex[elementKey];
    //    }
    //  }
    //}
  }
}
