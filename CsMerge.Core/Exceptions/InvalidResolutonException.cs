using System;

namespace CsMerge.Core.Exceptions {
  public class InvalidResolutonException: Exception {

    public InvalidResolutonException( string itemKey )
      : base( string.Format( "Resolution for '{0}' was invalid.", itemKey ) ) {
    }
  }
}
