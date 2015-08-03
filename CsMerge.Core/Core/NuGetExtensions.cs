using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using NLog;

using NuGet;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.PackagingCore;
using NuGet.Versioning;

namespace CsMerge.Core {
  public static class NuGetExtensions {

    public static IReadOnlyList<ConfigitPackageReference> ReadPackageReferences( string content ) {
      var result =
        new PackagesConfigReader( XDocument.Parse( content ) ).GetPackages().Select<PackageReference, ConfigitPackageReference>( p => (ConfigitPackageReference) p ).ToArray();
      return result;
    }

    public static bool Equals( PackageReference ref1, PackageReference ref2 ) {
      var frameworkComparer = new NuGetFrameworkFullComparer();
      var versionRangeComparer = new VersionRangeComparer();

      return ref1.IsDevelopmentDependency == ref2.IsDevelopmentDependency &&
             ref1.RequireReinstallation == ref2.RequireReinstallation &&
             ref1.IsUserInstalled == ref2.IsUserInstalled &&
             frameworkComparer.Equals( ref1.TargetFramework, ref2.TargetFramework ) &&
             ref1.PackageIdentity.Equals( ref2.PackageIdentity ) &&
             versionRangeComparer.Equals( ref1.AllowedVersions, ref2.AllowedVersions );
    }

    public static string ToFolderName( this PackageIdentity identity ) {
      return identity.Id + "." + identity.Version;
    }

    public static void Write( this IEnumerable<PackageReference> packages, string path ) {
      using ( var fs = new FileStream( path, FileMode.Create ) ) {
        packages.Write( fs );
      }
    }

    public static void Write( this IEnumerable<ConfigitPackageReference> packages, string path ) {
      using ( var fs = new FileStream( path, FileMode.Create ) ) {
        packages.Select( p => (PackageReference) p ).Write( fs );
      }
    }

    public static void Write( this IEnumerable<ConfigitPackageReference> packages, Stream stream ) {
      Write( packages.Select( p => (PackageReference) p ), stream );
    }

    public static void Write( this IEnumerable<PackageReference> packages, Stream stream ) {
      using ( PackagesConfigWriter writer = new PackagesConfigWriter( stream ) ) {
        foreach ( var package in packages ) {
          writer.WritePackageEntry( package );
        }
      }

      // XElement element = new XElement( "packages" );
      //element.Add( packages.Select( p => p.ToElement( element.Name.Namespace ) ) );
      //using ( var xmlWriter = XmlWriter.Create( writer, settings ) ) {
      //  element.WriteTo( xmlWriter );
      //}
    }

    public static string ToFolderName( this PackageReference reference ) {
      return reference.PackageIdentity.Id + "." + reference.PackageIdentity.Version;
    }

    public static PackageReference Clone( PackageReference source, NuGetVersion version = null ) {
      return new PackageReference( new PackageIdentity( source.PackageIdentity.Id, version ?? source.PackageIdentity.Version ),
                                   source.TargetFramework,
                                   source.IsUserInstalled,
                                   source.IsDevelopmentDependency,
                                   source.RequireReinstallation,
                                   source.AllowedVersions );
    }

    public static PackageIdentity PackageFromNuPkg( string packageFolder ) {
      var directoryInfo = new DirectoryInfo( packageFolder );
      if ( !directoryInfo.Exists ) {
        NLog.LogManager.GetCurrentClassLogger().Error(
          "Cannot resolve package identity of " + directoryInfo.FullName + " as the folder doesn´t exist" );
        return null;
      }

      var nupkg = directoryInfo.GetFiles( "*.nupkg" ).SingleOrDefault();
      if ( nupkg == null ) {
        NLog.LogManager.GetCurrentClassLogger().Error(
          "Cannot resolve package identity of " + directoryInfo.FullName + " as the folder doesn´t contain a nupkg file" );
        return null;
      }

      using ( var fs = new FileStream( nupkg.FullName, FileMode.Open ) ) {
        return new PackageReader( fs ).GetIdentity();
      }
    }

    /// <summary>
    /// Gets the relative path of the packages folder.
    /// </summary>
    /// <param name="folder">The folder that the returned path should be relative to. If null then the current directory is used.</param>
    public static string FindRelativePathOfPackagesFolder( string folder ) {
      var current = new DirectoryInfo( folder );

      while ( !new DirectoryInfo( Path.Combine( current.FullName, ".git" ) ).Exists ) {
        current = current.Parent;
        if ( current == null ) {
          throw new Exception( "Could not locate \".git\" folder" );
        }
      }

      var configFile = Path.Combine( current.FullName, ".nuget", "NuGet.config" );

      var packageFolder = "packages";

      if ( File.Exists( configFile ) ) {
        NuGet.Configuration.Settings settings = new NuGet.Configuration.Settings( Path.Combine( current.FullName, ".nuget" ) );
        packageFolder = settings.GetValue( "config", "repositoryPath" ) ?? packageFolder;
      }

      var fullPath = Path.GetFullPath( Path.Combine( Path.GetDirectoryName( configFile ), packageFolder ) );
      return MakeRelativePath( folder, fullPath );
    }


    public static string MakeRelativePath( string fromPath, string toPath ) {
      if ( string.IsNullOrEmpty( fromPath ) ) throw new ArgumentNullException( "fromPath" );
      if ( string.IsNullOrEmpty( toPath ) ) throw new ArgumentNullException( "toPath" );

      fromPath = PrepareForUri( fromPath );
      toPath = PrepareForUri( toPath );

      Uri fromUri = new Uri( fromPath );
      Uri toUri = new Uri( toPath );

      if ( fromUri.Scheme != toUri.Scheme ) { return toPath; } // path can't be made relative.

      Uri relativeUri = fromUri.MakeRelativeUri( toUri );
      string relativePath = Uri.UnescapeDataString( relativeUri.ToString() );

      if ( toUri.Scheme.ToUpperInvariant() == "FILE" ) {
        relativePath = relativePath.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
      }

      return relativePath;
    }

    public static void RestorePackages( string rootFolder ) {
      // TODO: Use NuGet API for this!

      var logger = LogManager.GetCurrentClassLogger();

      logger.Info( "Restoring nuget packages in " + rootFolder );

      var nugetExe = Path.Combine( rootFolder, @".nuget\nuget.exe" );
      if ( !File.Exists( nugetExe ) ) {
        throw new Exception( "cannot find " + nugetExe );
      }

      foreach ( var solutionFile in new DirectoryInfo( rootFolder ).GetFiles( "*.sln", SearchOption.AllDirectories ) ) {
        logger.Info( "Restoring packages for " + solutionFile.FullName );
        RunNugetRestore( rootFolder, logger, nugetExe, solutionFile.FullName );
      }
    }

    private static void RunNugetRestore(
      string rootFolder,
      Logger logger,
      string nugetExe,
      string solutionFilePath ) {

      var processStartInfo = new ProcessStartInfo( nugetExe, "restore " + solutionFilePath ) {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = rootFolder,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      var process = Process.Start( processStartInfo );

      logger.Info( process.StandardOutput.ReadToEnd() );
      logger.Info( process.StandardError.ReadToEnd() );

      if ( process.ExitCode != 0 ) {
        var exception = new Exception( "Failed to execute " + processStartInfo.FileName + " " + processStartInfo.Arguments );
        logger.Error( exception );
        throw exception;
      }
    }

    public static void InstallPackage(string subFolder, PackageReference package  ) {

      var logger = LogManager.GetCurrentClassLogger();

      string packagePath = Path.Combine( subFolder, FindRelativePathOfPackagesFolder( subFolder ) );

      // TODO: Use NuGet API for this!
      logger.Info( "Installing nuget package " + package.PackageIdentity + " to " + Path.GetFullPath( packagePath ) );
      var nugetExe = Path.Combine( GitHelper.FindRepoRoot( subFolder ), @".nuget\nuget.exe" );
      if ( !File.Exists( nugetExe ) ) {
        throw new Exception( "cannot find " + nugetExe );
      }

      var processStartInfo = new ProcessStartInfo( nugetExe, "install " + package.PackageIdentity.Id + " -Version " + package.PackageIdentity.Version + " -Pre" );
      processStartInfo.RedirectStandardOutput = true;
      processStartInfo.RedirectStandardError = true;

      processStartInfo.WorkingDirectory = packagePath;
      processStartInfo.UseShellExecute = false;
      processStartInfo.CreateNoWindow = true;

      var process = Process.Start( processStartInfo );
      logger.Info( process.StandardOutput.ReadToEnd() );
      logger.Info( process.StandardError.ReadToEnd() );

      if ( process.ExitCode != 0 ) {
        var exception = new Exception( "Failed to execute " + processStartInfo.FileName + " " + processStartInfo.Arguments );
        logger.Error( exception );
        throw exception;
      }
    }
    private static string PrepareForUri( string fromPath ) {
      if ( !fromPath.EndsWith( "/" ) && !fromPath.EndsWith( @"\" ) ) {
        fromPath = fromPath + @"\";
      }
      return fromPath;
    }
  }
}