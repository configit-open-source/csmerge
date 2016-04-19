using CsMerge.Core;
using CsMerge.Core.Exceptions;
using CsMerge.Core.Resolvers;
using CsMerge.UserQuestion;
using LibGit2Sharp;

using Integration;

using Project;

namespace CsMerge.Resolvers {
  public class UserConflictResolver<T>: IConflictResolver<T> where T : class, IConflictableItem {

    private readonly string _itemDescriptionWhenNull;
    private readonly string _notResolveOptionText;
    private readonly string _local;
    private readonly string _incoming;
    private readonly string _repositoryRootDirectory;

    public UserConflictResolver( CurrentOperation operation, string itemDescriptionWhenNull = "Not present", string notResolveOptionText = "Not installed", string repositoryRootDirectory = null ) {
      _notResolveOptionText = notResolveOptionText;
      _repositoryRootDirectory = repositoryRootDirectory;
      _itemDescriptionWhenNull = itemDescriptionWhenNull;

      _local = MergeTypeIntegrationExtensions.Local( operation );
      _incoming = MergeTypeIntegrationExtensions.Incoming( operation );
    }

    public MergeResult<T> Resolve( Conflict<T> conflict ) {

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

      var questionText = UserQuestion<T>.BuildQuestionText( options, $"Please resolve conflict in file: {conflict.FilePath}" );

      UserQuestion<T> userQuestion = new UserQuestion<T>( questionText, options );

      var resolvedItem = userQuestion.Resolve();

      ConflictItemType resolvedWith;

      var userOption = userQuestion.ResolvedOption.ToLower();

      if ( userOption == "b" ) {
        resolvedWith = ConflictItemType.Base;
      } else if ( userOption == _local[0].ToString().ToLower() ) {
        resolvedWith = ConflictItemType.Local;
      } else if ( userOption == _incoming[0].ToString().ToLower() ) {
        resolvedWith = ConflictItemType.Incoming;
      } else {
        resolvedWith = ConflictItemType.Unknown;
      }

      return new MergeResult<T>( conflict.Key, resolvedItem, conflict.GetMergeType(), resolvedWith );
    }
  }
}
