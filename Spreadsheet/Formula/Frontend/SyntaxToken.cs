namespace Formula.Frontend;

/// <summary>
///     <para>
///         A token, from a formula. Represents a logical chunk of a formula, and tracks it's literal spelling, kind,
///         and originating location. This is not dissimilar from Roslyn's SyntaxToken class of the same name. See
///         "lexical analysis."
///     </para>
///     <para>
///         As tokens are logical groupings of characters, we can expect that the formula `4 + 8` to tokenize into:
///
///         <code language="plaintext">
///             &lt;NumericLiteral:'1'@0+1&gt;
///             &lt;Trivia:' '@1+1&gt;
///             &lt;AdditionOperator:'+'@2+1&gt;
///             &lt;Trivia:' '@3+1&gt;
///             &lt;NumericLiteral:'2'@4+1&gt;
///         </code>
///     </para>
/// </summary>
/// <param name="kind">The variety of this token, based on its semantic meaning.</param>
/// <param name="spelling">The literal spelling of this token, as seen in the source area.</param>
/// <param name="span">The location that the literal spelling may be found at.</param>
public readonly struct SyntaxToken(SyntaxTokenKind kind, string spelling, SyntaxSpan span)
{
    /// <summary>
    ///     <para>
    ///         The variety of this token, based on its semantic meaning.
    ///     </para>
    /// </summary>
    public SyntaxTokenKind Kind { get; } = kind;

    /// <summary>
    ///     <para>
    ///         The literal spelling of this token, as seen in the source area.
    ///     </para>
    /// </summary>
    public string Spelling { get; } = spelling;

    /// <summary>
    ///     <para>
    ///         The location that the literal spelling may be found at.
    ///     </para>
    /// </summary>
    public SyntaxSpan Span { get; } = span;

    /// <inheritdoc/>
    public override string ToString() => $"<{Kind}:`{Spelling}'@{Span}>";
}

/// <summary>
///     A kind of <see cref="SyntaxToken"/>, representing the variety of a token, or semantic grouping of
///     characters. See the <see cref="Tokenizer"/> for precisely what counts for which token kind.
/// </summary>
public enum SyntaxTokenKind
{
    /// <summary>
    ///     <para>
    ///         Some sort of number, in some sort of notation.
    ///     </para>
    /// </summary>
    NumericLiteral,

    /// <summary>
    ///     <para>
    ///         A reference to a cell, such as A1 or B2.
    ///     </para>
    /// </summary>
    CellReference,

    /// <summary>
    ///     <para>
    ///         An operator representing addition.
    ///     </para>
    /// </summary>
    AdditionOperator,

    /// <summary>
    ///     <para>
    ///         An operator representing subtraction.
    ///     </para>
    /// </summary>
    SubtractionOperator,

    /// <summary>
    ///     <para>
    ///         An operator representing multiplication.
    ///     </para>
    /// </summary>
    MultiplicationOperator,

    /// <summary>
    ///     <para>
    ///         An operator representing division.
    ///     </para>
    /// </summary>
    DivisionOperator,

    /// <summary>
    ///     <para>
    ///         An operator representing exponentiation.
    ///     </para>
    /// </summary>
    LParenthesis,

    /// <summary>
    ///     <para>
    ///         An operator representing exponentiation.
    ///     </para>
    /// </summary>
    RParenthesis,

    /// <summary>
    ///     <para>
    ///         Some whitespace or other non-semantic character(s).
    ///     </para>
    /// </summary>
    Trivia,

    /// <summary>
    ///     <para>
    ///         Some unrecognized or invalid character(s).
    ///     </para>
    /// </summary>
    Error,

    /// <summary>
    ///     <para>
    ///         The end of the input stream.
    ///     </para>
    /// </summary>
    Eoi,
}