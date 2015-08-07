using CommandLine;
using CommandLine.Text;

namespace CsMerge {
  public class CsMergeOptions {

    [Option( 'm', "mode", DefaultValue = Mode.Merge, HelpText = "In the default 'merge' mode the tool will scan for conflicts to merge."
                                                                + " By specifying the mode as 'align' the tool will"
                                   + "instead ensure that project references matches the packages.config files and that only one version "
                                   + "of each package is used." )]
    public Mode Mode { get; set; }

    [Option( 'i', "input", Required = false, HelpText = "Optionally specifies the path to the root of the repository to process, "
                                                        + "defaults to the current working directory." )]
    public string InputFolder { get; set; }

    [Option( 'p', "upgrade-prefix", Required = false,
      HelpText = "Used with 'align' mode. Specifies a package id prefix of packages to upgrade as part of the align process "
                 + "(for example A.B will upgrade already installed packages A.B.C and A.B.D)."
                 + "Use with framework and version to specify the details of the package to target." )]
    public string UpgradePrefix { get; set; }

    [Option( 'f', "framework", Required = false, HelpText = "Framework to target for packages upgraded by the upgrade-prefix option" )]
    public string UpgradeFramework { get; set; }

    [Option( 'v', "version", Required = false, HelpText = "Package version to target for packages upgraded by the upgrade-prefix option." )]
    public string UpgradeVersion { get; set; }

    [HelpOption]
    public string GetUsage() {
      return HelpText.AutoBuild( this, current => HelpText.DefaultParsingErrorsHandler( this, current ) );
    }
  }
}