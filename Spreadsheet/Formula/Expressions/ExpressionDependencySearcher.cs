namespace Formula.Expressions;

/// <summary>
///     <para>
///         Scans an expression and collects all cell reference dependencies. This will not return duplicates.
///     </para>
/// </summary>
public class ExpressionDependencySearcher : IExpressionVisitor
{
    /// <summary>
    ///     <para>
    ///         The set of dependencies found in the expression.
    ///     </para>
    /// </summary>
    private readonly HashSet<CellReferenceExpression> _dependencies = [];
    
    /// <summary>
    ///     <para>
    ///         A read-only view of the dependencies found in the expression.
    ///     </para>
    /// </summary>
    public IReadOnlySet<CellReferenceExpression> Dependencies => _dependencies;
    
    /// <summary>
    ///     <para>
    ///         See <see cref="ExpressionDependencySearcher"/>; this constructor starts the search process.
    ///     </para>
    /// </summary>
    /// <param name="expression">The expression to canonicalize.</param>
    public ExpressionDependencySearcher(Expression expression)
    {
        expression.Accept(this);
    }

    /// <inheritdoc />
    public void Visit(BinaryOpExpression binary)
    {
        binary.Left.Accept(this);
        binary.Right.Accept(this);
    }

    /// <inheritdoc />
    public void Visit(CellReferenceExpression cellRef)
    {
        _dependencies.Add(cellRef);
    }
    
    /// <inheritdoc />
    public void Visit(ConstantExpression constant)
    {
    }

    /// <inheritdoc />
    public void Visit(ParentheticalExpression parenthetical)
    {
        parenthetical.Inner.Accept(this);
    }
}