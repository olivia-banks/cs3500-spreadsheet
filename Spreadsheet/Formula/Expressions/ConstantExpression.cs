using Formula.Frontend;

namespace Formula.Expressions;

/// <summary>
///     <para>
///         A constant numeric expression (e.g., 42, 3.14). Internally, this a double-precision floating point number.
///     </para>
///     <para>
///         TODO: Sort out other types (e.g., strings, booleans, errors).
///         TODO: Sort out precision/representation issues (e.g., decimal vs double).
///     </para>
/// </summary>
/// <param name="Span">The span of the expression in the source code.</param>
/// <param name="Value">The numeric value of the constant.</param>
public record ConstantExpression(SyntaxSpan Span, double Value) : Expression(Span)
{
    /// <inheritdoc/>
    public override void Accept(IExpressionVisitor visitor) => visitor.Visit(this);
}