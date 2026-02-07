namespace FormulaTests.Expressions;

using Formula.Expressions;
using Formula.Frontend;

/// <summary>
///     <para>
///         Tests for the <see cref="CellReferenceExpression"/> class.
///     </para>
/// </summary>
[TestClass]
public class CellReferenceExpressionTest
{
    /// <summary>
    ///     <para>
    ///         Check for equality of two cell reference expressions with the same column and row indices.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void CellReferenceExpressionEquals_TwoIdenticalFormulas_EqualityIsShown()
    {
        // Arrange
        var expr1 = new CellReferenceExpression(new SyntaxSpan(0, 2), 0, 0);
        var expr2 = new CellReferenceExpression(new SyntaxSpan(5, 2), 0, 0);
        
        // Act
        var areEqual = expr1.Equals(expr2);
        
        // Assert
        Assert.IsTrue(areEqual);
    }
    
    /// <summary>
    ///     <para>
    ///         Check for inequality of two same cell reference expressions.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void CellReferenceExpressionEquals_TwoDifferentFormulas_EqualityIsTrue()
    {
        // Arrange
        var expr = new CellReferenceExpression(new SyntaxSpan(0, 2), 0, 0);

        // Act
        var areEqual = expr.Equals(expr);
        
        // Assert
        Assert.IsTrue(areEqual);
    }
        
    /// <summary>
    ///     <para>
    ///         Check for inequality of two cell reference expressions where one is null.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void CellReferenceExpressionEquals_OneNull_EqualityIsFalse()
    {
        // Arrange
        var expr1 = new CellReferenceExpression(new SyntaxSpan(0, 2), 0, 0);

        // Act
        var areEqual = expr1.Equals(null);

        // Assert
        Assert.IsFalse(areEqual);
    }
}