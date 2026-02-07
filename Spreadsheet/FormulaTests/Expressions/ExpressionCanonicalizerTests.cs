namespace FormulaTests.Expressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Formula.Expressions;
using Formula.Frontend;

/// <summary>
///     <para>
///         Contains unit tests for the <see cref="ExpressionCanonicalizer"/> class.
///     </para>
/// </summary>
[TestClass]
public class ExpressionCanonicalizerTests
{
    /// <summary>
    ///     <para>
    ///         Tests that a simple constant expression is canonicalized correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionCanonicalizerConstructor_TestConstantCanonicalization_IsValid()
    {
        var tokenizer = new Tokenizer("42.0000");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var canonicalizer = new ExpressionCanonicalizer(expression);
        Assert.AreEqual("42", canonicalizer.CanonicalForm);
    }

    /// <summary>
    ///     <para>
    ///         Tests that a variable expression is canonicalized to uppercase.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionCanonicalizerConstructor_TestVariableCanonicalization_IsValid()
    {
        var tokenizer = new Tokenizer("x1");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var canonicalizer = new ExpressionCanonicalizer(expression);
        Assert.AreEqual("X1", canonicalizer.CanonicalForm);
    }
    
    /// <summary>
    ///     <para>
    ///         Tests that a variable expression made of multiple letters is canonicalized to uppercase.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionCanonicalizerConstructor_TestMultiLetterVariableCanonicalization_IsValid()
    {
        var tokenizer = new Tokenizer("vAr99");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();
        
        var canonicalizer = new ExpressionCanonicalizer(expression);
        Assert.AreEqual("VAR99", canonicalizer.CanonicalForm);
    }

    /// <summary>
    ///     <para>
    ///         Tests that addition and subtraction operators are preserved in canonical form.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionCanonicalizerVisitBinaryOpExpression_TestAdditionSubtraction_IsValid()
    {
        var tokenizer = new Tokenizer("x1+5-3");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var canonicalizer = new ExpressionCanonicalizer(expression);

        Assert.AreEqual("X1+5-3", canonicalizer.CanonicalForm);
    }

    /// <summary>
    ///     <para>
    ///         Tests that multiplication and division operators are preserved in canonical form.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionCanonicalizerVisitBinaryOpExpression_TestMultiplicationDivision_IsValid()
    {
        var tokenizer = new Tokenizer("2*X2/4");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var canonicalizer = new ExpressionCanonicalizer(expression);
        Assert.AreEqual("2*X2/4", canonicalizer.CanonicalForm);
    }

    /// <summary>
    ///     <para>
    ///         Tests that parentheses are preserved in the canonical form.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionCanonicalizerVisitParentheticalExpression_TestParentheses_IsValid()
    {
        var tokenizer = new Tokenizer("(x1+5)");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var canonicalizer = new ExpressionCanonicalizer(expression);
        Assert.AreEqual("(X1+5)", canonicalizer.CanonicalForm);
    }

    /// <summary>
    ///     <para>
    ///         Tests that a complex formula is canonicalized correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionCanonicalizerConstructor_TestComplexExpression_IsValid()
    {
        var tokenizer = new Tokenizer("(x1+5.0)*Y2/3");
        using var parser = new Parser(tokenizer.Tokens());
        var expression = parser.Parse();

        var canonicalizer = new ExpressionCanonicalizer(expression);
        Assert.AreEqual("(X1+5)*Y2/3", canonicalizer.CanonicalForm);
    }

    /// <summary>
    ///     <para>
    ///         Tests that multiple equivalent formulas produce the same canonical form.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ExpressionCanonicalizerConstructor_TestEquivalentFormulasProduceSameCanonicalForm_IsValid()
    {
        const string formula1 = "x1 + 5";
        const string formula2 = "X1+5.0";

        var tokenizer1 = new Tokenizer(formula1);
        using var parser1 = new Parser(tokenizer1.Tokens());
        var expression1 = parser1.Parse();

        var tokenizer2 = new Tokenizer(formula2);
        using var parser2 = new Parser(tokenizer2.Tokens());
        var expression2 = parser2.Parse();

        var canonical1 = new ExpressionCanonicalizer(expression1);
        var canonical2 = new ExpressionCanonicalizer(expression2);

        Assert.AreEqual(canonical1.CanonicalForm, canonical2.CanonicalForm);
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
        
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ExpressionCanonicalizer(binaryExpression));
    }
}