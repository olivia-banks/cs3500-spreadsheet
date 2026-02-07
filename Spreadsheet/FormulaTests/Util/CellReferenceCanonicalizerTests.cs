namespace FormulaTests.Util;

using Formula.Util;

/// <summary>
///     <para>
///         Tests for <see cref="Formula.Util.CellReferenceCanonicalizer"/>.
///     </para>
/// </summary>
[TestClass]
public class CellReferenceCanonicalizerTests
{
    /// <summary>
    ///     <para>
    ///         Tests that the canonicalizer correctly converts column and row indices to their canonical form.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void CellReferenceCanonicalizer_TestValidIndices_ReturnsValidCanonicalForm()
    {
        Assert.AreEqual("ZZ93", CellReferenceCanonicalizer.Canonicalize(701, 92));
    }

    /// <summary>
    ///     <para>
    ///         Tests that the canonicalizer throws an exception when given negative indices.
    ///     </para>
    /// </summary>    [TestMethod]
    [TestMethod]
    public void CellReferenceCanonicalizer_TestNegativeIndex_ThrowsInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => CellReferenceCanonicalizer.Canonicalize(-1, 0));
    }
}