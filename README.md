# A tool to assist in handling packages.config and project files during conflicts and upgrades

The primary purpose of the CsMerge tool is to provide intelligent merging of project files and packages.config files while also offering help in dealing with project file vs packages.config file inconsistencies.

## Disclaimer
Always backup your branch content before running this merge tool, its probably completely broken and will
eat your project files for lunch.

The tool makes certain assumptions about your usage of NuGet packages and versioning. In particular:

- You use git for version control.
- You never want to use different versions of a package in the same repository.
- When resolving conflicts, you want the newest version among the two.
- You don´t use upper limits on allowed package versions
- Its okay to re-order project files (makes it hard to handle conflicts for people *not* using the tool)

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

### Step 3 : Install
Run the binaries\CsMerge.Setup.msi installer. The installer has no UI, but will install to programs files
and add CsMerge.exe to path. Alternatively you can build it from source.

## Usage in Merge mode
The default mode of the tool is 'merge' where it looks for conflicting project and packages.config files. Run the tool after starting a merge or rebase.

When packages.config files or csproj are modified in both branches, the configured failmerge merge driver
will make sure they are always considered conflicting, and not accidentally merged as text files.

Simply run CsMerge.exe in the git repository or with the --input <root folder path>.

### packages.config
The tool resolves packages.config files first to make sure that we know what packages we need to reference 
in the project files.

If both branches modified the package entry, the highest version will be used. If only one branch has made changes to a package (including delete), then that change is simply applied. In case of conflicting changes (for example modify vs delete), the tool will ask the user to choose.

The merge tool uses Mine/Theirs correctly when rebasing and merging, so Mine is the changes in your branch, and not reversed as it is in non-rebase aware tools.

### Project files
After handling the packages.config files, the tool looks for conflicting project files (at some point it might look at all project files, to ensure correct references in new projects, but that is not implemented yet). Pending completion of a full project file merger, it will attempt to auto-resolve ItemGroup entries. 

All references to the nuget packages folder will be updated to match those listed in packages.config, deleting
references if the package has been removed from packages.config.

A non-NuGet reference will conflict if the same assembly name (ie. Configit.Core.Compile ) has conflicting changes, such as differing versions, or other properties on the reference. In this case the the user is queried for a resolution.

The remaining items are resolved using their Include value (if present) as key, and if no Include is specified, the entire content of the item element is used to compare.

In the current implementation the project files ItemGroups are restructured to have one ItemGroup per item action (ie Compile, None etc.) and sorted on that action. The items are ordered according to their key (Package Id, Include path, Project Guid etc). While this will initially cause a major restructuring, but otherwise keep a consistent ordering.

## Usage in Align mode
The Align mode has two purposes:
- Ensure that the same (most recent) version of a package is used throughout the repository (even if there are multiple solution files in the current version).
- Allow batch upgrade of related packages

To simply align references to existing packages simply run:

```
CsMerge --mode align
```

This will cause CsMerge to restore package for all solution files in the repository folder, after which CsMerge will scan all packages.config files to find the most recent version of each package. It will then update packages.config files appropriately and finally fixup assembly references in the project files.

You can include batch upgrade of related packages by specifying for example:

```
CsMerge --mode align --upgrade-prefix A.B --version 1.5.0 -framework net45
```

This will cause CsMerge to upgrade any currently installed packages with an Id starting with A.B to version 1.5.0 and targeting net45.