using System;

namespace CsMerge.UserQuestion {
  public class UserQuestionLiteralOption<T>: IUserQuestionOption<T> {

    public string OptionKey { get; private set; }
    public T Value { get; private set; }

    public string OptionDescription { get; set; }

    public UserQuestionLiteralOption( string optionKey, string optionDescription, T value ) {
      OptionKey = optionKey;
      Value = value;
      OptionDescription = optionDescription;
    }

    public T GetValue() {
      return Value;
    }

    public virtual string GetQuestionText() {
      return string.Format( "{0}{1}{2}", GetOptionText(), OptionDescription, Environment.NewLine );
    }

    protected virtual string GetOptionText() {
      return string.IsNullOrEmpty( OptionKey ) ? "" : string.Format( "({0}) ", OptionKey );
    }
  }
}
