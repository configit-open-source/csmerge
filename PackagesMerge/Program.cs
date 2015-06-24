using System;
using System.IO;
using System.Linq;

using Cpc.CsMerge.Core;

namespace PackagesMerge {

  public class Program {
    public static int Main( string[] args ) {
      if ( args.Length != 4 ) {
        Console.WriteLine( "Usage: <base file> <local file> <remote file> <output file>" );
        return -2;
      }
      string @base = args[0];
      string local = args[1];
      string theirs = args[2];
      string resolved = args[3];

      var logger = NLog.LogManager.GetCurrentClassLogger();
      var folder = Path.GetDirectoryName( Path.GetFullPath( @base ) );
      logger.Info( "Merging changes to " + Path.Combine( folder ?? string.Empty, "packages.config" ) );

      var result = PackagesConfigMerger.Merge( Package.Read( @base ), Package.Read( local ), Package.Read( theirs ), UserResolution ).ToArray();

      Package.Write( result, resolved );
      return 0;
    }

    private static string PackageToString( Package p ) {
      return p == null ? "not installed" : p.ToString();
    }

    private static Package UserResolution( Conflict<Package> conflict ) {
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
  }
}
