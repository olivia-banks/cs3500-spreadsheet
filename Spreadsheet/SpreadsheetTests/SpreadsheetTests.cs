namespace SpreadsheetTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spreadsheet;
using Formula;

/// <summary>
///     Contains unit tests for the <see cref="Spreadsheet"/> class.
/// </summary>
[TestClass]
public class SpreadsheetTests
{
    /// <summary>
    ///     Tests that a new spreadsheet has no non-empty cells.
    /// </summary>
    [TestMethod]
    public void GetNamesOfAllNonemptyCells_NewSpreadsheet_IsEmpty()
    {
        var sheet = new Spreadsheet();

        Assert.IsEmpty(sheet.GetNamesOfAllNonemptyCells());
        Assert.IsEmpty(sheet.GetLocationsOfAllNonemptyCells());
    }

    /// <summary>
    ///     Tests setting and retrieving a double cell.
    /// </summary>
    [TestMethod]
    public void SetCellContents_DoubleContent_CanBeRetrieved()
    {
        var sheet = new Spreadsheet();

        var order = sheet.SetCellContents("A1", 5.0);

        Assert.HasCount(1, order);
        Assert.AreEqual("A1", order[0]);

        var contents = sheet.GetCellContents("A1");
        Assert.AreEqual(5.0, contents);
    }

    /// <summary>
    ///     Tests setting and retrieving a string cell.
    /// </summary>
    [TestMethod]
    public void SetCellContents_StringContent_CanBeRetrieved()
    {
        var sheet = new Spreadsheet();

        sheet.SetCellContents("B2", "hello");

        var contents = sheet.GetCellContents("b2"); // case-insensitive
        Assert.AreEqual("hello", contents);
    }

    /// <summary>
    ///     Tests setting and retrieving a formula cell.
    /// </summary>
    [TestMethod]
    public void SetCellContents_FormulaContent_CanBeRetrieved()
    {
        var sheet = new Spreadsheet();
        var formula = new Formula("A1+2");

        sheet.SetCellContents("C3", formula);

        var contents = sheet.GetCellContents("C3");
        Assert.AreEqual(formula, contents);
    }

    /// <summary>
    ///     Tests that GetCellContents throws on invalid name.
    /// </summary>
    [TestMethod]
    public void GetCellContents_InvalidName_ThrowsInvalidNameException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<InvalidNameException>(() => sheet.GetCellContents("1A"));
    }

    /// <summary>
    ///     Tests that requesting contents of a valid but empty cell throws.
    /// </summary>
    [TestMethod]
    public void GetCellContents_EmptyCell_ThrowsInvalidNameException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<InvalidNameException>(() => sheet.GetCellContents("Z9"));
    }

    /// <summary>
    ///     Tests dependency ordering for simple chain.
    /// </summary>
    [TestMethod]
    public void SetCellContents_FormulaDependency_ReturnsCorrectOrder()
    {
        var sheet = new Spreadsheet();

        sheet.SetCellContents("A1", 5.0);
        sheet.SetCellContents("B1", new Formula("A1*2"));
        sheet.SetCellContents("C1", new Formula("B1+A1"));

        var order = sheet.SetCellContents("A1", 10.0);

        Assert.IsTrue(order[0] == "A1");
        Assert.IsTrue(order.Contains("B1"));
        Assert.IsTrue(order.Contains("C1"));
        Assert.IsTrue(order.IndexOf("B1") < order.IndexOf("C1"));
    }

    /// <summary>
    ///     Tests circular dependency detection (direct).
    /// </summary>
    [TestMethod]
    public void SetCellContents_DirectCircularDependency_ThrowsCircularException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<CircularException>(() => sheet.SetCellContents("A1", new Formula("A1+1")));
    }

    /// <summary>
    ///     Tests circular dependency detection (indirect).
    /// </summary>
    [TestMethod]
    public void SetCellContents_IndirectCircularDependency_ThrowsCircularException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<CircularException>(() =>
        {
            sheet.SetCellContents("A1", new Formula("B1+1"));
            sheet.SetCellContents("B1", new Formula("A1+1"));
        });
    }

    /// <summary>
    ///     Tests that names are normalized to uppercase.
    /// </summary>
    [TestMethod]
    public void GetNamesOfAllNonemptyCells_NormalizesNames()
    {
        var sheet = new Spreadsheet();

        sheet.SetCellContents("a1", 3.0);

        var names = sheet.GetNamesOfAllNonemptyCells();
        Assert.HasCount(1, names);
        Assert.Contains("A1", names);
    }

    /// <summary>
    ///     Tests that replacing a cell updates recalculation order.
    /// </summary>
    [TestMethod]
    public void SetCellContents_ReplaceCell_UpdatesDependencies()
    {
        var sheet = new Spreadsheet();

        sheet.SetCellContents("A1", 1.0);
        sheet.SetCellContents("B1", new Formula("A1+1"));

        var order = sheet.SetCellContents("A1", 2.0);

        Assert.HasCount(2, order);
        Assert.AreEqual("A1", order[0]);
        Assert.AreEqual("B1", order[1]);
    }
    
    /// <summary>
    ///     If B1 and C1 directly depend on A1, and D1 depends on B1,
    ///     then changing A1 should recalculate A1, B1, C1, and D1.
    ///     B1 and C1 must come after A1.
    ///     D1 must come after B1.
    /// </summary>
    [TestMethod]
    public void SetCellContents_DirectDependents_AreReturnedCorrectly()
    {
        var sheet = new Spreadsheet();

        // A1 has no dependencies
        sheet.SetCellContents("A1", 5.0);

        // Direct dependents of A1
        sheet.SetCellContents("B1", new Formula("A1 * 2"));
        sheet.SetCellContents("C1", new Formula("A1 + 3"));

        // D1 depends on B1 (indirectly depends on A1)
        sheet.SetCellContents("D1", new Formula("B1 + 1"));

        var order = sheet.SetCellContents("A1", 10.0);

        // Must include all affected cells
        Assert.HasCount(4, order);
        Assert.AreEqual("A1", order[0]);

        Assert.Contains("B1", order);
        Assert.Contains("C1", order);
        Assert.Contains("D1", order);

        // Direct dependents must come after A1
        Assert.IsLessThan(order.IndexOf("B1"), order.IndexOf("A1"));
        Assert.IsLessThan(order.IndexOf("C1"), order.IndexOf("A1"));

        // D1 must come after B1 (dependency ordering)
        Assert.IsLessThan(order.IndexOf("D1"), order.IndexOf("B1"));
    }
    
    /// <summary>
    ///     Tests that passing an invalid name to the double overload throws InvalidNameException.
    /// </summary>
    [TestMethod]
    public void SetCellContents_Double_InvalidName_ThrowsInvalidNameException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<InvalidNameException>(() => sheet.SetCellContents("1A", 5.0));
    }

    /// <summary>
    ///     Tests that passing an invalid name to the string overload throws InvalidNameException.
    /// </summary>
    [TestMethod]
    public void SetCellContents_String_InvalidName_ThrowsInvalidNameException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<InvalidNameException>(() => sheet.SetCellContents("$$$", "text"));
    }
}