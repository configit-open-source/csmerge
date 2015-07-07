using System;
using System.IO;
using System.Xml.Linq;

using CsMerge.Core;

using LibGit2Sharp;

using NLog;

namespace CsMerge {
  public class UserResolvers {
    public CurrentOperation Operation { get; set; }

    public UserResolvers( CurrentOperation operation ) {
      Operation = operation;
      Local = MergeTypeExtensions.Local( Operation );
      Incoming = MergeTypeExtensions.Incoming( Operation );
    }

    public string Local { get; private set; }

    public string Incoming { get; private set; }

    public void ResolveWithStandardMergetool(
     Repository repository,
     string fullConflictPath,
     XDocument baseContent,
     XDocument localContent,
     XDocument theirContent,
     Logger logger,
     string conflict ) {
      // Run the standard mergetool to deal with any remaining issues.
      var basePath = fullConflictPath + "_base";
      var localPath = fullConflictPath + "_local";
      var incomingPath = fullConflictPath + "_theirs";

      PackageReference.WriteXml( basePath, baseContent );
      PackageReference.WriteXml( localPath, localContent );
      PackageReference.WriteXml( incomingPath, theirContent );

      if ( GitHelper.RunStandardMergetool( repository, basePath, localPath, incomingPath, fullConflictPath ) == 0 ) {
        // The merge tool reports that the conflict was resolved
        logger.Info( "Resolved " + fullConflictPath + " using standad merge tool" );
        File.Delete( fullConflictPath );
        File.Move( localPath, fullConflictPath );

        repository.Stage( conflict );
      }
      else {
        logger.Info( "Did not resolve " + fullConflictPath );
        throw new OperationCanceledException();
      }

      File.Delete( basePath );
      File.Delete( incomingPath );
    }

    public PackageReference UserResolvePackage( IConflict<PackageReference> conflict ) {
      Console.WriteLine( "(b)ase :\n" + PackageToString( conflict.Base ) );
      Console.WriteLine( "(" + Local[0] + ")" + Local.Substring( 1 ) + ":\n" + PackageToString( conflict.Local ) );
      Console.WriteLine( "(" + Incoming[0] + ")" + Incoming.Substring( 1 ) + ":\n" + PackageToString( conflict.Incoming ) );
      return ChooseResolution( conflict );
    }

    private T ChooseResolution<T>( IConflict<T> conflict ) {
      Console.WriteLine( "Choose resolution:" );
      try {
        while ( true ) {
          string key = Console.ReadKey().KeyChar.ToString().ToUpperInvariant();

          if ( key == "B" ) {
            return conflict.Base;
          }
          if ( key == Local[0].ToString().ToUpperInvariant() ) {
            return conflict.Local;
          }
          if ( key == Incoming[0].ToString().ToUpperInvariant() ) {
            return conflict.Incoming;
          }
        }
      }
      finally {
        Console.WriteLine();
      }
    }

    public T UserResolveReference<T>( IConflict<Item> conflict ) where T: Item {
      Console.WriteLine( "(b)ase :\n" + conflict.Base );
      Console.WriteLine( "(" + Local[0] + ")" + Local.Substring( 1 ) + ":\n" + conflict.Local );
      Console.WriteLine( "(" + Incoming[0] + ")" + Incoming.Substring( 1 ) + ":\n" + conflict.Incoming );
      return (T) ChooseResolution( conflict );
    }

    private static string PackageToString( PackageReference p ) {
      return p == null ? "not installed" : p.ToString();
    }
  }
}