using System.Collections.Generic;
using System.Linq;
using CsMerge.Core;
using CsMerge.Core.Exceptions;
using CsMerge.Core.Resolvers;
using CsMerge.UserQuestion;

using LibGit2Sharp;

using Integration;

using Project;

namespace CsMerge.Resolvers {

  internal class UserDuplicateResolver<T>: IDuplicateResolver<T> where T: class, IConflictableItem {

    private readonly string _itemDescriptionWhenNull;
    private readonly string _notResolveOptionText;
    private readonly string _local;
    private readonly string _incoming;
    private readonly string _repositoryRootDirectory;

    public UserDuplicateResolver( CurrentOperation operation, string itemDescriptionWhenNull = "Not present", string notResolveOptionText = "Not installed", string repositoryRootDirectory = null ) {
      _repositoryRootDirectory = repositoryRootDirectory;
      _itemDescriptionWhenNull = itemDescriptionWhenNull;
      _notResolveOptionText = notResolveOptionText;
      _local = MergeTypeExtensions.Local( operation );
      _incoming = MergeTypeExtensions.Incoming( operation );
    }

    public T Resolve( Conflict<IEnumerable<T>> conflict ) {

      var allOptions = GetAllOptions( conflict ).ToList();

      if ( !allOptions.Any( o => o.Item.IsOptionValid() ) ) {
        throw new InvalidResolutonException( conflict.Key );
      }

      var options = new UserQuestionOptionsCollection<T>();

      options.AddRange( allOptions.Select( ToUserQuestionOption ) );

      if ( !string.IsNullOrEmpty( _repositoryRootDirectory ) ) {
        var gitResolver = new GitMergeToolResolver<T>( _repositoryRootDirectory, conflict );
        options.Add( "G", gitResolver.Resolve, "Git Merge Tool" );
      }

      options.Add<MergeAbortException>( "S", "Skip this file" );
      options.Add<UserQuitException>( "Q", "Quit" );

      var questionText = UserQuestion<T>.BuildQuestionText( options, string.Format( "Please resolve conflict in file: {0}", conflict.FilePath ) );

      var userQuestion = new UserQuestion<T>( questionText, options );

      return userQuestion.Resolve();
    }

    private IUserQuestionOption<T> ToUserQuestionOption( DuplicateItemOption<T> option ) {
      return option.Item.IsOptionValid()
        ? new UserQuestionLiteralWithDescriptionOption<T>( option.OptionKey, option.OptionName, option.Item, _itemDescriptionWhenNull )
        : new UserQuestionLiteralWithDescriptionOption<T>( string.Empty, string.Format( "{0} ({1})", option.OptionName, _notResolveOptionText ), option.Item, _itemDescriptionWhenNull );
    }

    private IEnumerable<DuplicateItemOption<T>> GetAllOptions( IConflict<IEnumerable<T>> conflict ) {

      var baseOptions = GetOptions( conflict.Base, "Base" );
      var localOptions = GetOptions( conflict.Local, _local );
      var incomingOptions = GetOptions( conflict.Incoming, _incoming );

      return baseOptions.Union( localOptions ).Union( incomingOptions );
    }

    private static IEnumerable<DuplicateItemOption<T>> GetOptions( IEnumerable<T> items, string optionPrefix ) {

      var itemsList = items.Distinct().ToList();

      if ( !itemsList.Any() ) {
        return new[] { new DuplicateItemOption<T> {
          OptionKey = GetOptionKey( optionPrefix, 1, 1 ),
          OptionName = GetOptionName( optionPrefix, 1, 1 )
        } };
      }

      return Enumerable.Range( 1, itemsList.Count ).Select( i => new DuplicateItemOption<T> {
        OptionKey = GetOptionKey( optionPrefix, i, itemsList.Count ),
        OptionName = GetOptionName( optionPrefix, i, itemsList.Count ),
        Item = itemsList[i - 1]
      } );
    }

    private static string GetOptionName( string optionName, int itemNumber, int itemCount ) {
      return itemCount > 1 ? string.Format( "{0} {1}", optionName, itemNumber ) : optionName;
    }

    private static string GetOptionKey( string optionName, int itemNumber, int itemCount ) {
      var optionKeyPrefix = optionName[0].ToString().ToUpper();
      return itemCount > 1 ? optionKeyPrefix + itemNumber : optionKeyPrefix;
    }

  }
}
