namespace Formula.Expressions;

/// <summary>
///     <para>
///         A visitor interface for expression nodes, following the Visitor design pattern.
///     </para>
/// </summary>
public interface IExpressionVisitor
{
    /// <summary>
    ///     <para>
    ///         Visits a binary operation expression node.
    ///     </para>
    /// </summary>
    void Visit(BinaryOpExpression binary);
    
    /// <summary>
    ///     <para>
    ///         Visits a cell reference expression node.
    ///     </para>
    /// </summary>
    void Visit(CellReferenceExpression cellRef);
    
    /// <summary>
    ///     <para>
    ///         Visits a constant expression node.
    ///     </para>
    /// </summary>
    void Visit(ConstantExpression constant);
    
    /// <summary>
    ///     <para>
    ///         Visits a parenthetical expression node.
    ///     </para>
    /// </summary>
    void Visit(ParentheticalExpression parenthetical);
}