namespace FormulaTests.Expressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Formula.Expressions;
using Formula.Frontend;
using System.Linq;

/// <summary>
///     <para>
///         Contains unit tests for the <see cref="ExpressionDependencySearcher"/> class.
///     </para>
/// </summary>
[TestClass]
public class ExpressionDependencySearcherTests
{
    /// <summary>
    ///     <para>
    ///         Tests that a constant expression has no dependencies.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionDependencySearcherConstructor_TestConstantExpressionHasNoDependencies_IsValid()
    {
        var tokenizer = new Tokenizer("42");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var searcher = new ExpressionDependencySearcher(expression);

        Assert.IsEmpty(searcher.Dependencies);
    }

    /// <summary>
    ///     <para>
    ///         Tests that a single cell reference is correctly detected as a dependency.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionDependencySearcherConstructor_TestSingleCellReference_IsValid()
    {
        var tokenizer = new Tokenizer("A1");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var searcher = new ExpressionDependencySearcher(expression);
        Assert.HasCount(1, searcher.Dependencies);

        var cell = searcher.Dependencies.First();
        Assert.AreEqual(0, cell.ColumnIndex);
        Assert.AreEqual(0, cell.RowIndex);
    }

    /// <summary>
    ///     <para>
    ///         Tests that multiple different cell references in a binary expression are all detected.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionDependencySearcherVisitBinaryOpExpression_TestMultipleCellReferences_IsValid()
    {
        var tokenizer = new Tokenizer("A1+B2");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var searcher = new ExpressionDependencySearcher(expression);

        Assert.HasCount(2, searcher.Dependencies);
        Assert.IsTrue(searcher.Dependencies.Any(c => c.ColumnIndex == 0 && c.RowIndex == 0));
        Assert.IsTrue(searcher.Dependencies.Any(c => c.ColumnIndex == 1 && c.RowIndex == 1));
    }

    /// <summary>
    ///     <para>
    ///         Tests that a parenthetical expression correctly reports dependencies of its inner expression.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionDependencySearcherVisitParentheticalExpression_TestParenthesesPreserveDependencies_IsValid()
    {
        var tokenizer = new Tokenizer("(C3)");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var searcher = new ExpressionDependencySearcher(expression);
        Assert.HasCount(1, searcher.Dependencies);

        var cell = searcher.Dependencies.First();
        Assert.AreEqual(2, cell.ColumnIndex);
        Assert.AreEqual(2, cell.RowIndex);
    }

    /// <summary>
    ///     <para>
    ///         Tests that duplicate cell references are only reported once.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionDependencySearcherConstructor_TestDuplicateCellReferencesAreIgnored_IsValid()
    {
        var tokenizer = new Tokenizer("A1+A1");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var searcher = new ExpressionDependencySearcher(expression);
        Assert.HasCount(1, searcher.Dependencies);

        var cell = searcher.Dependencies.First();
        Assert.AreEqual(0, cell.ColumnIndex);
        Assert.AreEqual(0, cell.RowIndex);
    }

    /// <summary>
    ///     <para>
    ///         Tests that a complex expression with constants, multiple cells, and parentheses reports all unique dependencies.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionDependencySearcherConstructor_TestComplexExpressionCollectsAllDependencies_IsValid()
    {
        var tokenizer = new Tokenizer("(A1+2)*(B2+C3)");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var searcher = new ExpressionDependencySearcher(expression);

        Assert.HasCount(3, searcher.Dependencies);
        Assert.IsTrue(searcher.Dependencies.Any(c => c.ColumnIndex == 0 && c.RowIndex == 0)); // A1
        Assert.IsTrue(searcher.Dependencies.Any(c => c.ColumnIndex == 1 && c.RowIndex == 1)); // B2
        Assert.IsTrue(searcher.Dependencies.Any(c => c.ColumnIndex == 2 && c.RowIndex == 2)); // C3
    }

    /// <summary>
    ///     <para>
    ///         Tests that we ignore numeric constants.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionDependencySearcherConstructor_TestIgnoresNumericConstants_IsValid()
    {
        var tokenizer = new Tokenizer("A1+100");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var searcher = new ExpressionDependencySearcher(expression);
        Assert.HasCount(1, searcher.Dependencies);
        var cell = searcher.Dependencies.First();
        Assert.AreEqual(0, cell.ColumnIndex);
        Assert.AreEqual(0, cell.RowIndex);
    }
}