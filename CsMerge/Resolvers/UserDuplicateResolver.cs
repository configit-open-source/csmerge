using System;
using System.Collections.Generic;
using System.Linq;
using CsMerge.Core;
using CsMerge.Core.Resolvers;
using LibGit2Sharp;

namespace CsMerge.Resolvers {

  internal class UserDuplicateResolver<T>: IDuplicateResolver<T> where T: class, IConflictableItem {

    private readonly string _itemDescriptionWhenNull;
    private readonly string _notResolveOptionText;
    private readonly string _local;
    private readonly string _incoming;

    public UserDuplicateResolver( CurrentOperation operation, string itemDescriptionWhenNull = "Not present", string notResolveOptionText = "Not installed" ) {
      _itemDescriptionWhenNull = itemDescriptionWhenNull;
      _notResolveOptionText = notResolveOptionText;
      _local = MergeTypeExtensions.Local( operation );
      _incoming = MergeTypeExtensions.Incoming( operation );
    }

    public T Resolve( Conflict<IEnumerable<T>> conflict ) {

      var allOptions = GetAllOptions( conflict ).ToList();

      string questionText = string.Join( Environment.NewLine, allOptions.Select( FormatQuestionOptionForItem ) );

      var options = allOptions.Where( o => o.Item.IsOptionValid() ).ToDictionary( o => o.OptionKey, o => o.Item );

      if ( !options.Any() ) {
        throw new InvalidResolutonException( conflict.Key );
      }

      var userQuestion = new UserQuestion<T>( questionText, options );

      return userQuestion.Resolve();
    }

    private IEnumerable<DuplicateItemOption<T>> GetAllOptions( Conflict<IEnumerable<T>> conflict ) {

      var baseOptions = GetOptions( conflict.Base, "Base" );
      var localOptions = GetOptions( conflict.Local, _local );
      var incomingOptions = GetOptions( conflict.Incoming, _incoming );

      return baseOptions.Union( localOptions ).Union( incomingOptions );
    }

    private IEnumerable<DuplicateItemOption<T>> GetOptions( IEnumerable<T> items, string optionPrefix ) {

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

    private string FormatQuestionOptionForItem( DuplicateItemOption<T> option ) {
      if ( option.Item.IsOptionValid() ) {
        return string.Format( "({1}) {2}:{0}{3}{0}",
          Environment.NewLine,
          option.OptionKey,
          option.OptionName,
          ToStringOrDefault( option.Item )
          );
      }

      return string.Format( "{2} ({1}):{0}{3}{0}",
          Environment.NewLine,
          _notResolveOptionText,
          option.OptionName,
          ToStringOrDefault( option.Item )
          );
    }

    private string ToStringOrDefault( T item ) {
      return item == null ? _itemDescriptionWhenNull : item.ToString();
    }
  }
}
