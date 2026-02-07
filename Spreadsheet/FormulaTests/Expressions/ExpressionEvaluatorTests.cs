namespace FormulaTests.Expressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Formula.Expressions;
using Formula.Frontend;
using System;

/// <summary>
///     <para>
///         Contains unit tests for the <see cref="ExpressionEvaluator"/> class.
///     </para>
/// </summary>
[TestClass]
public class ExpressionEvaluatorTests
{
    /// <summary>
    ///     <para>
    ///         Tests that evaluating a simple constant expression returns the correct value.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorConstructor_TestConstantEvaluation_IsValid()
    {
        var tokenizer = new Tokenizer("42");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var evaluator = new ExpressionEvaluator(expression, (_, _) => 0);

        Assert.AreEqual(42, evaluator.Result);
    }

    /// <summary>
    ///     <para>
    ///         Tests that evaluating two identical constant expressions returns the same value.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorConstructor_TestIdenticalConstantExpressionsEvaluateSame_IsValid()
    {
        var tokenizer1 = new Tokenizer("100");
        using var parser1 = new Parser(tokenizer1.Tokens());
        var expression1 = parser1.Parse();

        var tokenizer2 = new Tokenizer("100");
        using var parser2 = new Parser(tokenizer2.Tokens());
        var expression2 = parser2.Parse();

        var evaluator1 = new ExpressionEvaluator(expression1, (_, _) => 0);
        var evaluator2 = new ExpressionEvaluator(expression2, (_, _) => 0);

        Assert.AreEqual(evaluator1.Result, evaluator2.Result);
    }

    /// <summary>
    ///     <para>
    ///         Tests that a binary addition expression evaluates correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorVisitBinaryOpExpression_TestAdditionEvaluation_IsValid()
    {
        var tokenizer = new Tokenizer("1+2");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var evaluator = new ExpressionEvaluator(expression, (_, _) => 0);

        Assert.AreEqual(3, evaluator.Result);
    }

    /// <summary>
    ///     <para>
    ///         Tests that a binary subtraction expression evaluates correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorVisitBinaryOpExpression_TestSubtractionEvaluation_IsValid()
    {
        var tokenizer = new Tokenizer("5-3");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var evaluator = new ExpressionEvaluator(expression, (_, _) => 0);

        Assert.AreEqual(2, evaluator.Result);
    }

    /// <summary>
    ///     <para>
    ///         Tests that a binary multiplication expression evaluates correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorVisitBinaryOpExpression_TestMultiplicationEvaluation_IsValid()
    {
        var tokenizer = new Tokenizer("4*3");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var evaluator = new ExpressionEvaluator(expression, (_, _) => 0);

        Assert.AreEqual(12, evaluator.Result);
    }

    /// <summary>
    ///     <para>
    ///         Tests that a binary division expression evaluates correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorVisitBinaryOpExpression_TestDivisionEvaluation_IsValid()
    {
        var tokenizer = new Tokenizer("10/2");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var evaluator = new ExpressionEvaluator(expression, (_, _) => 0);

        Assert.AreEqual(5, evaluator.Result);
    }

    /// <summary>
    ///     <para>
    ///         Tests that division by zero throws a <see cref="DivideByZeroException"/>.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorVisitBinaryOpExpression_TestDivisionByZero_ThrowsDivideByZeroException()
    {
        var tokenizer = new Tokenizer("10/0");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        Assert.ThrowsExactly<DivideByZeroException>(() => new ExpressionEvaluator(expression, (_, _) => 0));
    }

    /// <summary>
    ///     <para>
    ///         Tests that evaluating a cell reference calls the provided cell lookup function correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorVisitCellReferenceExpression_TestCellLookupIsUsed_IsValid()
    {
        var tokenizer = new Tokenizer("A1");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        double Lookup(int col, int row) => col == 0 && row == 0 ? 123 : 0;

        var evaluator = new ExpressionEvaluator(expression, Lookup);

        Assert.AreEqual(123, evaluator.Result);
    }

    /// <summary>
    ///     <para>
    ///         Tests that a parenthetical expression evaluates to the same value as its inner expression.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorVisitParentheticalExpression_TestParenthesesEvaluation_IsValid()
    {
        var tokenizer = new Tokenizer("(7)");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var evaluator = new ExpressionEvaluator(expression, (_, _) => 0);

        Assert.AreEqual(7, evaluator.Result);
    }

    /// <summary>
    ///     <para>
    ///         Tests that a complex expression evaluates correctly with a mix of operations and parentheses.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorConstructor_TestComplexExpressionEvaluation_IsValid()
    {
        var formula = "(A1+2)*3";
        var tokenizer = new Tokenizer(formula);
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        double Lookup(int col, int row) => col == 0 && row == 0 ? 5 : 0;

        var evaluator = new ExpressionEvaluator(expression, Lookup);

        // (5+2)*3 = 21
        Assert.AreEqual(21, evaluator.Result);
    }

    /// <summary>
    ///     <para>
    ///         Tests that providing a null cell lookup function throws an <see cref="ArgumentNullException"/>.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorConstructor_TestNullCellLookup_ThrowsArgumentNullException()
    {
        var tokenizer = new Tokenizer("A1");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        Assert.ThrowsExactly<ArgumentNullException>(() => new ExpressionEvaluator(expression, null!));
    }

    /// <summary>
    ///     <para>
    ///         Tests that dividing a cell reference by zero throws a <see cref="DivideByZeroException"/>.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorConstructor_TestCellReferenceDivisionByZero_ThrowsDivideByZeroException()
    {
        var formula = "A1/0";
        var tokenizer = new Tokenizer(formula);
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        Assert.ThrowsExactly<DivideByZeroException>(() => new ExpressionEvaluator(expression, (_, _) => 0));
    }

    /// <summary>
    ///     <para>
    ///         Tests that a <see cref="ArgumentOutOfRangeException" /> will be thrown on a bad binary operator.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionEvaluatorVisitBinaryOpExpression_TestInvalidOperator_ThrowsArgumentOutOfRangeException()
    {
        var binaryExpression = new BinaryOpExpression(
            new SyntaxSpan(0, 3),
            (BinaryOpKind.Addition + 100),
            new ConstantExpression(new SyntaxSpan(0, 1), 1),
            new ConstantExpression(new SyntaxSpan(2, 1), 2)
        );
        
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ExpressionEvaluator(binaryExpression, (_, _) => 0));
    }
}