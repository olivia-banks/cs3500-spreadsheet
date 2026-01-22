using Formula.Frontend;

namespace Formula.Expressions;

/// <summary>
///     <para>
///         The base class for all expression types in the formula AST.
///     </para>
/// </summary>
public abstract record Expression(SyntaxSpan Span)
{
    /// <summary>
    ///     <para>
    ///         Accepts a visitor that implements the <see cref="IExpressionVisitor"/> interface.
    ///     </para>
    /// </summary>
    public abstract void Accept(IExpressionVisitor visitor);
}