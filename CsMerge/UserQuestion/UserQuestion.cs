using System;
using System.Collections.Generic;
using System.Linq;

using CsMerge.Core;
using CsMerge.Core.Exceptions;

namespace CsMerge.UserQuestion {

  /// <summary>
  /// A class to ask the user to choose an option and return the correct option
  /// </summary>
  /// <typeparam name="TResult">The type of option.</typeparam>
  public class UserQuestion<TResult> {

    private readonly string _question;
    private readonly IDictionary<string, IUserQuestionOption<TResult>> _options;

    public string ResolvedOption { get; private set; }

    /// <summary>
    /// A class to ask the user to choose an option and return the correct option
    /// </summary>
    /// <param name="options">A dictionary of options, keyed by the key(s) the user will enter to select the option.</param>
    public UserQuestion( IEnumerable<IUserQuestionOption<TResult>> options ) {
      var optionsList = options.ToList();
      _question = BuildQuestionText( optionsList );
      _options = optionsList.ToDictionary( o => o.OptionKey.ToLower() );
    }

    /// <summary>
    /// A class to ask the user to choose an option and return the correct option
    /// </summary>
    /// <param name="questionText">The question to display to the user.</param>
    /// <param name="options">A dictionary of options, keyed by the key(s) the user will enter to select the option.</param>
    public UserQuestion( string questionText, IEnumerable<IUserQuestionOption<TResult>> options ) {
      var optionsList = options.Where( o => !string.IsNullOrEmpty( o.OptionKey ) ).ToList();
      _question = questionText;
      _options = optionsList.ToDictionary( o => o.OptionKey.ToLower() );
    }

    /// <summary>
    /// Asks the question and resolves the chosen option.
    /// </summary>
    /// <returns>Returns the option chosen by the user.</returns>
    public TResult Resolve() {

      AskQuestion();

      try {

        while ( true ) {

          var userResponse = ReadUserInput();

          try {
            if ( _options.ContainsKey( userResponse ) ) {
              ResolvedOption = userResponse;
              return _options[userResponse].GetValue();
            } else {
              Console.WriteLine( " not recognised. Please try again." );
            }
          } catch ( MergeAbortException ) {
            throw;
          } catch ( UserQuitException ) {
            throw;
          } catch ( Exception exception ) {
            Console.WriteLine();
            Console.WriteLine( "An error occurred: {0}{1}Please try again.", exception.Message, Environment.NewLine );
          }
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

      Console.WriteLine( LogHelper.Header );

      foreach ( var line in _question.Split( new[] { Environment.NewLine }, StringSplitOptions.None ) ) {
        Console.WriteLine( line );
      }
    }

    /// <summary>
    /// Creates standard Yes/No options.
    /// </summary>
    public static IUserQuestionOption<bool>[] YesNoOptions() {
      return new IUserQuestionOption<bool>[] {
        new UserQuestionLiteralOption<bool>( "y", "Yes", true ),
        new UserQuestionLiteralOption<bool>( "n", "No", false )
      };
    }

    public static IUserQuestionOption<bool?>[] YesNoQuitOptions() {
      return new IUserQuestionOption<bool?>[] {
        new UserQuestionLiteralOption<bool?>( "y", "Yes", true ),
        new UserQuestionLiteralOption<bool?>( "n", "No", false ),
        new UserQuestionLiteralOption<bool?>( "q", "Quit", null )
      };
    }

    public static string BuildQuestionText( IEnumerable<IUserQuestionOption<TResult>> options, string prefixText = null ) {
      var prefixWithNewLine = string.IsNullOrEmpty( prefixText ) ? "" : prefixText + Environment.NewLine + Environment.NewLine;
      var questionText = prefixWithNewLine + string.Join( Environment.NewLine, options.Select( o => o.GetQuestionText() ) );

      return questionText;
    }
  }
}
