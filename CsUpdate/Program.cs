using System;
using System.IO;
using System.Linq;

using CsUpdate.Core;

using Integration;

using NLog;

namespace CsUpdate {
  class Program {
    static void Main( string[] args ) {
      var options = new CsUpdateOptions();
      if ( !CommandLine.Parser.Default.ParseArguments( args, options ) ) {
        return;
      }

      ProcessAlign( options.InputPath ?? Directory.GetCurrentDirectory(), options  );
    }

    private static void ProcessAlign( string rootFolder, CsUpdateOptions options ) {
      LogManager.GetCurrentClassLogger().Info( "Updating/aligning references in " + rootFolder );

      // TODO: Check specifically for known VS extensions only
      var projectFiles = new DirectoryInfo( rootFolder ).GetFiles( "*.*sproj", SearchOption.AllDirectories ).Select( f => f.FullName ).ToArray();

      // Restore packages now
      NuGetExtensions.RestorePackages( rootFolder );

      TargetPackageIndex targetPackageIndex = new TargetPackageIndex( projectFiles, options.PackageIds.Select( p => new TargetPackage( p ) ) );

      foreach ( var projectFile in projectFiles ) {
        new PackageReferenceAligner( projectFile, targetPackageIndex ).AlignReferences();
      }
    }
  }
}
