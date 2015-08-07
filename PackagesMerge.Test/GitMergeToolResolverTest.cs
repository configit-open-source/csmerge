using System.Collections.Generic;
using CsMerge.Core;
using CsMerge.Resolvers;
using NUnit.Framework;

using Project;

namespace PackagesMerge.Test {

  [TestFixture]
  public class GitMergeToolResolverTest {

    [Test, Explicit]
    public void ResolveConflict() {

      const string Key = "Reference.To.Resolve.With.MergeTool";
      const string HintPath = @"Reference\To\Resolve\With\MergeTool.dll";

      var baseRerence = new Reference( Key, null, null, HintPath );
      var localRerence = new Reference( Key, false, null, HintPath );
      var incomingRerence = new Reference( Key, null, true, HintPath );

      var conflict = new Conflict<Reference>( "TestFilePath", Key, baseRerence, localRerence, incomingRerence );

      // As the respository will not be in a rebase / merge mode, you need to alter the currentOperation in GitHelper.RunStandardMergetool

      GitMergeToolResolver<Reference> resolver = new GitMergeToolResolver<Reference>( "../../../", conflict );

      var result = resolver.Resolve();

      Assert.That( result, Is.Not.Null );
    }

    [Test, Explicit]
    public void ResolveDuplicates() {

      const string Key = "Reference.To.Resolve.With.MergeTool";
      const string HintPath = @"Reference\To\Resolve\With\MergeTool.dll";

      var baseRerence = new Reference( Key, null, null, HintPath );
      var localRerence = new Reference( Key, false, null, HintPath );
      var incomingRerence1 = new Reference( Key, null, true, HintPath );
      var incomingRerence2 = new Reference( Key, true, null, HintPath );

      var conflict = new Conflict<IEnumerable<Reference>>(
        "TestFilePath",
        Key,
        new[] { baseRerence },
        new[] { localRerence },
        new[] { incomingRerence1, incomingRerence2 } );

      // As the respository will not be in a rebase / merge mode, you need to alter the currentOperation in GitHelper.RunStandardMergetool

      GitMergeToolResolver<Reference> resolver = new GitMergeToolResolver<Reference>( "../../../", conflict );

      var result = resolver.Resolve();

      Assert.That( result, Is.Not.Null );
    }
  }
}
