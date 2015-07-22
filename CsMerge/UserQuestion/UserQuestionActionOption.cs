using System;

namespace CsMerge.UserQuestion {

  public class UserQuestionActionOption<T>: IUserQuestionOption<T> {

    public string OptionKey { get; private set; }

    private Func<T> Action { get; set; }

    private string QuestionText { get; set; }

    public UserQuestionActionOption( string optionKey, string questionText, Func<T> action ) {
      OptionKey = optionKey;
      Action = action;
      QuestionText = questionText;
    }

    public T GetValue() {
      return Action();
    }

    public string GetQuestionText() {
      return string.Format( "({0}) {1}{2}", OptionKey, QuestionText, Environment.NewLine );
    }
  }
}
