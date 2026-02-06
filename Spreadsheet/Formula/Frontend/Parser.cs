using System.Diagnostics;
using Formula.Expressions;

namespace Formula.Frontend;

/// <summary>
///     <para>
///         Parses formulas written in standard infix notation using standard precedence rules. See the
///         <see cref="Tokenizer"/> and <see cref="SyntaxTokenKind"/> for a list of supported tokens, their patterns,
///         and their delimiters.
///     </para>
///     <para>
///         This parser implements standard recursive descent, with precedence climbing when handling precedence-based
///         expressions (i.e., binary arithmetic operators).
///     </para>
///     <para>
///         While exceptions will eventually be reported in-band, and this parser made a bit more robust, for now
///         it will throw <see cref="FormulaFormatException"/> when it encounters syntax errors, as per the assignment
///         description.
///     </para>
/// </summary>
public class Parser : IDisposable
{
    /// <summary>
    ///     <para>
    ///         Maps binary operator token kinds to their precedence levels. Higher numbers indicate higher precedence.
    ///         This is used in the precedence climbing algorithm to determine when to consume operators.
    ///     </para>
    ///     <para>
    ///         The values here reflect standard arithmetic precedence; do you remember PEMDAS/BODMAS from school?
    ///     </para>
    /// </summary>
    private static readonly Dictionary<SyntaxTokenKind, int> BinaryOperatorPrecedence = new()
    {
        [SyntaxTokenKind.AdditionOperator] = 1,
        [SyntaxTokenKind.SubtractionOperator] = 1,
        [SyntaxTokenKind.MultiplicationOperator] = 2,
        [SyntaxTokenKind.DivisionOperator] = 2
    };

    /// <summary>
    ///     <para>
    ///         The token stream actively being parsed.
    ///     </para>
    /// </summary>
    private readonly IEnumerator<SyntaxToken> _tokens;

    /// <summary>
    ///     <para>
    ///         The current token being examined by the parser.
    ///     </para>
    /// </summary>
    private SyntaxToken _current;

    /// <summary>
    ///     <para>
    ///         Initializes a new instance of the <see cref="Parser"/> class.
    ///     </para>
    /// </summary>
    /// <param name="tokens">The token stream to parse.</param>
    public Parser(IEnumerable<SyntaxToken> tokens)
    {
        _tokens = tokens.GetEnumerator();
        Advance();
    }

    /// <summary>
    ///     <para>
    ///         A convenience constructor that initializes a new instance of the <see cref="Parser"/> class
    ///         from a <see cref="Tokenizer"/> directly.
    ///     </para>
    /// </summary>
    /// <param name="tokenizer"></param>
    public Parser(Tokenizer tokenizer) : this(tokenizer.Tokens())
    {
    }

    /// <summary>
    ///     <para>
    ///         Disposes of the parser and its resources.
    ///     </para>
    /// </summary>
    public void Dispose()
    {
        _tokens.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     <para>
    ///         Advances the token stream to the next non-trivia, non-error token.
    ///     </para>
    ///     <para>
    ///         TODO: We want to collect errors/trivia for reporting later.
    ///     </para>
    /// </summary>
    private void Advance()
    {
        do
        {
            // We don't need to check and see if we're at the end, because we will always end with Eoi, which will
            // break us out of the loop.
            _tokens.MoveNext();
            _current = _tokens.Current;

            // If we hit an error token, that's a syntax error.
            if (_current.Kind is SyntaxTokenKind.Error)
            {
                throw new FormulaFormatException($"{_current.Span}: unexpected token `{_current.Spelling}' found in expression.");
            }
        } while (_current.Kind is SyntaxTokenKind.Trivia);
    }

    /// <summary>
    ///     <para>
    ///         Parses the token stream according to standard infix notation rules with standard precedence; this parses
    ///         a complete expression (formula).
    ///     </para>
    /// </summary>
    /// <exception cref="Exception">An internal bug has occured due to a parser-state mismatch.</exception>
    public Expression Parse()
    {
        // A formula is just an expression that consumes the entire token stream.
        var expression = ParseExpression(0);

        // Ensure we've consumed everything. If not, that's a syntax error.
        if (_current.Kind != SyntaxTokenKind.Eoi)
        {
            throw new FormulaFormatException(
                $"{_current.Span}: unexpected token `{_current.Spelling}' (of type {_current.Kind}) found at end of expression... what's up?");
        }

        return expression;
    }

    /// <summary>
    ///     <para>
    ///         Parses an expression with the given minimum precedence. This implements a precedence climbing
    ///         algorithm to handle binary arithmetic operators. See the comments inside the method for more details.
    ///     </para>
    /// </summary>
    /// <param name="minPrecedence">
    ///     An internal parameter used for presidence climbing; functions outside this one should use 0 as the default.
    /// </param>
    private Expression ParseExpression(int minPrecedence)
    {
        var lhs = ParsePrimary();

        // A basic precedence climbing loop to handle binary arithmetic operators. This will repeatedly
        // consume operators and their right-hand side expressions as long as the operator precedence
        // is >= minPrecedence. Here's a simple example:
        //
        // For the expression "3 + 5 * 2" with minPrecedence = 0
        //  1. ParsePrimary consumes "3"
        //  2. See '+' with precedence 1 >= 0, consume it
        //  3. ParseExpression(2) for right-hand side:
        //  a. ParsePrimary consumes "5"
        //    b. See '*' with precedence 2 >= 2, consume it
        //    c. ParseExpression(3) for right-hand side:
        //      i. ParsePrimary consumes "2"
        //    d. No more operators with precedence >= 3, return
        // 4. No more operators with precedence >= 0, return

        while (BinaryOperatorPrecedence.TryGetValue(_current.Kind, out var tokenPrec) && tokenPrec >= minPrecedence)
        {
            var op = _current;

            // Consume operator.
            Advance();

            // Parse right-hand side expression (of higher precedence).
            var rhs = ParseExpression(tokenPrec + 1);
            var span = new SyntaxSpan(lhs.Span, rhs.Span);

            lhs = op.Kind switch
            {
                SyntaxTokenKind.AdditionOperator => new BinaryOpExpression(span, BinaryOpKind.Addition, lhs, rhs),
                SyntaxTokenKind.SubtractionOperator => new BinaryOpExpression(span, BinaryOpKind.Subtraction, lhs, rhs),
                SyntaxTokenKind.MultiplicationOperator => new BinaryOpExpression(span, BinaryOpKind.Multiplication, lhs,
                    rhs),
                SyntaxTokenKind.DivisionOperator => new BinaryOpExpression(span, BinaryOpKind.Division, lhs, rhs),
                _ => throw new FormulaFormatException(
                    $"{op.Span}: unexpected binary operator `{op.Spelling}' of type {op.Kind}, expected either `+', `-', `*', or `/'.")
            };
        }

        return lhs;
    }

    /// <summary>
    ///     <para>
    ///         Parses a primary expression: either a numeric literal, a cell reference, or a parenthesized expression.
    ///     </para>
    /// </summary>
    /// <exception cref="Exception">An internal bug has occured due to a parser-state mismatch.</exception>
    private Expression ParsePrimary()
    {
        switch (_current.Kind)
        {
            case SyntaxTokenKind.NumericLiteral:
                // Failure here should not happen, since the tokenizer should only produce numeric literals
                // that successfully parse as numbers (according to the course supplied regex). If it does,
                // that's a bug in the tokenizer.
                Debug.Assert(double.TryParse(_current.Spelling, out var constantValue));
                var constant = new ConstantExpression(_current.Span, constantValue);
                Advance();

                return constant;

            case SyntaxTokenKind.CellReference:
                var cellReference = ParseCellReference(_current.Span, _current.Spelling);
                Advance();

                return cellReference;

            case SyntaxTokenKind.LParenthesis:
                Advance();

                var inner = ParseExpression(0);
                var outer = new ParentheticalExpression(_current.Span, inner);

                if (_current.Kind != SyntaxTokenKind.RParenthesis)
                {
                    throw new FormulaFormatException(
                        $"{_current.Span}: expected `)' to match `(', but got `{_current.Spelling}' (of type {_current.Kind}) instead.");
                }

                Advance();
                return outer;

            default:
                throw new FormulaFormatException(
                    $"{_current.Span}: unexpected token `{_current.Spelling}' (of type {_current.Kind}), expected a number, variable, or '('.");
        }
    }

    /// <summary>
    ///     <para>
    ///         Parses a cell reference string (e.g., "A1", "B2", "AA10") into a <see cref="CellReferenceExpression"/>.
    ///     </para>
    /// </summary>
    /// <param name="cellRepr">The string representation of the cell in question.</param>
    /// <returns>A parsed version of the cell reference.</returns>
    private static CellReferenceExpression ParseCellReference(SyntaxSpan span, string cellRepr)
    {
        // How do we parse cell references? Well, they start with one or more letters (A-Z, case-insensitive)
        // indicating the column, followed by one or more digits (0-9) indicating the row.
        //
        // For example, "A1" is column 0, row 0; "B2" is column 1, row 1; "AA10" is column 26, row 9.
        // We need to convert the letters to a zero-based column index and the digits to a zero-based row index.
        // 
        // For the column, we treat the letters as a base-26 number, where A=1, B=2, ..., Z=26, and we accumulate
        // the value accordingly. We normalize to capital letters, subtract from 'A' to get the zero-based index
        // of any letters, and then multiply the accumulated value by 26 for each new letter we encounter.
        //
        // While doing this, we keep track of our position in the string so that when we reach the digits, we can
        // say "okay, everything from here on to the end is the row number." This is then just parsed as an integer
        // without any special sauce.

        // Get the column part.
        var columnStated = 0;
        var columnCursor = 0;

        try
        {
            for (; char.IsLetter(cellRepr[columnCursor]); columnCursor++)
            {
                columnStated = checked(columnStated * 26 + (char.ToUpperInvariant(cellRepr[columnCursor]) - 'A' + 1));
            }

            // Subtract 1 to get zero-based index.
            var columnIndex = checked(columnStated - 1);

            // Get row part (from where we left off to the end).
            if (!int.TryParse(cellRepr[columnCursor..], out var rowIndex))
            {
                throw new FormulaFormatException(
                    $"{span}: invalid cell reference `{cellRepr}'; expected digits after column letters.");
            }

            // Subtract 1 to get zero-based index.
            rowIndex = checked(rowIndex - 1);
            
            return new CellReferenceExpression(span, columnIndex, rowIndex);
        } catch (IndexOutOfRangeException)
        {
            throw new FormulaFormatException(
                $"{span}: invalid cell reference `{cellRepr}'; expected letters followed by digits.");
        }
        catch (OverflowException)
        {
            throw new FormulaFormatException(
                $"{span}: cell reference `{cellRepr}' is too large to be represented.");
        }
    }
}