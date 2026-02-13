namespace Formula.Expressions;

// ReSharper disable once InvalidXmlDocComment
/// <summary>
///     <para>
///         Computes a hash code for an expression tree using the visitor pattern.
///         Uses <see cref="HashCode.Add(double)"/> to produce a deterministic, robust hash.
///     </para>
/// </summary>
public class ExpressionHasher : IExpressionVisitor
{
    /// <summary>
    ///     <para>
    ///         The current hash value as nodes are visited.
    ///     </para>
    /// </summary>
    private HashCode _hash = new();

    /// <summary>
    ///     <para>
    ///         The computed hash code of the expression after visiting all nodes.
    ///     </para>
    /// </summary>
    public int ComputedHash => _hash.ToHashCode();

    /// <summary>
    ///     <para>
    ///         Initializes a new <see cref="ExpressionHasher"/> and begins computing the hash
    ///         for the provided expression.
    ///     </para>
    /// </summary>
    /// <param name="expression">The expression whose hash code is to be computed.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="expression"/> is <c>null</c>.
    /// </exception>
    public ExpressionHasher(Expression expression)
        => expression.Accept(this);

    /// <inheritdoc />
    public void Visit(BinaryOpExpression binary)
    {
        binary.Left.Accept(this);
        binary.Right.Accept(this);
        _hash.Add((int)binary.Op);
    }

    /// <inheritdoc />
    public void Visit(CellReferenceExpression cellRef)
    {
        _hash.Add(cellRef.location.ColumnIndex);
        _hash.Add(cellRef.location.RowIndex);
    }

    /// <inheritdoc />
    public void Visit(ConstantExpression constant)
    {
        _hash.Add(constant.Value.GetHashCode());
    }

    /// <inheritdoc />
    public void Visit(ParentheticalExpression parenthetical)
    {
        parenthetical.Inner.Accept(this);
        
        // Mix in a fixed value to differentiate parentheses from their inner expressions
        _hash.Add(0x9E3779B9);
    }
}
