using System.Text.RegularExpressions;

namespace Formula.Frontend;

/// <summary>
///     <para>
///         A tokenizer compliant with the tokenization rules for PS1 (University of Utah, CS3500, Spring 2026); for a
///         longer explanation, see <see cref="Formula"/>. Given some source formula text, this tokenizer will tokenize
///         it into <see cref="SyntaxToken"/>.
///     </para>
///     <para>
///         This tokenizer is based on regular expressions, for two main reasons:
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Compliance</term>
///             <description>
///                 A guaranteed compliant set of regular expressions for each token type were provided as part of the
///                 assignment description. By using these, we can eliminate a whole class of bugs.
///             </description>
///         </item>
///         <item>
///             <term>Simplicity</term>
///             <description>
///                 It is simpler than writing a tokenizer, even for formulas this simple. Time is of the essence!
///             </description>
///         </item>
///     </list>
///     <para>
///         Eventually, and as the grammar gets more complex, it will likely be worth rewriting this by-hand. It
///         shouldn't be too hard, and will allow us more control (e.g. tracking line numbers). It should also be
///         more performance, although I would imagine formulas are pretty small, so I don't think this is too much
///         of an issue.
///     </para>
///     <para>
///         Spaces are significant only insofar in that they delimit tokens. For example, "xy" is a single variable,
///         "x y" consists of two variables "x" and y; "x23" is a single variable;  and "x 23" consists of a variable
///         "x" and a number "23". Otherwise, spaces are to be removed.
///     </para>
///     <para>
///         Errors are reported in-band via <see cref="SyntaxTokenKind.Error"/> tokens; methods in this class will not
///         throw exceptions unless otherwise noted.
///     </para>
/// </summary>
/// <param name="text">The text, or formula source, to tokenize.</param>
public sealed class Tokenizer(string text)
{
    /// <summary>
    ///     A mapping of <see cref="SyntaxTokenKind">SyntaxTokenKinds</see> to regular expression patterns.
    /// </summary>
    private static readonly Dictionary<SyntaxTokenKind, string> TokenPatterns = new()
    {
        // A number, in base-10 decimal notation, possibly with a fractional and/or exponent part.
        [SyntaxTokenKind.NumericLiteral] = @"(?:\d+\.\d*|\d*\.\d+|\d+)(?:[eE][\+-]?\d+)?",

        // A cell reference, consisting of one or more letters followed by one or more digits.
        [SyntaxTokenKind.CellReference] = @"[a-zA-Z]+\d+",

        // The literal `+` sign, denoting addition.
        [SyntaxTokenKind.AdditionOperator] = @"\+",

        // The literal `-` sign, denoting subtraction.
        [SyntaxTokenKind.SubtractionOperator] = @"\-",

        // The literal `*` sign, denoting multiplication.
        [SyntaxTokenKind.MultiplicationOperator] = @"\*",

        // The literal `/` sign, denoting division.
        [SyntaxTokenKind.DivisionOperator] = @"/",

        // An opening parenthesis, marking the start of a sub-expression.
        [SyntaxTokenKind.LParenthesis] = @"\(",

        // A closing parenthesis, marking the end of a sub-expression.
        [SyntaxTokenKind.RParenthesis] = @"\)",

        // Some whitespace, which is non-necessary information.
        [SyntaxTokenKind.Trivia] = @"\s+",

        // Any single character not matched by any other token pattern.
        [SyntaxTokenKind.Error] = @".",

        // Eoi (end-of-input) is handled by the TokenPattern builder.
    };

    /// <summary>
    ///     <para>
    ///         "One giant regex," constructed from <see cref="TokenPatterns"/> put into their own separate named
    ///         capture groups, then OR'd together.
    ///     </para>
    /// </summary>
    private static readonly string TokenPattern = string.Join("|",
        TokenPatterns.Select(kv => $"(?<{kv.Key}>{kv.Value})")
    );

    /// <summary>
    ///     <para>
    ///         A compiled version of the <see cref="TokenPattern"/> pattern, meant for matching against input text.
    ///     </para>
    ///     <para>
    ///         TODO: Consider if a compile-time "GeneratedRegex" would be faster.
    ///     </para>
    /// </summary>
    private static readonly Regex TokenRegex = new(TokenPattern);

    /// <summary>
    ///     <para>
    ///         If we, at any point, encounter an unexpected token (i.e., one that produces an error token), this
    ///         will be set to true.
    ///     </para>
    /// </summary>
    public bool EncounteredUnexpectedToken { get; private set; } = false;

    /// <summary>
    ///     <para>
    ///         Returns a stream of tokens, matched by <see cref="TokenRegex"/>. 
    ///     </para>
    ///     <para>
    ///         This is guaranteed to have all text in one, and only one, capture group. Thus, errors are signaled
    ///         in-band, by the use of <see cref="SyntaxToken">Tokens</see> of kind
    ///         <see cref="SyntaxTokenKind.Error">error</see>. Errors are greedy, in that they will not exist in runs.
    ///     </para>
    ///     <para>
    ///         TODO: Move away from regular expressions (see <see cref="Tokenizer"/> for rationale).
    ///     </para>
    /// </summary>
    /// <returns>An iterator over all tokens.</returns>
    public IEnumerable<SyntaxToken> Tokens()
    {
        // All matches from the regex. We'll look through all the matches, figure out what capture groups they belong
        // to, and yield tokens thusly.
        var matches = TokenRegex.Matches(text);

        // We cannot yield error tokens immediately, because consecutive Error token must be merged into a single token.
        // Thus, we buffer the current run of Error tokens (if any).
        SyntaxToken? pendingError = null;

        // Iterate over every regex match produced by the lexer regex.
        // Each match corresponds to exactly one raw token.
        foreach (Match match in matches)
        {
            // Determine which token kind matched by checking which named
            // regex group succeeded. Exactly one group should succeed.
            var kind = Enum.GetValues<SyntaxTokenKind>()
                .First(k => match.Groups[k.ToString()].Success);

            // Extract the raw text and its location in the source.
            var spelling = match.Value;
            var span = new SyntaxSpan(match.Index, match.Length);

            // Error tokens are handled specially because consecutive errors
            // should be merged into a single token.
            if (kind == SyntaxTokenKind.Error)
            {
                // Mark that we've seen an unexpected token.
                EncounteredUnexpectedToken = true;

                // Merge consecutive Error tokens, or start a new one.
                pendingError = pendingError == null
                    ? new SyntaxToken(kind, spelling, span)
                    : new SyntaxToken(kind, pendingError.Value.Spelling + spelling,
                        new SyntaxSpan(pendingError.Value.Span.Index,
                            match.Index + match.Length - pendingError.Value.Span.Index));

                // Wait for next token or flush.
                continue;
            }

            // If we reach a non-Error token and have a buffered Error run,
            // flush it before yielding the current token.
            if (pendingError != null)
            {
                yield return (SyntaxToken)pendingError;
                pendingError = null;
            }

            // Yield non-Error tokens immediately.
            yield return new SyntaxToken(kind, spelling, span);
        }

        // If we have a pending Error token at the end, flush it.
        if (pendingError != null)
        {
            yield return (SyntaxToken)pendingError;
        }

        // Always yield an EOI token at the end.
        yield return new SyntaxToken(SyntaxTokenKind.Eoi, string.Empty, new SyntaxSpan(text.Length, 0));
    }
}