using System.Globalization;
using System.Text;
using Formula.Cell;

namespace Formula.Expressions;

/// <summary>
///     <para>
///        Converts an expression into its canonical string form, as defined in PS2.
///     </para>
/// </summary>
public class ExpressionCanonicalizer : IExpressionVisitor
{
    /// <summary>
    ///     <para>
    ///         The builder used to construct the canonical form string.
    ///     </para>
    /// </summary>
    private readonly StringBuilder _canonicalFormBuilder = new();
    
    /// <summary>
    ///     <para>
    ///         The canonical form of the expression, as constructed by visiting its nodes.
    ///     </para>
    /// </summary>
    public string CanonicalForm => _canonicalFormBuilder.ToString();

    /// <summary>
    ///     <para>
    ///         See <see cref="ExpressionCanonicalizer"/>; this constructor starts the canonicalization process.
    ///     </para>
    /// </summary>
    /// <param name="expression">The expression to canonicalize.</param>
    public ExpressionCanonicalizer(Expression expression)
    {
        expression.Accept(this);
    }

    /// <inheritdoc />
    public void Visit(BinaryOpExpression binary)
    {
        binary.Left.Accept(this);

        char operatorChar;
        switch (binary.Op)
        {
            case BinaryOpKind.Addition:
                operatorChar = '+';
                break;

            case BinaryOpKind.Subtraction:
                operatorChar = '-';
                break;

            case BinaryOpKind.Multiplication:
                operatorChar = '*';
                break;

            case BinaryOpKind.Division:
                operatorChar = '/';
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        _canonicalFormBuilder.Append(operatorChar);
        binary.Right.Accept(this);
    }

    /// <inheritdoc />
    public void Visit(CellReferenceExpression cellRef)
    {
        _canonicalFormBuilder.Append(cellRef.location.ToCanonicalString());
    }

    /// <inheritdoc />
    public void Visit(ConstantExpression constant)
    {
        _canonicalFormBuilder.Append(constant.Value.ToString(CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public void Visit(ParentheticalExpression parenthetical)
    {
        _canonicalFormBuilder.Append('(');
        parenthetical.Inner.Accept(this);
        _canonicalFormBuilder.Append(')');
    }
}