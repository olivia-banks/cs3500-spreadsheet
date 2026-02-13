namespace FormulaTests;

/// <summary>
///     <para>
///         This class tests the formula parser.
///     </para>
/// </summary>
[TestClass]
public class ParserTests
{
    /// <summary>
    ///     <para>
    ///         Ensure we can construct a new instance of the parser from a <see cref="Tokenizer" />
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ParserConstructor_BasicFormula_IsValid()
    {
        var tokenizer = new Formula.Frontend.Tokenizer("1 + 2");
        var parser = new Formula.Frontend.Parser(tokenizer);
        Assert.IsNotNull(parser);
    }

    /// <summary>
    ///     <para>
    ///         Ensure we can parse a mixed case, multi-length cell reference.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ParserParseCellReference_MixedCaseMultiLengthCellReference_IsValid()
    {
        var tokenizer = new Formula.Frontend.Tokenizer("AaBb123");
        var parser = new Formula.Frontend.Parser(tokenizer);
        var ast = parser.Parse();
        Assert.IsNotNull(ast);
    }

    /// <summary>
    ///     <para>
    ///         Ensure we throw on huge, huge cell columns.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ParserParseCellReferences_HugeCellReference_AreInvalid()
    {
        var hugeCellReference = "1 + A" + new string('1', 1000);
        var tokenizer = new Formula.Frontend.Tokenizer(hugeCellReference);
        var parser = new Formula.Frontend.Parser(tokenizer);
        Assert.ThrowsExactly<Formula.FormulaFormatException>(() => parser.Parse());
    }

    /// <summary>
    ///     <para>
    ///         Ensure we throw on huge, huge cell rows.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void ParserParseCellReferences_HugeCellRow_AreInvalid()
    {
        var hugeCellRow = new string('A', 1000) + "1";
        var tokenizer = new Formula.Frontend.Tokenizer(hugeCellRow);
        var parser = new Formula.Frontend.Parser(tokenizer);
        Assert.ThrowsExactly<Formula.FormulaFormatException>(() => parser.Parse());
    }
}