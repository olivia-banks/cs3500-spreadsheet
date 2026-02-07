using Formula.Frontend;

namespace Formula.Expressions;

/// <summary>
///     <para>
///         A cell reference expression (e.g., A1, B2, C3).
///     </para>
/// </summary>
/// <param name="Span">The span of the expression in the source code.</param>
/// <param name="ColumnIndex">The 0-based index of the column.</param>
/// <param name="RowIndex">The 0-based row of the column.</param>
public record CellReferenceExpression(SyntaxSpan Span, int ColumnIndex, int RowIndex) : Expression(Span)
{
    /// <inheritdoc/>
    public override void Accept(IExpressionVisitor visitor) => visitor.Visit(this);
    
    /// <summary>
    ///     <para>
    ///         Gets the hash code for a cell reference, ignoring the span.
    ///     </para>
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(ColumnIndex, RowIndex);
    
    /// <inheritdoc/>
    public virtual bool Equals(CellReferenceExpression? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return ColumnIndex == other.ColumnIndex && RowIndex == other.RowIndex;
    }
}