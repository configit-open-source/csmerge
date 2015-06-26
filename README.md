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

    Theirs deleted p1/10.0.0.0/net40-Client while Mine changed to add-p1/90.0.0.0/net45
    (b)ase: p1/10.0.0.0/net40-Client
    (m)ine: p1/90.0.0.0/net45
    (t)heirs: not installed
    Choose resolution:
    m
	Auto-merging packages.config

The merge tool uses Mine/Theirs correctly when rebasing and merging, so Mine is the changes in your branch, and not reversed as it is in non-rebase aware tools.

### Project files
After handling the packages.config files, the tool looks for conflicting project files (at some point it might look at all project files, to ensure correct references in new projects, but that is not implemented yet). Pending completion of a full project file merger, it will attempt to auto-resolve ItemGroup entries. 

All references to the nuget packages folder will be updated to match those listed in packages.config, deleting
references if the package has been removed from packages.config.

A non-NuGet reference will conflict if the same assembly name (ie. Configit.Core.Compile ) is has conflicting changes, such as differing versions, or other properties on the reference. In this case the the user is queried for a resolution.

The remaining items are resolved using their Include value (if present) as key, and if no Include is specified, the entire content of the item element is used to compare.

In the current implementation the project files ItemGroups are restructured to have one ItemGroup per item action (ie Compile, None etc.) and sorted on that action. The items are ordered according to their key (Package Id, Include path, Project Guid etc).
