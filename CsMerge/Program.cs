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
using GitSharp;
using NLog;

using PackagesMerge;

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
          GitHelper.RunGitCmd( "add", workingDir : folder.FullName, gitCmdArgs : conflict );
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
          GitHelper.RunGitCmd( "add", workingDir : folder.FullName, gitCmdArgs : conflict );
        }
        else {
          GitHelper.ResolveWithStandardMergetool( fullConflictPath, baseContent, localContent, theirContent, logger, conflict );
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

    private static void AddItems( XDocument localDocument, Item[] items ) {
      var itemGroupName = localDocument.Root.Name.Namespace.GetName( "ItemGroup" );

      var emptyGroups = new Stack<XElement>( localDocument.Descendants( itemGroupName ).Where( ig => ig.IsEmpty ) );

      foreach ( var itemGroup in items.GroupBy( r => r.Action ) ) {
        if ( emptyGroups.Count == 0 ) {
          emptyGroups.Push( new XElement( itemGroupName ) );
        }
        var group = emptyGroups.Pop();
        foreach ( var item in itemGroup ) {
          @group.Add( item.ToElement( localDocument.Root.Name.Namespace ) );
        }
      }
    }

    private static void DeleteItemsWithAction( XDocument document, params string[] actions ) {
      // TODO: If we completely parse the project file we dont need this
      var root = document.Root;
      Debug.Assert( root != null );
      actions.Select( a => root.Descendants( root.Name.Namespace.GetName( a ) ) ).ToList().ForEach( e => e.Remove() );
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
