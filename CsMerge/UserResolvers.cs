using System;

using Cpc.CsMerge.Core;

namespace CsMerge {
  public class UserResolvers {

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