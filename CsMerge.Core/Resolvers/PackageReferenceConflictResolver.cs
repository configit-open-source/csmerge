using System;
using System.Linq;

using CsMerge.Core.Exceptions;

using NLog;
using Project;

namespace CsMerge.Core.Resolvers {

  public class PackageReferenceConflictResolver: IConflictResolver<PackageReference> {

    private readonly IConflictResolver<PackageReference> _userConflictResolver;

    public PackageReferenceConflictResolver( IConflictResolver<PackageReference> userConflictResolver ) {
      _userConflictResolver = userConflictResolver;
    }

    public MergeResult<PackageReference> Resolve( Conflict<PackageReference> conflict ) {

      var logger = LogManager.GetCurrentClassLogger();

      // We only call this method if the reference was changed on on both sides to different state. See MergeHelper.Resolve

      if ( conflict.Local != null && conflict.Incoming != null ) {

        if ( conflict.Local.HasUnsupportedContent || conflict.Incoming.HasUnsupportedContent ) {
          return _userConflictResolver.Resolve( conflict );
        }

        // See if they are the same apart from the version, if so we can auto resolve to the highest version.
        if ( conflict.Local.EqualsIgnoringVersion( conflict.Incoming ) ) {
          var highestVersion = GetReferenceWithHighestVersion( conflict.Local, conflict.Incoming );

          if ( highestVersion.Reference == null ) {
            return _userConflictResolver.Resolve( conflict );
          }

          var changeDescription = conflict.Base == null ? "added" : "modified";
          var message = $"{LogHelper.Header}{Environment.NewLine}Both {changeDescription}: {conflict.Key}{Environment.NewLine}Picking highest version:{Environment.NewLine}{highestVersion.Reference}";
          logger.Info( message );

          return new MergeResult<PackageReference>( conflict.Key, highestVersion.Reference, conflict.GetMergeType(), highestVersion.ResolvedWith );
        }
      }

      if ( conflict.GetItems().All( i => !i.IsOptionValid() ) ) {
        throw new InvalidResolutonException( conflict.Key );
      }

      // We cannot auto resolve, let the user choose the resolution
      return _userConflictResolver.Resolve( conflict );
    }

    private static (PackageReference Reference, ConflictItemType ResolvedWith ) GetReferenceWithHighestVersion( PackageReference localReference, PackageReference incomingReference ) {

      var localResult = (localReference, ConflictItemType.Local);
      var incomingResult = (incomingReference, ConflictItemType.Incoming);
      var unresolvableResult = ((PackageReference) null, ConflictItemType.Incoming);

      if ( localReference.Version == incomingReference.Version ) {
        return localResult;
      }

      if ( localReference.ParsedVersion != null && incomingReference.ParsedVersion != null ) {
        return localReference.ParsedVersion >= incomingReference.ParsedVersion ? localResult : incomingResult;
      }

      const string WildCard = "*";

      var localParts = localReference.Version.Split( '.' );
      var incomingParts = incomingReference.Version.Split( '.' );

      var maxLength = Math.Max( localParts.Length, incomingParts.Length );

      for ( var i = 0; i < maxLength; i++ ) {
        var localPart = i < localParts.Length ? localParts[i] : "0";
        var incomingPart = i < incomingParts.Length ? incomingParts[i] : "0";

        // Wild cards are not supported unless they are the last part.
        // For example:
        //    1.*.2 makes no sense and cannot be auto resolved. 

        // Wild cards are only auto resolvable if compared to 0 or wildcard
        // For example:
        //    1.2.* vs 1.2.0 can be auto resolved to 1.2.*
        //    1.2.* vs 1.2.1 cannot be auto resolved because * would allow 1.2.0 which isn't allowed by 1.2.1
        
        if ( localPart == WildCard ) {
          return IsLastPart( localParts, i ) && IsZeroOrWildCard( incomingPart ) ? localResult : unresolvableResult;
        }

        if ( incomingPart == WildCard ) {
          return IsLastPart( incomingParts, i ) && IsZeroOrWildCard( localPart ) ? incomingResult : unresolvableResult;
        }

        var localPartInt = int.Parse( localPart );
        var incomingPartInt = int.Parse( incomingPart );

        if ( localPartInt == incomingPartInt ) {
          continue;
        }

        return localPartInt >= incomingPartInt ? localResult : incomingResult;
      }

      // The are the same, but maybe with implied parts. For example 1.0 vs 1.0.0
      return localResult;
    }

    private static bool IsZeroOrWildCard( string part ) {
      return part == "0" || part == "*";
    }

    private static bool IsLastPart( string[] parts, int index ) {
      return parts.Length - 1 == index;
    }
  }
}