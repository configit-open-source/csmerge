# Slightly less painful way to handle conflicts on nuget packages

## Disclaimer
The code in this repository is WIP, and currently quite hacky in an attempt to get a minimal viable tool, so don´t complain about it :)

Some things are not currently supported, including nuget packages installed with allowedVersion specified using [x.y,z,w] etc. 

## Setup

### Step 1 : Install merge driver for packages.config files
Add the following to `C:\Users\<username>\.gitconfig` (or another git config file):

    [merge "packagesconfig"]
    	name = packages.config merge driver
    	driver = "<path to PackagesMerge.exe"> %O %A %B %A
        recursive = binary

### Step 2 : Set attributes for packages.config
Add the following in your repository to `.git\info\attributes`:

    **/packages.config merge=packagesconfig

By placing it under `.git\info` it will not be versioned and so will not affect other users of the repositories.

You can also set it up system wide, see [http://git-scm.com/docs/gitattributes](http://git-scm.com/docs/gitattributes "Git Attributes") for details.

### Step 3 : Install merge driver for *.csproj files
Add the following to `C:\Users\<username>\.gitconfig` (or another git config file):

    [merge "csproj"]
    	name = csproj merge driver
    	driver = "<path to CsMerge.exe"> %O %A %B %A
        recursive = binary

### Step 4 : Set attributes for *.csproj
Add the following in your repository to `.git\info\attributes`:

    **/*.csproj merge=csproj

The merge driver should be able to support other project files types as well, so it can be set
up for other project types if necessary.

## Usage

### packages.config
As part of a rebase/merge the tool will attempt to automatically merge packages.config files. If both
branches modified the package, the highest version will be used. If only one branch has made changes to a package (including delete), then that change is simply applied. In case of conflicting changes (for example modify vs delete), the tool will ask the user to choose:

    Patch deleted p1/10.0.0.0/net40-Client while local changed to add-p1/90.0.0.0/net45
    (b)ase: p1/10.0.0.0/net40-Client
    (l)ocal: p1/90.0.0.0/net45
    (p)atch: not installed
    Choose resolution:
    pAuto-merging packages.config

Local is used to refer to the current working copy state, being "mine" if merging, and "theirs" if rebasing, while "patch" refers to the changes being applied (which is a patch with your changes if rebasing). 

### Project files
Pending completion of a full csproj merge tool, the custom merge driver for csproj files strips the base and two branch project files of all references pointing to the folder where nuget packages are stored, and then passes those files to the configured mergetool (ie, same as running git mergetool, but not showing nuget related conflicts). It will also resolve conflicts in other references by combining the reference changes.

After the two merge drivers have run any project files affected will be missing their references to nuget package assemblies. The easiest way to fix this is to make sure that all packages are restored, and then re-install all packages in the solution using the following command on the NuGet console:

    Update-Package -reinstall
