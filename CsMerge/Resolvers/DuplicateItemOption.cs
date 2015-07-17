namespace CsMerge.Resolvers {
  internal class DuplicateItemOption<T> {
    public string OptionName { get; set; }

    public string OptionKey { get; set; }

    public T Item { get; set; }
  }
}
