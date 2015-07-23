namespace CsMerge.UserQuestion {
  public interface IUserQuestionOption<T> {
    string OptionKey { get; }
    T GetValue();
    string GetQuestionText();
  }
}
