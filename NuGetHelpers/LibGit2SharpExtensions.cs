using LibGit2Sharp;

namespace Integration {
  public static class LibGit2SharpExtensions {

    public static string GetPath( this Conflict conflict ) {
      return ( conflict.Ours ?? conflict.Theirs ?? conflict.Ancestor ).Path;
    }

    public static IndexEntry GetEntry( this Conflict conflict, StageLevel stageLevel ) {

      switch ( stageLevel ) {
        case StageLevel.Ancestor:
          return conflict.Ancestor;
        case StageLevel.Ours:
          return conflict.Ours;
        case StageLevel.Theirs:
          return conflict.Theirs;
        default:
          return null;
      }
    }
  }
}
