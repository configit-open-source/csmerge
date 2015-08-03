using System;
using System.Collections.Generic;

using LibGit2Sharp;

using Reference = Project.Reference;

namespace Integration {

  public static class MergeTypeExtensions {

    private static string _addedSuffix;
    private static string _deletedSuffix;
    private static string _modifiedSuffix;

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
        case MergeType.BothAdded:
          return "Both Added";
        case MergeType.BothDeleted:
          return "Both Deleted";
        case MergeType.BothModified:
          return "Both Modified";
      }

      List<string> values = new List<string>();

      _addedSuffix = "Added";
      _modifiedSuffix = "Modified";
      _deletedSuffix = "Deleted";

      if ( type.HasFlag( MergeType.LocalAdded ) ) {
        values.Add( Local( operation ) + " " + _addedSuffix );
      }
      if ( type.HasFlag( MergeType.LocalDeleted ) ) {
        values.Add( Local( operation ) + " " + _deletedSuffix );
      }
      if ( type.HasFlag( MergeType.LocalModified ) ) {
        values.Add( Local( operation ) + " " + _modifiedSuffix );
      }
      if ( type.HasFlag( MergeType.IncomingAdded ) ) {
        values.Add( Incoming( operation ) + " " + _addedSuffix );
      }
      if ( type.HasFlag( MergeType.IncomingDeleted ) ) {
        values.Add( Incoming( operation ) + " " + _deletedSuffix );
      }
      if ( type.HasFlag( MergeType.IncomingModified ) ) {
        values.Add( Incoming( operation ) + " " + _modifiedSuffix );
      }
      return string.Join( ", ", values );
    }

    public static string ToString( this ConflictItemType conflictItemType, CurrentOperation operation ) {
      switch ( conflictItemType ) {
        case ConflictItemType.Local:
          return Local( operation );
        case ConflictItemType.Incoming:
          return Incoming( operation );
        case ConflictItemType.Unknown:
          return "Custom";
        default:
          return conflictItemType.ToString();
      }
    }

    public static void ApplyIsResolveOption( this Reference reference, ProjectPackages projectPackages ) {
      reference.IsResolveOption = !projectPackages.IsPackageReference( reference.HintPath ) ||
                                   projectPackages.IsPackageReferenced( reference.HintPath );
    }
  }
}
