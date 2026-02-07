namespace Formula.Expressions;

using System;

/// <summary>
///     <para>
///         Evaluates an expression tree to a numeric value using the visitor pattern. Of note here is
///         that <c>_result</c> is overwritten at each node, which is fine here but means the evaluator
///         cannot hold intermediate results for reuse. We shouldn't require that, though.
///     </para>
/// </summary>
public class ExpressionEvaluator : IExpressionVisitor
{
    /// <summary>
    ///     <para>
    ///         Delegate used to resolve the numeric value of a cell reference.
    ///     </para>
    /// </summary>
    private readonly Func<int, int, double> _cellLookup;

    /// <summary>
    ///     <para>
    ///         Stores the result of the most recently visited expression node.
    ///     </para>
    /// </summary>
    private double _result;

    /// <summary>
    ///     <para>
    ///         The final evaluated value of the expression.
    ///     </para>
    /// </summary>
    public double Result => _result;

    /// <summary>
    ///     <para>
    ///         Initializes a new <see cref="ExpressionEvaluator"/> and begins evaluation
    ///         of the provided expression.
    ///     </para>
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="cellLookup">
    ///     A function that maps a cell's column and row indices to its numeric value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="cellLookup"/> is <c>null</c>.
    /// </exception>
    public ExpressionEvaluator(Expression expression, Func<int, int, double> cellLookup)
    {
        _cellLookup = cellLookup ?? throw new ArgumentNullException(nameof(cellLookup), "Cell lookup function cannot be null.");
        
        expression.Accept(this);
    }

    /// <inheritdoc />
    public void Visit(BinaryOpExpression binary)
    {
        binary.Left.Accept(this);
        var leftValue = _result;

        binary.Right.Accept(this);
        var rightValue = _result;

        _result = binary.Op switch
        {
            BinaryOpKind.Addition => leftValue + rightValue,
            BinaryOpKind.Subtraction => leftValue - rightValue,
            BinaryOpKind.Multiplication => leftValue * rightValue,
            BinaryOpKind.Division => rightValue == 0
                ? throw new DivideByZeroException()
                : leftValue / rightValue,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <inheritdoc />
    public void Visit(CellReferenceExpression cellRef)
    {
        _result = _cellLookup(cellRef.ColumnIndex, cellRef.RowIndex);
    }

    /// <inheritdoc />
    public void Visit(ConstantExpression constant)
    {
        _result = constant.Value;
    }

    /// <inheritdoc />
    public void Visit(ParentheticalExpression parenthetical)
    {
        parenthetical.Inner.Accept(this);
    }
}