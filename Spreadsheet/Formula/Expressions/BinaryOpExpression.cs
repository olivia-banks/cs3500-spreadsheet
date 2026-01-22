using Formula.Frontend;

namespace Formula.Expressions;

/// <summary>
///     <para>
///         The kinds of binary operations supported in expressions.
///     </para>
/// </summary>
public enum BinaryOpKind
{
    /// <summary>
    ///     <para>
    ///         An addition operation (e.g., 1 + 2).
    ///     </para>
    /// </summary>
    Addition,
    
    /// <summary>
    ///     <para>
    ///         A subtraction operation (e.g., 2 - 1).
    ///     </para>
    /// </summary>
    Subtraction,
    
    /// <summary>
    ///     <para>
    ///         A multiplication operation (e.g., 2 * 3).
    ///     </para>
    /// </summary>
    Multiplication,
    
    /// <summary>
    ///     <para>
    ///         A division operation (e.g., 6 / 2).
    ///     </para>
    /// </summary>
    Division
}

/// <summary>
///    <para>
///        Represents a binary operation expression consisting of two operands and an operator.
///    </para>
/// </summary>
/// <param name="Span">The span of the expression in the source code.</param>
/// <param name="Op">The operator in question.</param>
/// <param name="Left">The left hand side (lhs) of the operator.</param>
/// <param name="Right">The right hand side (rhs) of the operator. </param>
public record BinaryOpExpression(SyntaxSpan Span, BinaryOpKind Op, Expression Left, Expression Right) : Expression(Span)
{
    /// <inheritdoc/>
    public override void Accept(IExpressionVisitor visitor) => visitor.Visit(this);
}