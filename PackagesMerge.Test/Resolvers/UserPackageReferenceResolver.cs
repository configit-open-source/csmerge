using System.Xml.Linq;
using CsMerge.Core;
using CsMerge.Core.Resolvers;
using Project;

namespace PackagesMerge.Test.Resolvers {
  
  public class UserPackageReferenceResolver: IConflictResolver<PackageReference> {
    
    public const string DummyVersion = "9.9.9-user99";

    public MergeResult<PackageReference> Resolve( Conflict<PackageReference> conflict ) {
      var packageReference = conflict.Local ?? conflict.Incoming ?? conflict.Base;
      var xElement = packageReference.ToElement( "" );
      var attribute = xElement.Attribute( XName.Get( "Version" ) );
      attribute.Value = DummyVersion;
      var resolvedItem = new PackageReference( xElement );
      return new MergeResult<PackageReference>( conflict.Key, resolvedItem, conflict.GetMergeType(), ConflictItemType.Unknown, true );
    }
  }
}
