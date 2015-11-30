using CsMerge.Core;
using CsMerge.Core.Resolvers;

using Integration;

using NUnit.Framework;

using PackagesMerge.Test.Resolvers;

using Project;

using static System.String;

namespace PackagesMerge.Test {
  [TestFixture]
  public class ReferenceConflictResolverTest {

    [Test]
    [TestCase( true )]
    [TestCase( false )]
    public void WhenOnlyVersionDiffers_ChooseNewest( bool mineHighest ) {
      ReferenceConflictResolver resolver = new ReferenceConflictResolver( new TestConflictResolver<Reference>( ConflictItemType.Local ) );

      var include = "NuGet.Frameworks, Version=$(version).0.0.0, Culture=neutral, PublicKeyToken=2e465378e3b1a8dd, processorArchitecture=MSIL";

      var newest = new Reference( include.Replace( "$(version)", "3" ), false, false, Empty );
      var older = new Reference( include.Replace( "$(version)", "2" ), false, false, Empty );

      var resolved = resolver.Resolve( new Conflict<Reference>(
        "myproject.csproj",
        "Configit.Dummy.dll",
        new Reference( include.Replace( "$(version)", "1" ), false, false, Empty ),
        mineHighest ? newest : older,
        mineHighest ? older : newest ) );

      Assert.That( resolved.IsResolved );
      Assert.That( resolved.MergeType, Is.EqualTo( MergeType.BothModified ) );
      Assert.That( resolved.ResolvedWith, Is.EqualTo( mineHighest ? ConflictItemType.Local : ConflictItemType.Incoming ) );
    }


    [Test]
    public void WhenIncludeCannotBeResolved() {
      var userConflictResolver = new TestConflictResolver<Reference>( ConflictItemType.Local );

      ReferenceConflictResolver resolver = new ReferenceConflictResolver( userConflictResolver );

      var baseReference = new Reference(
        "NuGet.Frameworks, Version=$(MyVersion), Culture=neutral, PublicKeyToken=2e465378e3b1a8dd, processorArchitecture=MSIL",
        false, false,
        @"\bin\$(Configuration)\NuGet.Frameworks.dll" );

      var mineReference = new Reference( "NuGet.Frameworks, Version=$(mine), Culture=neutral, PublicKeyToken=2e465378e3b1a8dd, processorArchitecture=MSIL",
                                        false, false, @"\bin\$(Configuration)\NuGet.Frameworks.dll" );

      var incomingReference = new Reference( "NuGet.Frameworks, Version=$(incoming), Culture=neutral, PublicKeyToken=2e465378e3b1a8dd, processorArchitecture=MSIL",
                                             false, false, @"\bin\$(Configuration)\NuGet.Frameworks.dll" );

      var resolved = resolver.Resolve( new Conflict<Reference>( "myproject.csproj", "Configit.Dummy.dll", baseReference, mineReference, incomingReference ) );

      // We expect fall back to user resolver
      Assert.That( userConflictResolver.Called );
      Assert.That( resolved.ResolvedWith, Is.EqualTo( ConflictItemType.Local ) );
    }
  }
}