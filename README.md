# A tool to assist in handling packages.config and project files during conflicts and upgrades

The primary purpose of the CsMerge tool is to provide intelligent merging of project files and packages.config files while also offering help in dealing with project file vs packages.config file inconsistencies.

## Disclaimer
Always backup your branch content before running this merge tool as there is always
the off chance that it will mess up you config files.

The tool makes certain assumptions about your usage of NuGet packages and versioning. In particular:

- You use git for version control.
- You never want to use different versions of a package in the same repository.
- When resolving conflicts, you want the newest version among the two.
- You don´t use upper limits on allowed package versions

## Setup

### Step 1 : Install using CsMerge.Setup.msi (or build from source)
You can download the latest installer under 'Releases'. You can also build directly from source and simply copy the files to a location that is on your path.

### Step 2 : Configure gitconfig
Once installed you can run
```
csmerge --configure-gitconfig <System|Global|Local|Xdg|(path to a config file)>
```

This will add the following to the indicated config file:

    [merge "failmerge"]
    	name = fail merge driver
    	driver = false
        recursive = binary

Any file types assigned to this merge driver will always fail to merge if they both have changes.
An alternative would be to mark those files as binary, but that will confuse diff viewers etc.

### Step 2 : Set attributes for packages.config and project files

```
csmerge --configure-gitattrib <path to a config file>
```

This will add the following to the indicated .gitattributes file:

    **/packages.config merge=failmerge
    **/*.*sproj merge=failmerge

These settings ensure that git will not attempt to automatically merge project and packages files as text (which could corrupt the XML content).

You can choose to add these settings to your global settings, or you can configure them on a per repository basis, either as part of the repository in `.git\info\attributes` to keep them out of version control.

You can read more about the various config locations here: [http://git-scm.com/docs/gitattributes](http://git-scm.com/docs/gitattributes "Git Attributes").

## Using CsMerge
The primary tool is 'CsMerge.exe' which looks for conflicting project and packages.config files. Run the tool after starting a merge or rebase.

When packages.config files or csproj are modified in both branches, the configured failmerge merge driver will make sure they are always considered conflicting, and not accidentally merged as text files.

Simply run CsMerge.exe in the git repository or with the --input <repo path>.

### packages.config
The tool resolves packages.config files first to make sure that we know what packages we need to reference in the project files.

If both branches modified the package entry, the highest version will be used. If only one branch has made changes to a package (including delete), then that change is simply applied. In case of conflicting changes (for example modify vs delete), the tool will ask the user to choose.

The merge tool uses Mine/Theirs correctly when rebasing and merging, so Mine is the changes in your branch, and not reversed as it is in non-rebase aware tools.

### Project files
After handling the packages.config files, the tool looks for conflicting project files. Pending completion of a full project file merger, it will attempt to auto-resolve ItemGroup entries.

All references to the nuget packages folder will be updated to match those listed in packages.config, deleting
references if the package has been removed from packages.config.

A non-NuGet reference will conflict if the same assembly name (ie. Configit.Core.Compile ) has conflicting changes, such as differing versions, or other properties on the reference. In this case the the user is queried for a resolution.

The remaining items are resolved using their Include value (if present) as key, and if no Include is specified, the entire content of the item element is used to compare.

## Using CsUpdate
An additional tool called `CsUpdate` is installed alongside CsMerge. It is intended to help performing cleanup in nuget package references from projects and packages.config files as well as fix various project file issues (duplicate items for example).

To simply consolidate references to existing packages to the highest version (batch version of the consolidate option in VS2015) simply run:

```
CsUpdate -i <folder>
```

This will cause CsUpdate to restore packages for all solution files in the repository folder, after which CsUpdate will scan all packages.config files to find the most recent version of each package. It will then update packages.config files appropriately and finally fixup assembly references in the project files.

Please be aware that CsUpdate does not currently run any install scripts, so if the new package version is significantly different from the old one, it is highly recommended to use the package manager in VS2015.

If you wish to retarget specific packages this can be achieved using the --packages option. The --packages option specifies a list of package specifications limiting the packages that will be upgraded or aligned. Each item in the list can either be a package id, or a tuple of the form <id>:<version> or <id>:<version>:<framework>. For example, the input could be

```
--packages A,B.C:1.2.0:net40-client,D.E.*:1.3.0
```

Wildcards ('*') are allowed at the end of the package id only. The versions must match already installed packages.
To upgrade to packages that are not already installed, use the --upgrade option (see --help for more information).
