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
    public void SetContentsOfCell_DoubleContent_CanBeRetrieved()
    {
        var sheet = new Spreadsheet();

        var order = sheet.SetContentsOfCell("A1", "5.0");

        Assert.HasCount(1, order);
        Assert.AreEqual("A1", order[0]);

        var contents = sheet.GetCellContents("A1");
        Assert.AreEqual(5.0, contents);
    }

    /// <summary>
    ///     Tests setting and retrieving a string cell.
    /// </summary>
    [TestMethod]
    public void SetContentsOfCell_StringContent_CanBeRetrieved()
    {
        var sheet = new Spreadsheet();

        sheet.SetContentsOfCell("B2", "hello");

        var contents = sheet.GetCellContents("b2"); // case-insensitive
        Assert.AreEqual("hello", contents);
    }

    /// <summary>
    ///     Tests setting and retrieving a formula cell.
    /// </summary>
    [TestMethod]
    public void SetContentsOfCell_FormulaContent_CanBeRetrieved()
    {
        var sheet = new Spreadsheet();
        var formula = new Formula("A1+2");

        sheet.SetContentsOfCell("C3", "=A1+2");

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
    public void SetContentsOfCell_FormulaDependency_ReturnsCorrectOrder()
    {
        var sheet = new Spreadsheet();

        sheet.SetContentsOfCell("A1", "5.0");
        sheet.SetContentsOfCell("B1", "=A1*2");
        sheet.SetContentsOfCell("C1", "=B1+A1");

        var order = sheet.SetContentsOfCell("A1", "10.0");

        Assert.AreEqual("A1", order[0]);
        Assert.Contains("B1", order);
        Assert.Contains("C1", order);
        Assert.IsLessThan(order.IndexOf("C1"), order.IndexOf("B1"));
    }

    /// <summary>
    ///     Tests circular dependency detection (direct).
    /// </summary>
    [TestMethod]
    public void SetContentsOfCell_DirectCircularDependency_ThrowsCircularException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<CircularException>(() => sheet.SetContentsOfCell("A1", "=A1+1"));
    }

    /// <summary>
    ///     Tests circular dependency detection (indirect).
    /// </summary>
    [TestMethod]
    public void SetContentsOfCell_IndirectCircularDependency_ThrowsCircularException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<CircularException>(() =>
        {
            sheet.SetContentsOfCell("A1", "=B1+1");
            sheet.SetContentsOfCell("B1", "=A1+1");
        });
    }

    /// <summary>
    ///     Tests that names are normalized to uppercase.
    /// </summary>
    [TestMethod]
    public void GetNamesOfAllNonemptyCells_NormalizesNames()
    {
        var sheet = new Spreadsheet();

        sheet.SetContentsOfCell("a1", "3.0");

        var names = sheet.GetNamesOfAllNonemptyCells();
        Assert.HasCount(1, names);
        Assert.Contains("A1", names);
    }

    /// <summary>
    ///     Tests that replacing a cell updates recalculation order.
    /// </summary>
    [TestMethod]
    public void SetContentsOfCell_ReplaceCell_UpdatesDependencies()
    {
        var sheet = new Spreadsheet();

        sheet.SetContentsOfCell("A1", "1.0");
        sheet.SetContentsOfCell("B1", "=A1+1");

        var order = sheet.SetContentsOfCell("A1", "2.0");

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
    public void SetContentsOfCell_DirectDependents_AreReturnedCorrectly()
    {
        var sheet = new Spreadsheet();

        // A1 has no dependencies
        sheet.SetContentsOfCell("A1", "5.0");

        // Direct dependents of A1
        sheet.SetContentsOfCell("B1", "=A1 * 2");
        sheet.SetContentsOfCell("C1", "=A1 + 3");

        // D1 depends on B1 (indirectly depends on A1)
        sheet.SetContentsOfCell("D1", "=B1 + 1");

        var order = sheet.SetContentsOfCell("A1", "10.0");

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
    public void SetContentsOfCell_Double_InvalidName_ThrowsInvalidNameException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<InvalidNameException>(() => sheet.SetContentsOfCell("1A", "5.0"));
    }

    /// <summary>
    ///     Tests that passing an invalid name to the string overload throws InvalidNameException.
    /// </summary>
    [TestMethod]
    public void SetContentsOfCell_String_InvalidName_ThrowsInvalidNameException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<InvalidNameException>(() => sheet.SetContentsOfCell("$$$", "text"));
    }

    /// <summary>
    ///     Tests that passing an invalid name to the formula overload throws InvalidNameException.
    /// </summary>
    [TestMethod]
    public void GetCellValue_FormulaWithError_ReturnsFormulaError()
    {
        var sheet = new Spreadsheet();

        sheet.SetContentsOfCell("A1", "5.0");
        sheet.SetContentsOfCell("B1", "=A1/0"); // Division by zero error

        var value = sheet.GetCellValue("B1");
        Assert.IsInstanceOfType(value, typeof(FormulaError));
    }

    /// <summary>
    ///     Tests that getting the value of a cell with an equation that references a non-numeric cell returns a FormulaError.
    /// </summary>
    [TestMethod]
    public void GetCellValue_FormulaWithNonNumericReference_ReturnsFormulaError()
    {
        var sheet = new Spreadsheet();

        sheet.SetContentsOfCell("A1", "text");
        sheet.SetContentsOfCell("B1", "=A1+5"); // Invalid reference

        Assert.ThrowsExactly<FormulaFormatException>(() => sheet.GetCellValue("B1"));
    }

    /// <summary>
    ///     Tests that passing an invalid name to the formula overload throws InvalidNameException.
    /// </summary>
    [TestMethod]
    public void SpreadsheetThis_InvalidName_ThrowsInvalidNameException()
    {
        var sheet = new Spreadsheet();

        Assert.ThrowsExactly<InvalidNameException>(() => sheet["1A"]);
    }

    /// <summary>
    ///     Test that loading a simple JSON file, with a mix of doubles and formulas, correctly populates the
    ///     spreadsheet.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_SimpleJSONLoading_IsValid()
    {
        var json = """
                   {
                       "Cells": {
                           "A1": { "StringForm": "10" },
                           "A2": { "StringForm": "text" },
                           "A3": { "StringForm": "=A1+A2" }
                       }
                   }
                   """;

        // We need to write this to a temporary file because the Spreadsheet constructor expects a file path, not a
        // JSON string.
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);

        var sheet = new Spreadsheet(tempFilePath);

        Assert.AreEqual(10.0, sheet.GetCellContents("A1"));
        Assert.AreEqual("text", sheet.GetCellContents("A2"));
        Assert.AreEqual(new Formula("A1+A2"), sheet.GetCellContents("A3"));
    }

    /// <summary>
    ///     Test that loading a JSON file with a simple arithmetic dependency chain
    ///     correctly builds cell contents without cycles.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_MultiLevelDependencyJSON_IsValid()
    {
        var json = """
                   {
                       "Cells": {
                           "C1": { "StringForm": "5" },
                           "C2": { "StringForm": "=C1+1" },
                           "C3": { "StringForm": "=C2*2" }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);

        var sheet = new Spreadsheet(tempFilePath);

        Assert.AreEqual(5.0, sheet.GetCellContents("C1"));
        Assert.AreEqual(new Formula("C1+1"), sheet.GetCellContents("C2"));
        Assert.AreEqual(new Formula("C2*2"), sheet.GetCellContents("C3"));
    }

    /// <summary>
    ///     Test that loading a JSON file with a formula that does not reference
    ///     other cells is handled correctly.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_ConstantFormulaJSON_IsValid()
    {
        var json = """
                   {
                       "Cells": {
                           "E1": { "StringForm": "=1+2" },
                           "F1": { "StringForm": "42" }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);

        var sheet = new Spreadsheet(tempFilePath);

        Assert.AreEqual(new Formula("1+2"), sheet.GetCellContents("E1"));
        Assert.AreEqual(42.0, sheet.GetCellContents("F1"));
    }

    /// <summary>
    ///     Test that loading a JSON file with an indirect cyclic reference
    ///     throws an exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_IndirectCycleJSON_ThrowsSpreadsheetReadWriteException()
    {
        var json = """
                   {
                       "Cells": {
                           "A1": { "StringForm": "=B1+1" },
                           "B1": { "StringForm": "=A1+1" }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);

        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(tempFilePath));
    }

    /// <summary>
    ///     Test that loading a JSON file with a direct self-referencing formula
    ///     throws a circular dependency exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_DirectSelfReferenceJSON_ThrowsSpreadsheetReadWriteException()
    {
        var json = """
                   {
                       "Cells": {
                           "A1": { "StringForm": "=A1+1" }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);

        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(tempFilePath));
    }

    /// <summary>
    ///     Test that loading a JSON file with a reference to a non-existent cell
    ///     doesn't an exception during construction (since this is only an error when evaluating the formula, not when
    ///     parsing it).
    /// </summary>
    [TestMethod]
    public void Spreadsheet_InvalidReferenceJSON_ThrowsSpreadsheetReadWriteException()
    {
        var json = """
                   {
                       "Cells": {
                           "C1": { "StringForm": "=Z9+1" }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);

        _ = new Spreadsheet(tempFilePath);
    }

    /// <summary>
    ///     Test that loading a JSON file containing malformed JSON syntax
    ///     results in a read/write exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_MalformedJSON_ThrowsReadException()
    {
        var json = """
                   {
                       "Cells": {
                           "E1": { "StringForm": "5" }
                           "E2": { "StringForm": "=E1+2" }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);

        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(tempFilePath));
        ;
    }

    /// <summary>
    ///     Test that loading a JSON file where StringForm is not a string
    ///     results in a read/write exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_NonStringStringFormJSON_ThrowsReadException()
    {
        var json = """
                   {
                       "Cells": {
                           "G1": { "StringForm": 123 }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);

        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(tempFilePath));
    }

    /// <summary>
    ///     Test that loading a JSON file where 'Cells' is not an object results in a read/write exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_NonObjectCellsJSON_ThrowsReadException()
    {
        var json = """
                   {
                       "Cells": "This should be an object, not a string"
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);

        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(tempFilePath));
    }

    /// <summary>
    ///     Test that loading a JSON file where 'StringForm' doesn't exist for a cell results in a read/write exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_MissingStringFormJSON_ThrowsReadException()
    {
        var json = """
                   {
                       "Cells": {
                           "H1": { "NotStringForm": "5" }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);

        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(tempFilePath));
    }

    /// <summary>
    ///     Test that loading a JSON file where 'StringForm' is not a string results in a read/write exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_StringFormNotStringJSON_ThrowsReadException()
    {
        var json = """
                   {
                       "Cells": {
                           "I1": { "StringForm": 12345 }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);
        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(tempFilePath));
    }

    /// <summary>
    ///     Test that loading a JSON file where 'StringForm' is null results in a read/write exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_StringFormNullJSON_ThrowsReadException()
    {
        var json = """
                   {
                       "Cells": {
                           "J1": { "StringForm": null }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);
        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(tempFilePath));
    }

    /// <summary>
    ///     Test that loading a JSON file where 'Cells' contains a non-object value results in a read/write exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_CellsNotObjectJSON_ThrowsReadException()
    {
        var json = """
                   {
                       "Cells": {
                           "K1": "This should be an object, not a string"
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);
        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(tempFilePath));
    }

    /// <summary>
    ///     Test that loading a JSON file where 'Cells' is missing results in a read/write exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_MissingCellsJSON_ThrowsReadException()
    {
        var json = """
                   {
                       "NotCells": {
                           "L1": { "StringForm": "5" }
                       }
                   }
                   """;

        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, json);
        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(tempFilePath));
    }

    /// <summary>
    ///     Test that loading an invalid file path results in a read/write exception.
    /// </summary>
    [TestMethod]
    public void Spreadsheet_InvalidFilePath_ThrowsReadException()
    {
        var invalidFilePath = Path.Combine(Path.GetTempPath(), "nonexistent.json");

        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => new Spreadsheet(invalidFilePath));
    }

    /// <summary>
    ///     Tests that passing an invalid name to the formula overload throws InvalidNameException.
    /// </summary>
    [TestMethod]
    public void SpreadsheetReadWriteException_SetMessage_PersistsMessage()
    {
        var ex = new SpreadsheetReadWriteException("File not found");
        Assert.AreEqual("File not found", ex.Message);
    }

    /// <summary>
    ///     Test that saving a spreadsheet to a file and then loading it back results in a spreadsheet with the same cell contents.
    /// </summary>
    [TestMethod]
    public void SpreadsheetSave_SimpleSaveAndLoad_IsConsistent()
    {
        var sheet = new Spreadsheet();
        sheet.SetContentsOfCell("A1", "5.0");
        sheet.SetContentsOfCell("B1", "=A1*2");
        sheet.SetContentsOfCell("C1", "=B1+A1");
        sheet.SetContentsOfCell("D1", "text");

        var tempFilePath = Path.GetTempFileName();
        sheet.Save(tempFilePath);

        Console.WriteLine($"Saved spreadsheet to {tempFilePath}");

        var loadedSheet = new Spreadsheet(tempFilePath);

        Assert.AreEqual(5.0, loadedSheet.GetCellContents("A1"));
        Assert.AreEqual(new Formula("A1*2"), loadedSheet.GetCellContents("B1"));
        Assert.AreEqual(new Formula("B1+A1"), loadedSheet.GetCellContents("C1"));
        Assert.AreEqual("text", loadedSheet.GetCellContents("D1"));
    }

    /// <summary>
    ///     Test that trying to save a spreadsheet to an invalid file path results in a SpreadsheetReadWriteException.
    /// </summary>
    [TestMethod]
    public void SpreadsheetSave_TrySaveToInvalidLocation_ThrowsSpreadsheetReadWriteException()
    {
        var sheet = new Spreadsheet();
        sheet.SetContentsOfCell("A1", "5.0");

        var invalidFilePath = Path.Combine(Path.GetTempPath(), "nonexistent_directory", "spreadsheet.json");

        Assert.ThrowsExactly<SpreadsheetReadWriteException>(() => sheet.Save(invalidFilePath));
    }
}