using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Cpc.CsMerge.Core;
using Cpc.CsMerge.Core.Parsing;

using GitSharp;

using NLog;

using SharpGit;
using SharpGit.Plumbing;

namespace CsMerge {
  public class Program {
   
    private static string FindRelativePathOfPackagesFolder( string folder = null ) {
      folder = folder ?? Directory.GetCurrentDirectory();
      DirectoryInfo current = new DirectoryInfo( Directory.GetCurrentDirectory() );

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

    static void Main( string[] args ) {
      if ( args.Length != 1 ) {
        args = new[] { Directory.GetCurrentDirectory() };
      }

      DirectoryInfo folder = new DirectoryInfo( args[0] );
      var logger = LogManager.GetCurrentClassLogger();
      logger.Debug( "Scanning " + folder );

      Repository gitRepo = new Repository( folder.FullName );

      foreach ( var conflict in gitRepo.Status.MergeConflict.Where( p => p.EndsWith( ".csproj" ) ) ) {
        var fullConflictPath = Path.Combine( folder.FullName, conflict );
        logger.Info( "Examining conflict for " + fullConflictPath );

        var baseContent = GetContent( 1, conflict, folder.FullName );
        var localContent = GetContent( 2, conflict, folder.FullName );
        var theirContent = GetContent( 3, conflict, folder.FullName );

        var conflictFolder = Path.GetDirectoryName( conflict );

        XDocument localDocument = XDocument.Parse( localContent );
        XDocument theirDocument = XDocument.Parse( theirContent );
        XDocument baseDocument = XDocument.Parse( baseContent );

        var projFileName = Path.GetFileName( conflict );

        var projectFolder = Path.Combine( folder.FullName, conflictFolder );

        var packageIndex = new PackagesInfo( projectFolder, FindRelativePathOfPackagesFolder( projectFolder ) );

        Item[] items = ProjectMerger.Merge( projFileName, packageIndex, baseDocument, localDocument, theirDocument, UserResolve ).ToArray();

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
          new GitClient().Stage( conflict );
        }
        else {
          ResolveWithStandardMergetool( fullConflictPath, baseContent, localContent, theirContent, logger, gitRepo, conflict );
        }
      }
    }

    private static void ResolveWithStandardMergetool(
      string fullConflictPath,
      string baseContent,
      string localContent,
      string theirContent,
      Logger logger,
      Repository gitRepo,
      string conflict ) {
      // Run the standard mergetool to deal with any remaining issues.
      var basePath = fullConflictPath + "_base";
      var localPath = fullConflictPath + "_local";
      var theirsPath = fullConflictPath + "_theirs";

      File.WriteAllText( basePath, baseContent );

      File.WriteAllText( localPath, localContent );

      File.WriteAllText( theirsPath, theirContent );
      if ( RunStandardMergetool( basePath, localPath, fullConflictPath, theirsPath, logger ) == 0 ) {
        // The merge tool reports that the conflict was resolved
        new GitClient().Stage( conflict );
        logger.Info( "Manually resolved " + fullConflictPath );
      }
      else {
        logger.Info( "Did not resolve " + fullConflictPath );
      }
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
          @group.Add( item.ToElement( @group.Name.Namespace ) );
        }
      }
    }

    private static void DeleteItemsWithAction( XDocument document, params string[] actions ) {
      // TODO: If we completely parse the project file we dont need this
      var root = document.Root;
      Debug.Assert( root != null );
      actions.Select( a => root.Descendants( root.Name.Namespace.GetName( a ) ) ).ToList().ForEach( e => e.Remove() );
    }

    // TODO: This is re-usable
    private static Reference UserResolve( Conflict<Reference> conflict ) {
      Console.WriteLine( "(b)ase: " + conflict.Base );
      Console.WriteLine( "(l)ocal: " + conflict.Local );
      Console.WriteLine( "(p)atch: " + conflict.Patch );
      while ( true ) {
        var key = Console.ReadKey();
        switch ( key.KeyChar ) {
          case 'b': return conflict.Base;
          case 'l': return conflict.Local;
          case 'p': return conflict.Patch;
        }
      }
    }

    private static string GetContent( int stage, string path, string folder ) {
      // :<n>:<path>, e.g. :0:README, :README
      //A colon, optionally followed by a stage number (0 to 3) and a colon, 
      // followed by a path, names a blob object in the index at the given path.
      // A missing stage number (and the colon that follows it) names a stage 0 entry. 
      // During a merge, stage 1 is the common ancestor, stage 2 is the target branch’s version 
      // (typically the current branch), and stage 3 is the version from the branch which is being merged.
      return GitHelper.GetGitValue( cmd : "show", gitCmdArgs : ":" + stage + ":" + path, workingDir: folder );
    }

    private static int RunStandardMergetool( string @base, string local, string resolved, string theirs, Logger logger ) {
      string cmdLine =
        GitHelper.GetMergeCmdLine()
          .Replace( "$BASE", @base )
          .Replace( "$LOCAL", local )
          .Replace( "$MERGED", resolved )
          .Replace( "$REMOTE", theirs );

      logger.Debug( "Invoking:\n" + cmdLine );

      var processStartInfo = new ProcessStartInfo( "cmd.exe", "/C \"" + cmdLine + "\"" ) {
        CreateNoWindow = true,
        UseShellExecute = true,
      };

      var process = Process.Start( processStartInfo );

      if ( process == null ) {
        throw new Exception( "Could not execute " + cmdLine );
      }

      process.WaitForExit();
      return process.ExitCode;
    }

    private static XmlWriter CreateWriter( string path ) {
      return XmlWriter.Create( path, new XmlWriterSettings {
        Indent = true,
        IndentChars = "  ",
        CloseOutput = true,
        Encoding = Encoding.UTF8,
        NewLineChars = "\n",
        NewLineHandling = NewLineHandling.None
      } );
    }

    public static XDocument Merge( XDocument baseDoc, XDocument localDoc, XDocument theirDoc ) {
      if ( localDoc.ToString().Equals( theirDoc.ToString() ) ) {
        return localDoc;
      }

      var baseRefs = baseDoc.Descendants( baseDoc.Root.Name.Namespace.GetName( "Reference" ) ).ToArray();
      var localRefs = localDoc.Descendants( localDoc.Root.Name.Namespace.GetName( "Reference" ) ).ToArray();
      var theirRefs = theirDoc.Descendants( theirDoc.Root.Name.Namespace.GetName( "Reference" ) ).ToArray();

      IEnumerable<XElement> combinedRefs = CombineElementChanges( baseRefs, localRefs, theirRefs ).ToArray();

      foreach ( var oldRef in baseRefs.Concat( localRefs ).Concat( theirRefs ) ) {
        oldRef.Remove();
      }
      XDocument merged = new XDocument( localDoc );

      var root = merged.Root;
      if ( root == null ) {
        throw new Exception( "null root!" );
      }

      var someReference = root.Descendants( root.Name.Namespace.GetName( "Reference" ) ).FirstOrDefault();

      XElement parentElement;
      if ( someReference != null ) {
        parentElement = someReference.Parent;
        Debug.Assert( parentElement != null );
      }
      else {
        // Create an item group to store the reference
        parentElement = new XElement( root.Name.Namespace.GetName( "ItemGroup" ) );
        root.Add( parentElement );
      }

      foreach ( var cRef in combinedRefs ) {
        parentElement.Add( cRef );
      }
      return merged;
    }

    public static IEnumerable<XElement> CombineElementChanges(
      IEnumerable<XElement> baseElements,
      IEnumerable<XElement> localElements,
      IEnumerable<XElement> theirElements ) {

      var logger = LogManager.GetCurrentClassLogger();

      var baseIndex = baseElements.ToDictionary( e => e.ToString(), e => e );

      var localIndex = localElements.ToDictionary( e => e.ToString(), e => e );

      var theirIndex = theirElements.ToDictionary( e => e.ToString(), e => e );

      foreach ( var elementKey in baseIndex.Keys.Union( localIndex.Keys ).Union( theirIndex.Keys ) ) {
        bool inBase = baseIndex.ContainsKey( elementKey );
        bool inLocal = localIndex.ContainsKey( elementKey );
        bool inTheirs = theirIndex.ContainsKey( elementKey );

        if ( !inLocal && !inTheirs ) {
          logger.Info( "Discarding " + baseIndex[elementKey] );
          continue; // agree on deleted
        }

        if ( inBase && !inTheirs ) {
          logger.Info( "Discarding " + baseIndex[elementKey] );
          continue; // was in base and theirs deleted
        }
        if ( inBase && !inLocal ) {
          logger.Info( "Discarding " + baseIndex[elementKey] );
          continue; // was in base and local deleted
        } 
        if ( inTheirs ) {
          yield return theirIndex[elementKey];
        }
        else {
          yield return localIndex[elementKey];
        }
      }
    }

    private static void RemovePackageReferences( string packagesPrefix, XDocument document ) {
      var logger = LogManager.GetCurrentClassLogger();
      //logger.Debug( "Removing nuget references from " + document.ToString() );

      var root = document.Root;
      if( root == null) {
        throw new ArgumentException( "Invalid csproj file" );
      }
      var references = root.Descendants( root.Name.Namespace.GetName("Reference" ) ).ToArray();

      foreach ( var reference in references ) {
        var hintPath = reference.Elements( reference.Name.Namespace.GetName( "HintPath" ) ).FirstOrDefault();
        if( hintPath == null ) {
          continue;
        }
        if ( hintPath.Value.StartsWith( packagesPrefix ) ) {
          logger.Debug( "Removing reference with hintpath " + hintPath.Value );
          reference.Remove();
        }
      }
    }
  }
}
