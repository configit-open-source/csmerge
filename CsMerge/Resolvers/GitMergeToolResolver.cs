using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CsMerge.Core;
using CsMerge.Core.Exceptions;

using Integration;

using Project;

using SerialisationHelper = CsMerge.Core.SerialisationHelper;

namespace CsMerge.Resolvers {

  public class GitMergeToolResolver<T> where T: class, IConflictableItem {

    private readonly string _repositoryRootDirectory;
    private readonly string _key;
    private readonly IEnumerable<XElement> _baseElements;
    private readonly IEnumerable<XElement> _localElements;
    private readonly IEnumerable<XElement> _incomingElements;

    public GitMergeToolResolver( string repositoryRootDirectory, Conflict<T> conflict ) {
      _key = conflict.Key;
      _repositoryRootDirectory = repositoryRootDirectory;
      _baseElements = GetElements( conflict.Base );
      _localElements = GetElements( conflict.Local );
      _incomingElements = GetElements( conflict.Incoming );
    }

    public GitMergeToolResolver( string repositoryRootDirectory, Conflict<IEnumerable<T>> conflict ) {
      _key = conflict.Key;
      _repositoryRootDirectory = repositoryRootDirectory;
      _baseElements = conflict.Base.WhereNotNull().Select( i => i.ToElement( "" ) );
      _localElements = conflict.Local.WhereNotNull().Select( i => i.ToElement( "" ) );
      _incomingElements = conflict.Incoming.WhereNotNull().Select( i => i.ToElement( "" ) );
    }

    public T Resolve() {
      var xmlElement = GitHelper.ResolveWithStandardMergetool(
        _repositoryRootDirectory,
        _key,
        _baseElements,
        _localElements,
        _incomingElements
        );

      if ( xmlElement == null ) {
        return null;
      }

      var resolved = SerialisationHelper.ParseAsConflictableItem( xmlElement ) as T;

      if ( resolved != null ) {
        return resolved;
      }

      throw new InvalidResolutonException( _key );
    }
    
    private static IEnumerable<XElement> GetElements( T item ) {
      return item == null ? new XElement[0] : new[] { item.ToElement( "" ) };
    }
  }
}
