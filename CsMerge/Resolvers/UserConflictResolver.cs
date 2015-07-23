using CsMerge.Core;
using CsMerge.Core.Exceptions;
using CsMerge.Core.Resolvers;
using CsMerge.UserQuestion;
using LibGit2Sharp;

namespace CsMerge.Resolvers {
  public class UserConflictResolver<T>: IConflictResolver<T> where T: class, IConflictableItem {

    private readonly string _itemDescriptionWhenNull;
    private readonly string _notResolveOptionText;
    private readonly string _local;
    private readonly string _incoming;
    private readonly string _repositoryRootDirectory;

    public UserConflictResolver( CurrentOperation operation, string itemDescriptionWhenNull = "Not present", string notResolveOptionText = "Not installed", string repositoryRootDirectory = null ) {
      _notResolveOptionText = notResolveOptionText;
      _repositoryRootDirectory = repositoryRootDirectory;
      _itemDescriptionWhenNull = itemDescriptionWhenNull;

      _local = MergeTypeExtensions.Local( operation );
      _incoming = MergeTypeExtensions.Incoming( operation );
    }

    public T Resolve( Conflict<T> conflict ) {

      var options = new UserQuestionOptionsCollection<T>();

      options.AddConditional( "Base", conflict.Base, _itemDescriptionWhenNull, conflict.Base.IsOptionValid(), _notResolveOptionText );
      options.AddConditional( _local, conflict.Local, _itemDescriptionWhenNull, conflict.Local.IsOptionValid(), _notResolveOptionText );
      options.AddConditional( _incoming, conflict.Incoming, _itemDescriptionWhenNull, conflict.Incoming.IsOptionValid(), _notResolveOptionText );

      if ( !string.IsNullOrEmpty( _repositoryRootDirectory ) ) {
        var gitResolver = new GitMergeToolResolver<T>( _repositoryRootDirectory, conflict );
        options.Add( "G", gitResolver.Resolve, "Git Merge Tool" );
      }

      options.Add<MergeAbortException>( "S", "Skip this file" );
      options.Add<UserQuitException>( "Q", "Quit" );

      var questionText = UserQuestion<T>.BuildQuestionText( options, string.Format( "Please resolve conflict in file: {0}", conflict.FilePath ) );

      UserQuestion<T> userQuestion = new UserQuestion<T>( questionText, options );

      return userQuestion.Resolve();
    }
  }
}
