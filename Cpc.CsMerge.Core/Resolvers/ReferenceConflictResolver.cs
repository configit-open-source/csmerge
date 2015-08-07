using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace CsMerge.Core.Resolvers {

  public class ReferenceConflictResolver: IConflictResolver<Reference> {

    private readonly IConflictResolver<Reference> _userConflictResolver;

    public ReferenceConflictResolver( IConflictResolver<Reference> userConflictResolver ) {
      _userConflictResolver = userConflictResolver;
    }

    public MergeResult<Reference> Resolve( Conflict<Reference> conflict ) {

      if ( conflict.Local != null && conflict.Incoming != null ) {

        var logger = LogManager.GetCurrentClassLogger();

        // They wont be the same if we got here - they will have been auto resolved.
        var validPackageItems = new List<Tuple<ConflictItemType, Reference>> {
          new Tuple<ConflictItemType, Reference>( ConflictItemType.Local, conflict.Local),
          new Tuple<ConflictItemType, Reference>( ConflictItemType.Incoming, conflict.Incoming)
        }.Where( r => r.Item2.IsOptionValid() ).ToList();

        if ( validPackageItems.Count == 0 ) {
          throw new InvalidResolutonException( conflict.Key );
        }

        // If only one of the two is an installed package, then we can auto resolve to that item.
        if ( validPackageItems.Count == 1 ) {
          var resolvedItem = validPackageItems.Single();

          logger.Info( $"{LogHelper.Header}{Environment.NewLine}Both modified: {conflict.Key}{Environment.NewLine}Picking installed package:{Environment.NewLine}{resolvedItem}" );

          return new MergeResult<Reference>( conflict.Key, resolvedItem.Item2, conflict.GetMergeType(), resolvedItem.Item1 );
        }

        // To get here, they must both be valid options.
        // See if they are the same apart from the version, if so we can auto resolve to the highest version.
        if ( conflict.Local.ReferenceAssemblyName.Version != null && conflict.Incoming.ReferenceAssemblyName.Version != null ) {
          var local = conflict.Local.Clone();
          var incoming = conflict.Incoming.Clone();
          var localName = local.ReferenceAssemblyName;
          var incomingName = incoming.ReferenceAssemblyName;
          var localVersionHigher = localName.Version > incomingName.Version;
          var maxVersion = localVersionHigher ? localName.Version : incomingName.Version;

          localName.Version = maxVersion;
          incomingName.Version = maxVersion;

          local.Include = localName.ToString();
          incoming.Include = incomingName.ToString();

          if ( local == incoming ) {
            var changeDescription = conflict.Base == null ? "added" : "modified";
            var message = $"{LogHelper.Header}{Environment.NewLine}Both {changeDescription}: {conflict.Key}{Environment.NewLine}Picking highest version:{Environment.NewLine}{local}";
            logger.Info( message );
            var resolvedWith = localVersionHigher ? ConflictItemType.Local : ConflictItemType.Incoming;
            return new MergeResult<Reference>( conflict.Key, local, conflict.GetMergeType(), resolvedWith );
          }
        }
      }

      if ( conflict.GetItems().All( i => !i.IsOptionValid() ) ) {
        throw new InvalidResolutonException( conflict.Key );
      }

      // We cannot auto resolve, let the user choose the resolution
      return _userConflictResolver.Resolve( conflict );
    }
  }
}
