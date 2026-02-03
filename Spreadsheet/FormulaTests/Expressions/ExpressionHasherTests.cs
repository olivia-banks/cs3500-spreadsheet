namespace FormulaTests.Expressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Formula.Expressions;
using Formula.Frontend;

/// <summary>
///     <para>
///         Contains unit tests for the <see cref="ExpressionHasher"/> class.
///     </para>
/// </summary>
[TestClass]
public class ExpressionHasherTests
{
    /// <summary>
    ///     <para>
    ///         Tests that the hash for a simple constant expression is deterministic and consistent.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionHasherConstructor_TestConstantHash_IsValid()
    {
        var tokenizer = new Tokenizer("42");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var hasher = new ExpressionHasher(expression);
        var hash1 = hasher.ComputedHash;

        var hasher2 = new ExpressionHasher(expression);
        var hash2 = hasher2.ComputedHash;

        Assert.AreEqual(hash1, hash2);
    }
    
    /// <summary>
    ///     <para>
    ///         Tests that the hash for the same constant expression in different instances produces the same hash.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionHasherConstructor_TestIdenticalConstantExpressionsProduceSameHash_IsValid()
    {
        var tokenizer1 = new Tokenizer("100");
        using var parser1 = new Parser(tokenizer1.Tokens());
        var expression1 = parser1.Parse();
        
        var tokenizer2 = new Tokenizer("100");
        using var parser2 = new Parser(tokenizer2.Tokens());
        var expression2 = parser2.Parse();
        
        var hasher1 = new ExpressionHasher(expression1);
        var hasher2 = new ExpressionHasher(expression2);
        
        Assert.AreEqual(hasher1.ComputedHash, hasher2.ComputedHash);
    }

    /// <summary>
    ///     <para>
    ///         Tests that the hash for a binary expression is affected by the operator.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionHasherVisitBinaryOpExpression_TestOperatorAffectsHash_IsValid()
    {
        var tokenizer1 = new Tokenizer("1+2");
        using var parser1 = new Parser(tokenizer1.Tokens());
        var expression1 = parser1.Parse();

        var tokenizer2 = new Tokenizer("1*2");
        using var parser2 = new Parser(tokenizer2.Tokens());
        var expression2 = parser2.Parse();

        var hasher1 = new ExpressionHasher(expression1);
        var hasher2 = new ExpressionHasher(expression2);

        Assert.AreNotEqual(hasher1.ComputedHash, hasher2.ComputedHash);
    }

    /// <summary>
    ///     <para>
    ///         Tests that the hash for a binary expression changes if one operand changes.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionHasherVisitBinaryOpExpression_TestOperandAffectsHash_IsValid()
    {
        var tokenizer1 = new Tokenizer("3+5");
        using var parser1 = new Parser(tokenizer1.Tokens());
        var expression1 = parser1.Parse();

        var tokenizer2 = new Tokenizer("4+5");
        using var parser2 = new Parser(tokenizer2.Tokens());
        var expression2 = parser2.Parse();

        var hasher1 = new ExpressionHasher(expression1);
        var hasher2 = new ExpressionHasher(expression2);

        Assert.AreNotEqual(hasher1.ComputedHash, hasher2.ComputedHash);
    }

    /// <summary>
    ///     <para>
    ///         Tests that the hash for a cell reference depends on both row and column.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionHasherVisitCellReferenceExpression_TestRowAndColumnAffectsHash_IsValid()
    {
        var tokenizer1 = new Tokenizer("A1");
        using var parser1 = new Parser(tokenizer1.Tokens());
        var expression1 = parser1.Parse();

        var tokenizer2 = new Tokenizer("A2");
        using var parser2 = new Parser(tokenizer2.Tokens());
        var expression2 = parser2.Parse();

        var hasher1 = new ExpressionHasher(expression1);
        var hasher2 = new ExpressionHasher(expression2);

        Assert.AreNotEqual(hasher1.ComputedHash, hasher2.ComputedHash);
    }

    /// <summary>
    ///     <para>
    ///         Tests that the hash for a parenthetical expression differs from its inner expression.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionHasherVisitParentheticalExpression_TestParenthesesAffectsHash_IsValid()
    {
        var tokenizer1 = new Tokenizer("5");
        using var parser1 = new Parser(tokenizer1.Tokens());
        var innerExpression = parser1.Parse();

        var tokenizer2 = new Tokenizer("(5)");
        using var parser2 = new Parser(tokenizer2.Tokens());
        var parentheticalExpression = parser2.Parse();

        var hasherInner = new ExpressionHasher(innerExpression);
        var hasherParenthetical = new ExpressionHasher(parentheticalExpression);

        Assert.AreNotEqual(hasherInner.ComputedHash, hasherParenthetical.ComputedHash);
    }

    /// <summary>
    ///     <para>
    ///         Tests that two identical complex expressions produce the same hash.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionHasherConstructor_TestComplexExpressionHash_IsValid()
    {
        var formula = "(A1+2)*3";
        var tokenizer1 = new Tokenizer(formula);
        using var parser1 = new Parser(tokenizer1.Tokens());
        var expression1 = parser1.Parse();

        var tokenizer2 = new Tokenizer(formula);
        using var parser2 = new Parser(tokenizer2.Tokens());
        var expression2 = parser2.Parse();

        var hasher1 = new ExpressionHasher(expression1);
        var hasher2 = new ExpressionHasher(expression2);

        Assert.AreEqual(hasher1.ComputedHash, hasher2.ComputedHash);
    }
}