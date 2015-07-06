using System;
using System.Collections.Generic;
using System.Linq;

namespace CsMerge {

  public class UserQuestion<TResult> {

    private readonly string _question;
    private readonly IDictionary<string, TResult> _options;

    public UserQuestion( string question, IDictionary<string, TResult> options ) {
      _question = question;
      _options = options.ToDictionary( o => o.Key.ToLower(), o => o.Value );
    }

    public TResult Resolve() {

      AskQuestion();

      try {

        while ( true ) {

          string key = Console.ReadKey().KeyChar.ToString().ToLowerInvariant();

          TResult result;

          if ( TryParseResponse( key, out result ) ) {
            return result;
          }
        }
      } finally {
        Console.WriteLine();
      }
    }

    protected virtual void AskQuestion() {
      
      Console.WriteLine();

      foreach ( var line in _question.Split( new[] { Environment.NewLine }, StringSplitOptions.None ) ) {
        Console.WriteLine( line );
      }
    }

    protected virtual bool TryParseResponse( string userResponse, out TResult result ) {

      if ( !_options.ContainsKey( userResponse ) ) {
        result = default( TResult );
        return false;
      }

      result = _options[userResponse];
      return true;
    }

    public static IDictionary<string, bool> YesNoOptions() {
      return new Dictionary<string, bool> {
        { "y", true },
        { "n", false }
      };
    }
  }
}
