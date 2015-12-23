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

  internal class UserDuplicateResolver<T>: IDuplicateResolver<T> where T : class, IConflictableItem {

    private readonly string _itemDescriptionWhenNull;
    private readonly string _notResolveOptionText;
    private readonly string _local;
    private readonly string _incoming;
    private readonly string _repositoryRootDirectory;

    public UserDuplicateResolver( CurrentOperation operation, string itemDescriptionWhenNull = "Not present", string notResolveOptionText = "Not installed", string repositoryRootDirectory = null ) {
      _repositoryRootDirectory = repositoryRootDirectory;
      _itemDescriptionWhenNull = itemDescriptionWhenNull;
      _notResolveOptionText = notResolveOptionText;
      _local = MergeTypeIntegrationExtensions.Local( operation );
      _incoming = MergeTypeIntegrationExtensions.Incoming( operation );
    }

    public MergeResult<T> Resolve( Conflict<IEnumerable<T>> conflict ) {

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

      var questionText = UserQuestion<T>.BuildQuestionText( options, $"Please resolve conflict in file: {conflict.FilePath}" );

      var userQuestion = new UserQuestion<T>( questionText, options );

      var resolvedItem = userQuestion.Resolve();

      ConflictItemType resolvedWith;

      var userOption = userQuestion.ResolvedOption.ToLower();

      if ( userOption.StartsWith( "b" ) ) {
        resolvedWith = ConflictItemType.Base;
      } else if ( userOption.StartsWith( _local[0].ToString().ToLower() ) ) {
        resolvedWith = ConflictItemType.Local;
      } else if ( userOption.StartsWith( _incoming[0].ToString().ToLower() ) ) {
        resolvedWith = ConflictItemType.Incoming;
      } else {
        resolvedWith = ConflictItemType.Unknown;
      }

      return new MergeResult<T>( conflict.Key, resolvedItem, conflict.GetMergeType(), resolvedWith );
    }

    private IUserQuestionOption<T> ToUserQuestionOption( DuplicateItemOption<T> option ) {
      return option.Item.IsOptionValid()
        ? new UserQuestionLiteralWithDescriptionOption<T>( option.OptionKey, option.OptionName, option.Item, _itemDescriptionWhenNull )
        : new UserQuestionLiteralWithDescriptionOption<T>( string.Empty, $"{option.OptionName} ({_notResolveOptionText})", option.Item, _itemDescriptionWhenNull );
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
      var paddedItemNumber = PadItemNumber( itemNumber, itemCount );
      return itemCount > 1 ? $"{optionName} {paddedItemNumber}" : optionName;
    }

    private static string GetOptionKey( string optionName, int itemNumber, int itemCount ) {
      var optionKeyPrefix = optionName[0].ToString().ToUpper();
      var paddedItemNumber = PadItemNumber( itemNumber, itemCount );
      return itemCount > 1 ? optionKeyPrefix + paddedItemNumber : optionKeyPrefix;
    }

    private static string PadItemNumber( int itemNumber, int itemCount ) {
      return itemNumber.ToString( "D" + itemCount.ToString().Length );
    }

  }
}
