using System;

namespace CsMerge.UserQuestion {
  public class UserQuestionLiteralWithDescriptionOption<T>: UserQuestionLiteralOption<T> {

    private string QuestionTextWhenNull { get; set; }

    public UserQuestionLiteralWithDescriptionOption( string optionKey, string optionDescription, T value, string questionTextWhenNull )
      : base( optionKey, optionDescription, value ) {
      QuestionTextWhenNull = questionTextWhenNull;
    }

    public override string GetQuestionText() {
      return string.Format( "{0}{1}:{2}{3}{2}",
        GetOptionText(),
        OptionDescription,
        Environment.NewLine,
        ValueStringOrDefault() );
    }

    private string ValueStringOrDefault() {
      return Value == null ? QuestionTextWhenNull : Value.ToString();
    }
  }
}
