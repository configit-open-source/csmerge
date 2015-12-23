using System;
using System.Collections.Generic;
using System.Linq;

using CsMerge.Core.Exceptions;

using NLog;
using Project;

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
        if ( conflict.Local.ReferenceAssemblyName != null && conflict.Incoming.ReferenceAssemblyVersion != null ) {
          var local = conflict.Local;
          var incoming = conflict.Incoming;
          var localName = local.GetAssemblyName();
          var incomingName = incoming.GetAssemblyName();
          var localVersionHigher = localName.Version > incomingName.Version;
          var maxVersion = localVersionHigher ? localName.Version : incomingName.Version;

          localName.Version = maxVersion;
          incomingName.Version = maxVersion;

          local = local.CloneWith( include: localName.ToString() );
          incoming = incoming.CloneWith( include: localName.ToString() );

          if ( local == incoming ) {
            var changeDescription = conflict.Base == null ? "added" : "modified";
            var message = $"{LogHelper.Header}{Environment.NewLine}Both {changeDescription}: {conflict.Key}{Environment.NewLine}Picking highest version:{Environment.NewLine}{local}";
            logger.Info( message );
            var resolvedWith = localVersionHigher ? ConflictItemType.Local : ConflictItemType.Incoming;
            var resolvedItem = localVersionHigher ? local : incoming;
            return new MergeResult<Reference>( conflict.Key, resolvedItem, conflict.GetMergeType(), resolvedWith );
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