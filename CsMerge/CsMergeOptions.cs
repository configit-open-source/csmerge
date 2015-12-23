using CommandLine;
using CommandLine.Text;

namespace CsMerge {
  public class CsMergeOptions {

    [Option( 'i', "input", Required = false, HelpText = "Optionally specifies the path to the root of the repository to process, "
                                                        + "defaults to the current working directory." )]
    public string InputFolder { get; set; }

    [HelpOption]
    public string GetUsage() {
      return HelpText.AutoBuild( this, current => HelpText.DefaultParsingErrorsHandler( this, current ) );
    }
  }
}