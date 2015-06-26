using System;
using System.IO;
using System.Xml.Linq;

using Cpc.CsMerge.Core;

using LibGit2Sharp;

using NLog;

using Reference = Cpc.CsMerge.Core.Reference;

namespace CsMerge {
  public class UserResolvers {

    public static void ResolveWithStandardMergetool(
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
      var theirsPath = fullConflictPath + "_theirs";

      Package.WriteXml( basePath, baseContent );
      Package.WriteXml( localPath, localContent );
      Package.WriteXml( theirsPath, theirContent );

      if ( GitHelper.RunStandardMergetool( repository, basePath, localPath, fullConflictPath, theirsPath, logger ) == 0 ) {
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
      File.Delete( theirsPath );
    }

    public static Package UserResolvePackage( IConflict<Package> conflict ) {
      Console.WriteLine( "(b)ase:\n" + PackageToString( conflict.Base ) );
      Console.WriteLine( "(l)ocal:\n" + PackageToString( conflict.Local ) );
      Console.WriteLine( "(p)atch:\n" + PackageToString( conflict.Patch ) );
      Console.WriteLine( "Choose resolution:" );
      while ( true ) {
        var key = Console.ReadKey();
        switch ( key.KeyChar ) {
          case 'b': return conflict.Base;
          case 'l': return conflict.Local;
          case 'p': return conflict.Patch;
        }
      }
    }

    public static T UserResolveReference<T>( IConflict<Item> conflict ) where T: Item {
      Console.WriteLine( "(b)ase:\n" + conflict.Base );
      Console.WriteLine( "(l)ocal:\n" + conflict.Local );
      Console.WriteLine( "(p)atch:\n" + conflict.Patch );
      while ( true ) {
        var key = Console.ReadKey();
        switch ( key.KeyChar ) {
          case 'b': return (T) conflict.Base;
          case 'l': return (T) conflict.Local;
          case 'p': return (T) conflict.Patch;
        }
      }
    }

    private static string PackageToString( Package p ) {
      return p == null ? "not installed" : p.ToString();
    }
  }
}