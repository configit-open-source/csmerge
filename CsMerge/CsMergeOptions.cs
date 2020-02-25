using CommandLine;
using CommandLine.Text;

namespace CsMerge {

  public class CsMergeOptions {

    [Option( 'd', "debug", Required = false, HelpText = "Optionally specifies whether to launch the debugger", DefaultValue = false )]
    public bool Debug { get; set; }

    [Option( 'i', "input", Required = false, HelpText = "Optionally specifies the path to the root of the repository to process, "
                                                        + "defaults to the current working directory." )]
    public string InputFolder { get; set; }

    [Option( "configure-gitconfig", HelpText = "Can either be a path to a git config file or one of 'System', 'Global', 'Xdg', 'Local' to automatically locate the file. Using this flag will add a [merge] entry to the indicated git config file that can be used to always prevent automatic merge. " +
                                               "Its safe to install system-wide or globally for a user as it is only used when enabled through configure-gitattrib" )]
    public string ConfigureGitConfig { get; set; }
    [Option( "configure-gitattrib", HelpText = "Specifies a path to a gitattributes file (for example in git repo or in the user .gitattributes). Using this flag will map packages.config and project files to the merge entry installed by configure-gitconfig. " +
                                               "This prevents these files merging as text (which can corrupt the xml contents)." )]

    public string ConfigureGitAttributes { get; set; }

    [HelpOption]
    public string GetUsage() {
      return HelpText.AutoBuild( this, current => HelpText.DefaultParsingErrorsHandler( this, current ) );
    }
  }
}