# Slightly less painful way to handle conflicts on nuget packages

## Disclaimer
Always backup your branch content before running this merge tool, its probably completely broken and will
eat your project files for lunch.

The code in this repository is WIP, and currently quite hacky in an attempt to get a minimal viable tool, so don´t complain about it :)

## Setup

### Step 1 : Create a merge driver that always fails the merge
Add the following to `C:\Users\<username>\.gitconfig` (or another git config file):

    [merge "failmerge"]
    	name = fail merge driver
    	driver = false
        recursive = binary

Any file types assigned to this merge driver will always fail to merge if they both have changes.
An alternative would be to mark those files as binary, but that will confuse diff viewers etc.

### Step 2 : Set attributes for packages.config and project files
Add the following in your repository to `.git\info\attributes`:

    **/packages.config merge=failmerge
    **/*.csproj merge=failmerge
    
By placing it under `.git\info` it will not be versioned and so will not affect other users of the repositories.

You can also set it up system wide, see [http://git-scm.com/docs/gitattributes](http://git-scm.com/docs/gitattributes "Git Attributes") for details.

## Usage

When packages.config files or csproj are modified in both branches, the configured failmerge merge driver
will make sure they are always considered conflicting, and not accidentally merged as text files.

You can now execute `CsMerge.exe`. Either provide the path to the git folder as an argument, or run it from the
folder you want to resolve conflicts for.

### packages.config
The tool resolves packages.config files first to make sure that we know what packages we need to reference 
in the project files.

If both branches modified the package entry, the highest version will be used. If only one branch has made changes to a package (including delete), then that change is simply applied. In case of conflicting changes (for example modify vs delete), the tool will ask the user to choose:

    Patch deleted p1/10.0.0.0/net40-Client while local changed to add-p1/90.0.0.0/net45
    (b)ase: p1/10.0.0.0/net40-Client
    (l)ocal: p1/90.0.0.0/net45
    (p)atch: not installed
    Choose resolution:
    pAuto-merging packages.config

Local is used to refer to the current working copy state, being "mine" if merging, and "theirs" if rebasing, while "patch" refers to the changes being applied (which is a patch with your changes if rebasing).

### Project files
After handling the packages.config files, the tool looks for conflicting project files (at some point it might look at all project files, to ensure correct references in new projects, but that is not implemented yet). Pending completion of a full project file merger, it will attempt to auto-resolve ItemGroup entries with action `Reference`, `None`, `Compile` and `ProjectReference`. Only non-NuGet `Reference`s can result in a conflict requiring user interaction.

All references to the nuget packages folder will be updated to match those listed in packages.config, deleting
references if the package has been removed from packages.config.

A non-NuGet reference will conflict if the same assembly name (ie. Configit.Core.Compile ) is has conflicting changes, such as differing versions, or other properties on the reference. In this case the the user is queried for a resolution.

In the current implementation the project files ItemGroups will be restructured.
