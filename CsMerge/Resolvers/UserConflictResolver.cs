using System;
using System.Collections.Generic;
using CsMerge.Core;
using CsMerge.Core.Resolvers;

using LibGit2Sharp;

namespace CsMerge.Resolvers {
  internal class UserConflictResolver<T>: IConflictResolver<T> where T: IConflictableItem {

    private readonly string _itemDescriptionWhenNull;
    private readonly string _notResolveOptionText;
    private readonly string _local;
    private readonly string _incoming;

    public UserConflictResolver( CurrentOperation operation, string itemDescriptionWhenNull = "Not present", string notResolveOptionText = "Not installed" ) {
      _notResolveOptionText = notResolveOptionText;
      _itemDescriptionWhenNull = itemDescriptionWhenNull;

      _local = MergeTypeExtensions.Local( operation );
      _incoming = MergeTypeExtensions.Incoming( operation );
    }

    public T Resolve( Conflict<T> conflict ) {

      string questionText = string.Format( "{1}{0}{2}{0}{3}",
        Environment.NewLine,
        FormatQuestionOption( "Base", conflict.Base ),
        FormatQuestionOption( _local, conflict.Local ),
        FormatQuestionOption( _incoming, conflict.Incoming )
        );

      var options = new Dictionary<string, T>();

      if ( conflict.Base.IsOptionValid() ) {
        options.Add( "b", conflict.Base );
      }

      if ( conflict.Local.IsOptionValid() ) {
        options.Add( _local[0].ToString(), conflict.Local );
      }

      if ( conflict.Incoming.IsOptionValid() ) {
        options.Add( _incoming[0].ToString(), conflict.Incoming );
      }

      UserQuestion<T> userQuestion = new UserQuestion<T>( questionText, options );

      return userQuestion.Resolve();
    }

    private string FormatQuestionOption( string option, T item ) {

      if ( item.IsOptionValid() ) {
        return string.Format( "({1}){2}:{0}{3}{0}",
          Environment.NewLine,
          option[0].ToString().ToUpper(),
          option.Substring( 1 ),
          ToStringOrDefault( item )
          );
      }

      return string.Format( "{2} ({1}):{0}{3}{0}",
          Environment.NewLine,
          _notResolveOptionText,
          option,
          ToStringOrDefault( item )
          );
    }

    private string ToStringOrDefault( T item ) {
      return item == null ? _itemDescriptionWhenNull : item.ToString();
    }
  }
}
