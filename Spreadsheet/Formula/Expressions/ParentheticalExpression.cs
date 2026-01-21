using Formula.Frontend;

namespace Formula.Expressions;

/// <summary>
///     <para>
///         Represents a parenthetical expression, which encapsulates another expression within parentheses.
///     </para>
/// </summary>
/// <param name="Span">The span of the expression in the source code.</param>
/// <param name="Inner">The inner expression contained within the parentheses.</param>
public record ParentheticalExpression(SyntaxSpan Span, Expression Inner) : Expression(Span)
{
}