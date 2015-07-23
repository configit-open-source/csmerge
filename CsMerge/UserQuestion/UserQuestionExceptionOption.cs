using System;

namespace CsMerge.UserQuestion {

  public class UserQuestionExceptionOption<T, TException>: UserQuestionActionOption<T> where TException: Exception, new() {

    public UserQuestionExceptionOption( string optionKey, string questionText )
      : base( optionKey, questionText, ThrowException ) {

    }

    private static T ThrowException() {
      throw new TException();
    }
  }
}
