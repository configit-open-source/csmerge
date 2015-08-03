namespace CsMerge {
  public enum Mode {
    /// <summary>
    /// Handles conflicts in packages.config and project files.
    /// </summary>
    Merge,
    /// <summary>
    /// Aligns references across packages.config files 
    /// </summary>
    Align,
  }
}