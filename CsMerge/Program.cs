using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using NLog;

namespace CsMerge {
  public class Program {

    private static string GetConfigValue( string key ) {
      var processStartInfo = new ProcessStartInfo( "git", "config " + key );
      processStartInfo.RedirectStandardOutput = true;
      processStartInfo.UseShellExecute = false;

      var process = Process.Start( processStartInfo );

      if ( process == null ) {
        throw new Exception( "Could not execute " + processStartInfo.FileName + " " + processStartInfo.Arguments );
      }

      string result = process.StandardOutput.ReadLine();
      process.WaitForExit();
      Console.WriteLine(key + " =  " + result);
      return result;
    }

    private static string GetMergeCmdLine() {
      string mergetool = GetConfigValue( "merge.tool" );
      return GetConfigValue( "mergetool." + mergetool + ".cmd" );
    }

    private static string FindRelativePathOfPackagesFolder() {
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

    static int Main( string[] args ) {
      if ( args.Length != 4 ) {
        Console.WriteLine( "Usage: <base file> <local file> <remote file> <output file>" );
        return -2;
      }
      var logger = LogManager.GetCurrentClassLogger();
      
      string @base = Path.GetFullPath(args[0]);
      string local = Path.GetFullPath( args[1] );
      string theirs = Path.GetFullPath( args[2] );
      string resolved = Path.GetFullPath( args[3] );

      
      logger.Info( "Removing package references from conflicting .csproj file to enable better merge" );
      logger.Info( "Please remember to re-install nuget packages after the merge is complete!" );

      XDocument localDocument = XDocument.Load( @local );
      XDocument theirDocument = XDocument.Load( @theirs );
      XDocument baseDocument = XDocument.Load( @base );

      string relativePackagePath = FindRelativePathOfPackagesFolder();

      RemovePackageReferences( relativePackagePath, localDocument );
      RemovePackageReferences( relativePackagePath, theirDocument );
      RemovePackageReferences( relativePackagePath, baseDocument );
      Merge( baseDocument, localDocument, theirDocument );

      logger.Debug( "Found packages folder at " + relativePackagePath );

      if ( localDocument.ToString().Equals( theirDocument.ToString() ) ) {
        logger.Info( "Match after removing references" );
        using ( var writer = CreateWriter( resolved ) ) {
          localDocument.WriteTo( writer );
        }
        return 0;
      }

      // Setup for calling the normal git merge tool
      using ( var writer = CreateWriter( @local ) ) {
        localDocument.WriteTo( writer );
      }

      using ( var writer = CreateWriter( theirs ) ) {
        theirDocument.WriteTo( writer );
      }

      using ( var writer = CreateWriter( @base ) ) {
        baseDocument.WriteTo( writer );
      }

      string cmdLine = GetMergeCmdLine()
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

    public static void Merge( XDocument baseDoc, XDocument localDoc, XDocument theirDoc ) {
      if ( localDoc.ToString().Equals( theirDoc.ToString() ) ) {
        return;
      }
      var baseRefs = baseDoc.Descendants( baseDoc.Root.Name.Namespace.GetName("Reference" )).ToArray();
      var localRefs = localDoc.Descendants( localDoc.Root.Name.Namespace.GetName("Reference" )).ToArray();
      var theirRefs = theirDoc.Descendants( theirDoc.Root.Name.Namespace.GetName("Reference" ) ).ToArray();

      IEnumerable<XElement> combinedRefs = MergeElements( baseRefs, localRefs, theirRefs ).ToArray();

      foreach ( var oldRef in baseRefs.Concat( localRefs ).Concat( theirRefs )) {
        oldRef.Remove();
      }

      foreach ( var document in new[] { baseDoc, localDoc, theirDoc } ) {
        var root = document.Root;
        if ( root == null ) {
          throw new Exception( "null root!" );
        }

        var someReference = root.Descendants( root.Name.Namespace.GetName( "Reference" ) ).FirstOrDefault();

        XElement parentElement;
        if( someReference != null) {
          parentElement = someReference.Parent;
          Debug.Assert( parentElement != null );
        }
        else{
          // Create an item group to store the reference
          parentElement = new XElement( root.Name.Namespace.GetName( "ItemGroup" ) );
          root.Add( parentElement );
        }

        foreach ( var cRef in combinedRefs ) {
          parentElement.Add( cRef );
        }
      }
    }

    public static IEnumerable<XElement> MergeElements(
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
        logger.Info( "Keeping " + elementKey );  // added by one of the branches  
        if ( inTheirs ) {

          yield return theirIndex[elementKey];
        }
        else {
          yield return localIndex[elementKey];
        }
      }
    }

    public static void RemovePackageReferences( string packagesPrefix, XDocument document ) {
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
