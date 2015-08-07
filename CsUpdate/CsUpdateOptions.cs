using System.Collections.Generic;

using CommandLine;
using CommandLine.Text;

namespace CsUpdate {
  public class CsUpdateOptions {

    [Option( 'i', "input", Required = false, HelpText = "Optionally specifies the path to the root of the repository to process or the path to a .sln file, "
                                                        + "defaults to the current working directory. "
                                                        + "If a folder is provided packages will be upgraded across multiple .sln files in the folder." )]
    public string InputPath { get; set; }

    [OptionList( 'p', "packages", ',',
      HelpText = "Optionally specifies a list of package specifications limiting the packages that will be upgraded or aligned. "
                 + "Each item in the list can either be a package id, or a tuple of the form <id>:<version> or <id>:<version>:<framework>. "
                 + "For example, the input could be\nA,B.C:1.2.0:net40-client,D.E.*:1.3.0\n"
                 + "Wildcards ('*') are allowed at the end of the package id only. "
                 + "If version and framework is specified without --upgrade, "
                 + "they must match version/framework already installed in the solution(s), "
                 + "and all references to those packages will be aligned to that specification instead.")]
    public List<string> PackageIds { get; set; }

    [Option( 'u', "upgrade", Required = false,
      HelpText = "Specifies that the packages specified by --packages should be upgraded and not just aligned" )]
    public bool Upgrade { get; set; }

    [HelpOption]
    public string GetUsage() {
      return HelpText.AutoBuild( this, current => HelpText.DefaultParsingErrorsHandler( this, current ) );
    }
  }
}