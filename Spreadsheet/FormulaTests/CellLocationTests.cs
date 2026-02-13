namespace FormulaTests.Cell;

using Formula.Cell;

/// <summary>
///     <para>
///         Tests for <see cref="Formula.Cell.CellLocation"/>.
///     </para>
/// </summary>
[TestClass]
public class CellLocationTests
{
    /// <summary>
    ///     <para>
    ///         Tests that the <see cref="CellLocation.FromString"/> method correctly parses a simple cell reference string and returns the expected column and row indices.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void CellLocationFromString_TestValidCellReference_ReturnsCorrectIndices()
    {
        var cellLocation = CellLocation.FromString("C5");
        Assert.AreEqual(2, cellLocation.ColumnIndex);
        Assert.AreEqual(4, cellLocation.RowIndex);
    }
    
    /// <summary>
    ///     <para>
    ///         Tests that the canonicalizer correctly converts column and row indices to their canonical form for simple cases.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void CellLocationToCanonicalString_TestSimpleIndices_ReturnsValidCanonicalForm()
    {
        Assert.AreEqual("A1", new CellLocation(0, 0).ToCanonicalString());
    }

    /// <summary>
    ///     <para>
    ///         Tests that the canonicalizer correctly converts column and row indices to their canonical form.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void CellLocationCanonicalize_TestValidIndices_ReturnsValidCanonicalForm()
    {
        Assert.AreEqual("ZZ93", CellLocation.Canonicalize(701, 92));
    }

    /// <summary>
    ///     <para>
    ///         Tests that the canonicalizer throws an exception when given negative indices.
    ///     </para>
    /// </summary>    [TestMethod]
    [TestMethod]
    public void CellLocationCanonicalize_TestNegativeIndex_ThrowsInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => CellLocation.Canonicalize(-1, 0));
    }
}