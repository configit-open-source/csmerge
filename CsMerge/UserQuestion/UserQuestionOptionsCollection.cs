using System;
using System.Collections.Generic;

namespace CsMerge.UserQuestion {
  public class UserQuestionOptionsCollection<T>: List<IUserQuestionOption<T>> {

    public void Add( string optionKey, string optionDescription, T value, string textWhenNull ) {
      Add( new UserQuestionLiteralWithDescriptionOption<T>( optionKey, optionDescription, value, textWhenNull ) );
    }

    public void Add( string optionKey, Func<T> action, string questionText ) {
      Add( new UserQuestionActionOption<T>( optionKey, questionText, action ) );
    }

    public void Add<TException>( string optionKey, string questionText ) where TException: Exception, new() {
      Add( new UserQuestionExceptionOption<T, TException>( optionKey, questionText ) );
    }

    public void AddConditional( string optionDescription, T value, string textWhenNull, bool available, string textWhenNotAvailable ) {
      AddConditional( optionDescription[0].ToString(), optionDescription, value, textWhenNull, available, textWhenNotAvailable );
    }

    public void AddConditional( string optionKey, string optionDescription, T value, string textWhenNull, bool available, string textWhenNotAvailable ) {
      if ( available ) {
        Add( new UserQuestionLiteralWithDescriptionOption<T>( optionKey, optionDescription, value, textWhenNull ) );
      } else {
        Add( new UserQuestionLiteralWithDescriptionOption<T>( null, string.Format( "{0} ({1})", optionDescription, textWhenNotAvailable ), value, textWhenNull ) );
      }
    }
  }
}
