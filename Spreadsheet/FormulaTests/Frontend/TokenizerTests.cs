namespace FormulaTests;

using Formula.Frontend;

/// <summary>
///     <para>
///         This class tests the formula tokenizer.
///     </para>
/// </summary>
[TestClass]
public class TokenizerTests
{
    /// <summary>
    ///     <para>
    ///         This test tries to tokenize a basic formula string.
    ///     </para>
    ///     <para>
    ///         A lot of code relies on this tokenizer, but we need to call it from it's own testing class to increase
    ///         coverage.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void TokenizerTokens_BasicFormulaString_IsValidAndTokenizesOkay()
    {
        var tokenizer = new Tokenizer("(1 + 2) * A1");
        var tokens = tokenizer.Tokens().Where(t => t.Kind != SyntaxTokenKind.Trivia).ToList();
        Assert.IsNotNull(tokens);
        Assert.HasCount(7 + 1, tokens);

        Assert.AreEqual(SyntaxTokenKind.LParenthesis, tokens[0].Kind);
        Assert.AreEqual(SyntaxTokenKind.NumericLiteral, tokens[1].Kind);
        Assert.AreEqual(SyntaxTokenKind.AdditionOperator, tokens[2].Kind);
        Assert.AreEqual(SyntaxTokenKind.NumericLiteral, tokens[3].Kind);
        Assert.AreEqual(SyntaxTokenKind.RParenthesis, tokens[4].Kind);
        Assert.AreEqual(SyntaxTokenKind.MultiplicationOperator, tokens[5].Kind);
        Assert.AreEqual(SyntaxTokenKind.CellReference, tokens[6].Kind);
        Assert.AreEqual(SyntaxTokenKind.Eoi, tokens[7].Kind);
    }
}