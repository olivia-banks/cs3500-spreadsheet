namespace Formula.Frontend;

/// <summary>
///     <para>
///         Represents an offset and a length into some text. This is meant to be used for metadata reporting inside the
///         Spreadsheet Formula tokenizer and parser. E.g., we can know that the second '+' character came from index 9
///         and has a length of 1.
///     </para>
///     <para>
///         This may be of particular use in error reporting. Since the Tokenizer reports errors in-band through the use
///         of error tokens, we may track down a given region of weird text to a specific range in the formula source.
///     </para>
/// </summary>
/// <param name="index">The index into the given source material that this span may be found at.</param>
/// <param name="length">The length of the given span.</param>
public readonly struct SyntaxSpan(int index, int length)
{
    /// <summary>
    ///     <para>
    ///         The index into the source text.
    ///     </para>
    /// </summary>
    public int Index { get; } = index;
    
    /// <summary>
    ///     <para>
    ///         The length of the span.
    ///     </para>
    /// </summary>
    public int Length { get; } = length;

    /// <inheritdoc/>
    public override string ToString() => $"{Index}+{Length}";
   
    /// <summary>
    ///     <para>
    ///         Merges two SyntaxSpan instances into one that covers both spans.
    ///     </para>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    public SyntaxSpan(SyntaxSpan left, SyntaxSpan right)
        : this(left.Index, left.Length + right.Length)
    {
    }
}