namespace FormulaTests;

using Formula.Frontend;

/// <summary>
///     <para>Unit tests for the <see cref="SyntaxToken"/> class.</para>
/// </summary>
[TestClass]
public class SyntaxTokenTests
{
    /// <summary>
    ///     <para>
    ///         Test that <see cref="SyntaxToken.ToString()" /> method doesn't explode.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void SyntaxTokenToString_NumericToken_DoesNotThrow()
    {
        var token = new SyntaxToken(SyntaxTokenKind.NumericLiteral, "123", new SyntaxSpan(0, 3));
        var result = token.ToString();
        Assert.IsNotNull(result);
    }
}