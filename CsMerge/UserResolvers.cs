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
        logger.Info( "Manually resolved " + fullConflictPath );
        File.Delete( fullConflictPath );
        File.Move( localPath, fullConflictPath );

        //RunGitCmd( "add", workingDir: Path.GetDirectoryName( fullConflictPath ), gitCmdArgs: conflict );
        repository.Stage( conflict );
      }
      else {
        logger.Info( "Did not resolve " + fullConflictPath );
        File.Delete( localPath );
      }

      File.Delete( basePath );
      File.Delete( theirsPath );
    }
    public static Package UserResolvePackage( Conflict<Package> conflict ) {
      Console.WriteLine( "(b)ase: " + PackageToString( conflict.Base ) );
      Console.WriteLine( "(l)ocal: " + PackageToString( conflict.Local ) );
      Console.WriteLine( "(p)atch: " + PackageToString( conflict.Patch ) );
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

    public static Reference UserResolveReference( Conflict<Reference> conflict ) {
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

    private static string PackageToString( Package p ) {
      return p == null ? "not installed" : p.ToString();
    }
  }
}