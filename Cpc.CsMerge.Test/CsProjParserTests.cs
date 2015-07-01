using System;
using System.Linq;
using System.Xml.Linq;

using CsMerge.Core;
using CsMerge.Core.Parsing;

using NUnit.Framework;

namespace Cpc.CsMerge.Test {
  [TestFixture]
  public class CsProjParserTests {
    private const string TestProjectName = "Test.csproj";

    private static ProjectFile Project( params object[] children ) {
      var root = new XElement( "Project", children );
      var parser = new CsProjParser();
      using ( var ms = root.ToStream() ) {
        return parser.Parse( TestProjectName, ms );
      }
    }

    private static XElement ItemGroup( params object[] children ) {
      return new XElement( "ItemGroup", children );
    }

    private static XElement Compile( string fileName ) {
      return new XElement( "Compile", new XAttribute( "Include", fileName ) );
    }

    private static XElement SystemReference( string assemblyName ) {
      return new XElement( "Reference", new XAttribute( "Include", assemblyName ) );
    }

    private static XElement NuGetReference( string assemblyName, bool specificVersion, string hintPath ) {
      return new XElement(
        "Reference"
        , new XAttribute( "Include", assemblyName )
        , new XElement( "SpecificVersion", specificVersion )
        , new XElement( "HintPath", hintPath )
      );
    }

    private static XElement ProjectReference( string csProjPath, Guid projectGuid, string name ) {
      return new XElement(
        "ProjectReference"
        , new XAttribute( "Include", csProjPath )
        , new XElement( "Project", projectGuid )
        , new XElement( "Name", name )
      );
    }

    [Test]
    public void Can_parse_empty_proj_file() {
      var proj = Project();

      Assert.That( proj.Name, Is.EqualTo( TestProjectName ) );
      Assert.That( proj.ItemGroups, Is.Empty );
    }

    [Test]
    public void Can_parse_empty_item_group() {
      var proj = Project( ItemGroup() );

      Assert.That( proj.ItemGroups, Has.Count.EqualTo( 1 ) );
    }

    //[Test]
    //public void Can_parse_compile_elements_in_root() {
    //  var proj = Project( ItemGroup( Compile( "testFile.cs" ) ) );

    //  var item = proj.ItemGroups.Single().Items.OfType<FileIncludeItem>().Single();

    //  Assert.That( item.FileName, Is.EqualTo( "testFile.cs" ) );
    //  Assert.That( item.Folder, Is.Empty );
    //}

    //[Test]
    //public void Can_parse_compile_elements_in_directory() {
    //  var proj = Project( ItemGroup( Compile( "test\\testFile.cs" ) ) );

    //  var item = proj.ItemGroups.Single().Items.OfType<RawItem>().Single();

    //  //Assert.That( item.FileName, Is.EqualTo( "testFile.cs" ) );
    //  //Assert.That( item.Folder, Is.EqualTo( "test" ) );
    //}

    [Test]
    public void Can_parse_compile_elements_in_nested_directories() {
      var proj = Project( ItemGroup( Compile( "test\\test\\testFile.cs" ) ) );

      var item = proj.ItemGroups.Single().Items.OfType<FileIncludeItem>().Single();

      Assert.That( item.FileName, Is.EqualTo( "testFile.cs" ) );
      Assert.That( item.Folder, Is.EqualTo( "test\\test" ) );
    }

    [Test]
    public void ReferenceToXelement() {
      var nuGetReference = NuGetReference( "blah", true, "mypath" );
      var reference = new Reference( nuGetReference );
      var element = reference.ToElement( XNamespace.None );

      Assert.That( element.SameNsElement( "HintPath" ).Value, Is.EqualTo( "mypath" ) );
      Assert.That( element.SameNsElement( "SpecificVersion" ).Value, Is.EqualTo( "true" ) );
      Assert.That( element.Attribute( "Include" ).Value, Is.EqualTo( "blah" ) );
    }

    //[Test]
    //public void Can_parse_multiple_compile_elements() {
    //  var proj = Project(
    //    ItemGroup(
    //      Compile( "test\\test\\testFile.cs" ),
    //      Compile( "test\\testFile.cs" )
    //    )
    //  );

    //  var items = proj.ItemGroups.Single().Items.OfType<FileIncludeItem>().ToList();

    //  Assert.That( items, Has.Count.EqualTo( 2 ) );

    //  Assert.That( items[0].FileName, Is.EqualTo( "testFile.cs" ) );
    //  Assert.That( items[0].Folder, Is.EqualTo( "test\\test" ) );

    //  Assert.That( items[1].FileName, Is.EqualTo( "testFile.cs" ) );
    //  Assert.That( items[1].Folder, Is.EqualTo( "test" ) );
    //}

    [Test]
    public void Can_parse_system_references() {
      const string assemblyName = "System.Linq";

      var proj = Project( ItemGroup( SystemReference( assemblyName ) ) );

      var item = proj.ItemGroups.Single().Items.OfType<Reference>().Single();

      Assert.That( item.Include, Is.EqualTo( assemblyName ) );
      Assert.That( item.SpecificVersion, Is.Null );
      Assert.That( item.HintPath, Is.Null );
    }

    [Test]
    public void Can_parse_nuget_references() {
      const string assemblyName = "nunit.framework, Version=2.6.3.13283, Culture=neutral, PublicKeyToken=96d09a1eb7f44a77, processorArchitecture=MSIL";
      const string hintPath = @"..\packages\NUnit.2.6.3\lib\nunit.framework.dll";

      var proj = Project( ItemGroup( NuGetReference(
        assemblyName,
        false,
        hintPath
      ) ) );

      var item = proj.ItemGroups.Single().Items.OfType<Reference>().Single();

      Assert.That( item.Include, Is.EqualTo( assemblyName ) );
      Assert.That( item.SpecificVersion, Is.EqualTo( false ) );
      Assert.That( item.HintPath, Is.EqualTo( hintPath ) );
    }

    [Test]
    public void Can_parse_project_references() {
      var proj = Project( ItemGroup( ProjectReference( "MyReference\\MyReference.csproj", new Guid( "AC585C31-D3A8-4D29-AC59-817D7ED7D403" ), "MyReference" ) ) );

      var item = proj.ItemGroups.Single().Items.OfType<ProjectReference>().Single();

      Assert.That( item.CsProjPath, Is.EqualTo( "MyReference\\MyReference.csproj" ) );
      Assert.That( item.ProjectId, Is.EqualTo( new Guid ("AC585C31-D3A8-4D29-AC59-817D7ED7D403" ) ) );
      Assert.That( item.Name, Is.EqualTo( "MyReference"  ) );
    }
  }
}
