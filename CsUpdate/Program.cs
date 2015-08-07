using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CsMerge;
using CsMerge.Core;

using NLog;

using NuGet.Configuration;

namespace CsUpdate {
  class Program {
    static void Main( string[] args ) {
      var options = new CsUpdateOptions();
      if ( !CommandLine.Parser.Default.ParseArguments( args, options ) ) {
        return;
      }

      // ProcessAlign(  );
    }

    private static void ProcessAlign( Logger logger, string rootFolder, CsUpdateOptions options, DirectoryInfo folder ) {
      logger.Info( "Updating/aligning references in " + rootFolder );

      //string pattern = options.Upgrade;
      //string patternVersion = options.UpgradeVersion;
      //string framework = options.UpgradeFramework;

      // TODO: Check specifically for known VS extensions only
      var projectFiles = folder.GetFiles( "*.*sproj", SearchOption.AllDirectories ).Select( f => f.FullName ).ToArray();

      // Restore packages now
      NuGetExtensions.RestorePackages( rootFolder );

      //TargetPackageIndex targetPackageIndex = new TargetPackageIndex( projectFiles, pattern, patternVersion, framework );

      //foreach ( var projectFile in projectFiles ) {
      //  new PackageReferenceAligner( projectFile, targetPackageIndex ).AlignReferences();
      //}
    }
  }
}
