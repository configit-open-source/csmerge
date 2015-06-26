using System;
using System.Collections.Generic;
using System.Text;

using LibGit2Sharp;

namespace CsMerge {
  [Flags]
  public enum MergeType {
    NoChanges = 0,
    LocalDeleted = 1 << 0,
    IncomingDeleted = 1 << 1,
    LocalAdded = 1 << 2,
    IncomingAdded = 1 << 3,
    IncomingModified = 1 << 4,
    LocalModified = 1 << 5
  }

  public static class MergeTypeExtensions {
    private static string AddedSuffix;

    private static string DeletedSuffix;

    private static string ModifiedSuffix;

    public const string Mine = "Mine";

    public const string Theirs = "Theirs";

    public static string Local( CurrentOperation operation ) {
      if ( operation == CurrentOperation.Merge || operation == CurrentOperation.Revert ) {
        return Mine;
      }
      if ( operation == CurrentOperation.Rebase ||
           operation == CurrentOperation.RebaseInteractive ||
           operation == CurrentOperation.RebaseMerge ||
           operation == CurrentOperation.CherryPick ) {
        return Theirs;
      }
      throw new NotSupportedException( "Operation type : " + operation );
    }

    public static string Incoming( CurrentOperation operation ) {
      return Local( operation ) == Mine ? Theirs : Mine;
    }

    public static string ToString( this MergeType type, CurrentOperation operation ) {
      switch ( type ) {
        case MergeType.NoChanges:
          return type.ToString();
        case ( MergeType.LocalAdded | MergeType.IncomingAdded ):
          return "BothAdded";
        case ( MergeType.LocalDeleted | MergeType.IncomingDeleted ):
          return "BothDeleted";
      }

      List<string> values = new List<string>();

      AddedSuffix = "Added";
      ModifiedSuffix = "Modified";
      DeletedSuffix = "Deleted";

      if ( type.HasFlag( MergeType.LocalAdded ) ) {
        values.Add( Local( operation ) + AddedSuffix );
      }
      if ( type.HasFlag( MergeType.LocalDeleted ) ) {
        values.Add( Local( operation ) + DeletedSuffix );
      }
      if ( type.HasFlag( MergeType.LocalModified ) ) {
        values.Add( Local( operation ) + ModifiedSuffix );
      }
      if ( type.HasFlag( MergeType.IncomingAdded ) ) {
        values.Add( Incoming( operation ) + AddedSuffix );
      }
      if ( type.HasFlag( MergeType.IncomingDeleted ) ) {
        values.Add( Incoming( operation ) + DeletedSuffix );
      }
      if ( type.HasFlag( MergeType.IncomingModified ) ) {
        values.Add( Incoming( operation ) + ModifiedSuffix );
      }
      return string.Join( "+", values );
    }
  }
}