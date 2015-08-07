using System;
using System.Collections.Generic;
using System.Linq;

using NLog;

using Project;

namespace CsMerge.Core.Resolvers {

  public class ReferenceConflictResolver: IConflictResolver<Reference> {

    private readonly IConflictResolver<Reference> _userConflictResolver;

    public ReferenceConflictResolver( IConflictResolver<Reference> userConflictResolver ) {
      _userConflictResolver = userConflictResolver;
    }

    public Reference Resolve( Conflict<Reference> conflict ) {

      if ( conflict.Local != null && conflict.Incoming != null ) {

        var logger = LogManager.GetCurrentClassLogger();

        // They wont be the same if we got here. They will have been auto resolved in the base class.
        var validPackageItems = new List<Reference> { conflict.Local, conflict.Incoming }.Where( r => r.IsOptionValid() ).ToList();

        if ( validPackageItems.Count == 0 ) {
          throw new InvalidResolutonException( conflict.Key );
        }

        // If only one of the two is an installed package, then we can auto resolve to that item.
        if ( validPackageItems.Count == 1 ) {
          var resolvedItem = validPackageItems.Single();

          logger.Info( "Both modified: {1}{0}Picking installed package:{0}{2}", Environment.NewLine, conflict.Key, resolvedItem );

          return resolvedItem;
        }

        // To get here, they must both be valid options.
        // See if they are the same apart from the version, if so we can auto resolve to the highest version.
        var local = conflict.Local.Clone();
        var incoming = conflict.Incoming.Clone();
        var localName = local.GetAssemblyName();
        var incomingName = incoming.GetAssemblyName(); // TODO: This can throw an exception if version is unparsable (say $(MyVersion))
        var maxVersion = localName.Version > incomingName.Version ? localName.Version : incomingName.Version;

        localName.Version = maxVersion;
        incomingName.Version = maxVersion;

        local.Include = localName.ToString();
        incoming.Include = incomingName.ToString();

        if ( local == incoming ) {
          logger.Info( "Both modified: {1}{0}Picking highest version:{0}{2}", Environment.NewLine, conflict.Key, local );
          return local;
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
