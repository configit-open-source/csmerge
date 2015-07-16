using System;
using System.Collections.Generic;
using System.Linq;

namespace CsMerge {

  /// <summary>
  /// A class to ask the user to choose an option and return the correct option
  /// </summary>
  /// <typeparam name="TResult">The type of option.</typeparam>
  public class UserQuestion<TResult> {

    private readonly string _question;
    private readonly IDictionary<string, TResult> _options;

    /// <summary>
    /// A class to ask the user to choose an option and return the correct option
    /// </summary>
    /// <param name="question">The question to ask.</param>
    /// <param name="options">A dictionary of options, keyed by the key(s) the user will enter to select the option.</param>
    public UserQuestion( string question, IDictionary<string, TResult> options ) {
      _question = question;
      _options = options.ToDictionary( o => o.Key.ToLower(), o => o.Value );
    }

    /// <summary>
    /// Asks the question and resolves the chosen option.
    /// </summary>
    /// <returns>Returns the option chosen by the user.</returns>
    public TResult Resolve() {

      AskQuestion();

      try {

        while ( true ) {

          string key = ReadUserInput();

          TResult result;

          if ( TryParseResponse( key, out result ) ) {
            return result;
          }

          Console.WriteLine( " not recognised. Please try again." );
        }
      } finally {
        Console.WriteLine();
      }
    }

    private string ReadUserInput() {

      string input = "";
      int maxLength = _options.Keys.Select( k => k.Length ).Max();

      while ( true ) {
        input += Console.ReadKey().KeyChar.ToString().ToLowerInvariant();

        if ( input.Length >= maxLength || _options.ContainsKey( input ) ) {
          return input;
        }
      }
    }

    private void AskQuestion() {

      Console.WriteLine( string.Empty.PadLeft( 70, '-' ) );

      foreach ( var line in _question.Split( new[] { Environment.NewLine }, StringSplitOptions.None ) ) {
        Console.WriteLine( line );
      }
    }

    private bool TryParseResponse( string userResponse, out TResult result ) {

      if ( !_options.ContainsKey( userResponse ) ) {
        result = default( TResult );
        return false;
      }

      result = _options[userResponse];
      return true;
    }

    /// <summary>
    /// Creates standard Yes/No options.
    /// </summary>
    public static IDictionary<string, bool> YesNoOptions() {
      return new Dictionary<string, bool> {
        { "y", true },
        { "n", false }
      };
    }
  }
}
