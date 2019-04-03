using System;
using System.Xml.Linq;
using NUnit.Framework;
using Project;

namespace PackagesMerge.Test {

  [TestFixture]
  public class PackageReferenceTests {

    [Test]
    public void CanSerializeAndDeserializeElement() {

      var xml = @"<PackageReference Include=""Some.Nuget.Package"" Version=""1.2.3-beta1"" Condition=""'$(TargetFramework)' == 'net452'"">
      <IncludeAssets>runtime;build</IncludeAssets>
      <ExcludeAssets>native;contentfiles</ExcludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>";

      var element = XElement.Parse( xml );

      var packageReference = new PackageReference( element );

      Assert.That( packageReference.Key, Is.EqualTo( "Some.Nuget.Package" ) );
      Assert.That( packageReference.Include, Is.EqualTo( "Some.Nuget.Package" ) );
      Assert.That( packageReference.Version, Is.EqualTo( "1.2.3-beta1" ) );
      Assert.That( packageReference.ParsedVersion, Is.Null ); // Can't parse beta1
      Assert.That( packageReference.Condition, Is.EqualTo( "'$(TargetFramework)' == 'net452'" ) );
      Assert.That( packageReference.HasUnsupportedContent, Is.EqualTo( false ) );
      Assert.That( packageReference.IncludeAssets, Is.EquivalentTo( new[] { "runtime", "build" } ) );
      Assert.That( packageReference.ExcludeAssets, Is.EquivalentTo( new[] { "native", "contentfiles" } ) );
      Assert.That( packageReference.PrivateAssets, Is.EquivalentTo( new[] { "all" } ) );

      var serializedElement = packageReference.ToElement( element.GetDefaultNamespace() );

      Assert.That( XNode.DeepEquals( serializedElement, element ), Is.True );
    }

    [Test]
    public void HasUnsupportedContentDetectedForAttributes() {
      
      var xml = @"<PackageReference Include=""Some.Nuget.Package"" Version=""1.2.3"" UnsupportedAttribute=""True"" />";

      var element = XElement.Parse( xml );

      var packageReference = new PackageReference( element );

      Assert.That( packageReference.HasUnsupportedContent, Is.EqualTo( true ) );
    }
        
    [Test]
    public void HasUnsupportedContentDetectedForElements() {
      
      var xml = @"<PackageReference Include=""Some.Nuget.Package"" Version=""1.2.3""><UnsupportedAttribute>True</UnsupportedAttribute></PackageReference>";

      var element = XElement.Parse( xml );

      var packageReference = new PackageReference( element );

      Assert.That( packageReference.HasUnsupportedContent, Is.EqualTo( true ) );
    }
    
    [Test]
    public void ParsableVersionsAreParsed() {
      
      var xml = @"<PackageReference Include=""Some.Nuget.Package"" Version=""1.2.3"" />";

      var element = XElement.Parse( xml );

      var packageReference = new PackageReference( element );

      Assert.That( packageReference.ParsedVersion, Is.EqualTo( new Version( 1, 2, 3 ) ) );
    }

    [Test]
    public void AssetsAreStandardized() {
      
      var xml = @"<PackageReference Include=""Some.Nuget.Package"" Version=""1.2.3"">
      <IncludeAssets>runtime; build;; ;</IncludeAssets>
      <ExcludeAssets>runtime; build;; ;</ExcludeAssets>
      <PrivateAssets>runtime; build;; ;</PrivateAssets>
    </PackageReference>";

      var element = XElement.Parse( xml );

      var packageReference = new PackageReference( element );
      
      var assets = new[] { "runtime", "build" };

      Assert.That( packageReference.IncludeAssets, Is.EquivalentTo( assets ) );
      Assert.That( packageReference.ExcludeAssets, Is.EquivalentTo( assets ) );
      Assert.That( packageReference.PrivateAssets, Is.EquivalentTo( assets ) );

      var serializedElement = packageReference.ToElement( element.GetDefaultNamespace() );
      
      var expectedXml = @"<PackageReference Include=""Some.Nuget.Package"" Version=""1.2.3"">
      <IncludeAssets>runtime;build</IncludeAssets>
      <ExcludeAssets>runtime;build</ExcludeAssets>
      <PrivateAssets>runtime;build</PrivateAssets>
    </PackageReference>";
      
      var expectedElement = XElement.Parse( expectedXml );

      Assert.That( XNode.DeepEquals( serializedElement, expectedElement ), Is.True );
    }
  }
}
