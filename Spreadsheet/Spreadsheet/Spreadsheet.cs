using System.Globalization;
using System.Text.Encodings.Web;

namespace Spreadsheet;

using Formula.Cell;
using Formula;
using DependencyGraph;
using System.Text.Json;

/// <summary>
///     <para>
///       A Spreadsheet object represents the state of a simple spreadsheet.  A
///       spreadsheet represents an infinite number of named cells.
///     </para>
///     <para>
///         Valid Cell Names: A string is a valid cell name if and only if it is one or
///         more letters followed by one or more numbers, e.g., A5, BC27.
///     </para>
///     <para>
///         Cell names are case-insensitive, so "x1" and "X1" are the same cell name.
///         Your code should normalize (uppercased) any stored name but accept either.
///     </para>
///     <para>
///         A spreadsheet represents a cell corresponding to every possible cell name.  (This
///         means that a spreadsheet contains an infinite number of cells.)  In addition to
///         a name, each cell has a contents and a value.  The distinction is important.
///     </para>
///     <para>
///         The <b>contents</b> of a cell can be (1) a string, (2) a double, or (3) a Formula.
///         If the contents of a cell is set to the empty string, the cell is considered empty.
///     </para>
///     <para>
///         By analogy, the contents of a cell in Excel is what is displayed on
///         the editing line when the cell is selected.
///     </para>
///     <para>
///         In a new spreadsheet, the contents of every cell is the empty string. Note:
///         this is by definition (it is IMPLIED, not stored).
///     </para>
///     <para>
///         The <b>value</b> of a cell can be (1) a string, (2) a double, or (3) a FormulaError.
///         (By analogy, the value of an Excel cell is what is displayed in that cell's position
///         in the grid.) We are not concerned with cell values yet, only with their contents,
///         but for context:
///     </para>
///     <list type="number">
///         <item>If a cell's contents is a string, its value is that string.</item>
///         <item>If a cell's contents is a double, its value is that double.</item>
///         <item>
///             <para>
///                 If a cell's contents is a Formula, its value is either a double or a FormulaError,
///                 as reported by the Evaluate method of the Formula class.  For this assignment,
///                 you are not dealing with values yet.
///             </para>
///         </item>
///     </list>
///     <para>
///         Spreadsheets are never allowed to contain a combination of Formulas that establish
///         a circular dependency.  A circular dependency exists when a cell depends on itself,
///         either directly or indirectly.
///     </para>
///     <para>
///         For example, suppose that A1 contains B1*2, B1 contains C1*2, and C1 contains A1*2.
///         A1 depends on B1, which depends on C1, which depends on A1.  That's a circular
///         dependency.
///     </para>
/// </summary>
public class Spreadsheet
{
    /// <summary>
    ///     <para>
    ///         The main data structure for storing the contents of the spreadsheet. This maps from cell
    ///         locations to their corresponding cell values. Only non-empty cells are stored in this dictionary,
    ///         so if a cell is not present in the dictionary, it is considered to have empty contents.
    ///     </para>
    /// </summary>
    private readonly Dictionary<CellLocation, Cell> _cells = [];

    /// <summary>
    ///     <para>
    ///         The dependency graph for the spreadsheet. This tracks the dependencies between cells, so that
    ///         when a cell is updated, we can efficiently determine which other cells need to be recalculated.
    ///     </para>
    /// </summary>
    private readonly DependencyGraph _dependencyGraph = new();


    /// <summary>
    ///     <para>
    ///         Create an empty spreadsheet.  By definition, every cell in the spreadsheet has the empty string as its
    ///         contents.
    ///     </para>
    /// </summary>
    public Spreadsheet()
    {
    }

    /// <summary>
    ///     <para>
    ///         Create a new spreadsheet from a JSON file (by path). The JSON file should be in the format produced by
    ///         the Save method. If there are any problems loading the file, including if the file is not in the correct
    ///         format, throw a SpreadsheetReadWriteException with an informative message.
    ///     </para>
    /// </summary>
    /// <param name="filename">The path to the JSON file to load the spreadsheet from.</param>
    /// <exception cref="SpreadsheetReadWriteException">Thrown if there are any problems loading the file, including if the file is not in the correct format.</exception>
    public Spreadsheet(string filename)
    {
        try
        {
            // We can use System.Text.Json to read the JSON file and parse it into a JsonDocument. Then we can extract
            // the cell data from the JSON and populate our spreadsheet using the existing SetContentsOfCell method,
            // which will handle parsing the string form and updating the dependency graph.
            using var stream = File.OpenRead(filename);
            using var doc = JsonDocument.Parse(stream);

            var root = doc.RootElement;

            if (!root.TryGetProperty("Cells", out JsonElement cellsElement))
            {
                throw new SpreadsheetReadWriteException("Malformed JSON: Missing 'Cells' property.");
            }

            if (cellsElement.ValueKind != JsonValueKind.Object)
            {
                throw new SpreadsheetReadWriteException("Malformed JSON: 'Cells' must be an object.");
            }

            // Now we can iterate over the properties of the "Cells" object, where each property name is a cell name,
            // and the value is an object containing the "StringForm" of the cell contents.
            foreach (JsonProperty cellProperty in cellsElement.EnumerateObject())
            {
                var cellName = cellProperty.Name;
                var cellValue = cellProperty.Value;

                if (cellValue.ValueKind != JsonValueKind.Object)
                {
                    throw new SpreadsheetReadWriteException($"Malformed cell '{cellName}': Expected object.");
                }

                if (!cellValue.TryGetProperty("StringForm", out JsonElement stringFormElement))
                {
                    throw new SpreadsheetReadWriteException(
                        $"Malformed cell '{cellName}': Missing 'StringForm' property.");
                }

                if (stringFormElement.ValueKind != JsonValueKind.String)
                {
                    throw new SpreadsheetReadWriteException(
                        $"Malformed cell '{cellName}': 'StringForm' must be a string.");
                }

                var stringForm = stringFormElement.GetString();
                if (stringForm == null)
                {
                    throw new SpreadsheetReadWriteException(
                        $"Malformed cell '{cellName}': 'StringForm' cannot be null.");
                }

                // Now we can set the contents of the cell using the existing SetContentsOfCell method, which will
                // handle parsing the string form and updating the dependency graph.
                SetContentsOfCell(cellName, stringForm);
            }
        }
        catch (Exception ex)
        {
            throw new SpreadsheetReadWriteException($"Error loading spreadsheet: {ex.Message}");
        }
        
        // Finally, we can mark the spreadsheet as not changed, since we just loaded it.
        Changed = false;
    }

    /// <summary>
    ///     <para>
    ///         This method converts a cell name string into a CellLocation object. It is used internally to parse cell
    ///         names in the public methods that take string names, and it also serves as a single point of truth for
    ///         how we parse and validate cell names. If the name is invalid, it throws an InvalidNameException.
    ///     </para>
    /// </summary>
    /// <param name="name">The cell name string to parse.</param>
    /// <returns>The CellLocation corresponding to the given name.</returns>
    private static CellLocation LocationOfReference(string name)
    {
        try
        {
            return CellLocation.FromString(name);
        }
        catch (ArgumentException)
        {
            throw new InvalidNameException();
        }
    }

    /// <summary>
    ///     <para>
    ///         Provides a copy of the locations of all of the cells in the spreadsheet that contain
    ///         information (i.e., non-empty cells).
    ///     </para>
    /// </summary>
    /// <returns>A set of the locations of all the non-empty cells in the spreadsheet.</returns>
    public ISet<CellLocation> GetLocationsOfAllNonemptyCells()
        => _cells.Keys.ToHashSet();

    /// <summary>
    ///     <para>
    ///         Provides a copy of the normalized names of all of the cells in the spreadsheet
    ///         that contain information (i.e., non-empty cells).
    ///     </para>
    /// </summary>
    /// <returns>A set of the names of all the non-empty cells in the spreadsheet.</returns>
    public ISet<string> GetNamesOfAllNonemptyCells()
        => _cells.Keys.Select(c => CellLocation.Canonicalize(c.ColumnIndex, c.RowIndex)).ToHashSet();

    /// <summary>
    ///     <para>
    ///         Returns the contents (as opposed to the value) of the named cell. This is a string wrapper
    ///         around <see cref="GetCellContents(CellLocation)"/>.
    ///     </para>
    /// </summary>
    /// <exception cref="InvalidNameException">Thrown if the name is invalid.</exception>
    /// <param name="name">The name of the spreadsheet cell to query. </param>
    /// <returns>
    ///     <para>
    ///         The contents as either a string, a double, or a Formula.
    ///         See the class header summary.
    ///     </para>
    /// </returns>
    public object GetCellContents(string name)
        => GetCellContents(LocationOfReference(name));

    /// <summary>
    ///     <para>
    ///         Returns the contents (as opposed to the value) of the named cell.
    ///     </para>
    ///     <para>
    ///         TODO: This is a poor design. Ideally, everything should be represented as a formula, and if the formula
    ///           has no variables, then we can do a constant evaluation. This should require introducing a string type
    ///           into the formula language, which is a non-trivial amount of work. For now, we have this awkward
    ///           triality of string/double/formula, which is a bit of a mess, but it is what it is. It is also what
    ///           fits the spec, and "we're not supposed to do any more work than we need to," so here we are.
    ///
    ///           I hate this class, but I don't hate my grade, so here we are.
    ///     </para>
    /// </summary>
    /// <param name="location">The location of the spreadsheet cell to query. </param>
    /// <returns>
    ///     <para>
    ///         The contents as either a string, a double, or a Formula.
    ///         See the class header summary.
    ///     </para>
    /// </returns>
    public object GetCellContents(CellLocation location)
    {
        try
        {
            return _cells[location].AsObject();
        }
        catch (KeyNotFoundException)
        {
            throw new InvalidNameException();
        }
    }

    /// <summary>
    ///     <para>
    ///         Set the contents of the cell at the given location to the given cell. This is a wrapper around
    ///         <see cref="SetCellContents(string, double)"/>, <see cref="SetCellContents(string, string)"/>, and
    ///         <see cref="SetCellContents(string, Formula)"/>.
    ///     </para>
    /// </summary>
    /// <param name="location">The location of the cell to replace.</param>
    /// <param name="cell">The cell to replace the current cell with.</param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException"></exception>
    private IList<CellLocation> SetCellContents(CellLocation location, Cell cell)
    {
        var name = location.ToCanonicalString();

        // Remove any existing dependees (we are replacing the cell)
        _dependencyGraph.ReplaceDependees(name, []);

        // If the new contents is a formula, register its dependencies
        if (cell.AsObject() is Formula formula)
        {
            var variables = formula.GetVariables();
            _dependencyGraph.ReplaceDependees(name, variables);
        }

        _cells[location] = cell;
        return GetCellsToRecalculate(location).ToList();
    }

    /// <summary>
    ///     <para>
    ///         Set the contents of the named cell to the given number.
    ///     </para>
    /// </summary>
    /// <exception cref="InvalidNameException">Thrown if the name is invalid>.</exception>
    /// <param name="name"> The name of the cell. </param>
    /// <param name="number"> The new contents of the cell. </param>
    /// <returns>
    ///     <para>
    ///         This method returns an ordered list consisting of the passed in name
    ///         followed by the names of all other cells whose value depends, directly
    ///         or indirectly, on the named cell.
    ///     </para>
    ///     <para>
    ///         The order must correspond to a valid dependency ordering for recomputing
    ///         all of the cells, i.e., if you re-evaluate each cells in the order of the list,
    ///         the overall spreadsheet will be correctly updated.
    ///     </para>
    ///     <para>
    ///         For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
    ///         list [A1, B1, C1] is returned, i.e., A1 was changed, so then A1 must be
    ///         evaluated, followed by B1, followed by C1.
    ///     </para>
    /// </returns>
    private IList<string> SetCellContents(string name, double number)
        => SetCellContents(LocationOfReference(name), new Cell(number))
            .Select(c => CellLocation.Canonicalize(c.ColumnIndex, c.RowIndex)).ToList();

    /// <summary>
    ///     <para>
    ///         The contents of the named cell becomes the given text.
    ///     </para>
    /// </summary>
    /// <exception cref="InvalidNameException">Thrown if the name is invalid.</exception>
    /// <param name="name">The name of the cell.</param>
    /// <param name="text">The new contents of the cell.</param>
    /// <returns>The same list as defined in <see cref="SetCellContents(string, double)"/>.</returns>
    private IList<string> SetCellContents(string name, string text)
        => SetCellContents(LocationOfReference(name), new Cell(text))
            .Select(c => CellLocation.Canonicalize(c.ColumnIndex, c.RowIndex)).ToList();

    /// <summary>
    ///     <para>
    ///         Set the contents of the named cell to the given formula.
    ///     </para>
    /// </summary>
    /// <exception cref="InvalidNameException">If the name is invalid, throw an InvalidNameException</exception>
    /// <exception cref="CircularException">
    ///     <para>
    ///         If changing the contents of the named cell to be the formula would
    ///         cause a circular dependency, throw a CircularException, and no
    ///         change is made to the spreadsheet.
    ///     </para>
    /// </exception>
    /// <param name="name"> The name of the cell. </param>
    /// <param name="formula"> The new contents of the cell. </param>
    /// <returns>The same list as defined in <see cref="SetCellContents(string, double)"/>.</returns>
    private IList<string> SetCellContents(string name, Formula formula)
        => SetCellContents(LocationOfReference(name), new Cell(formula))
            .Select(c => CellLocation.Canonicalize(c.ColumnIndex, c.RowIndex)).ToList();

    /// <summary>
    ///   <para>
    ///     This method is implemented for you, but makes use of your GetDirectDependents.
    ///   </para>
    ///   <para>
    ///     Returns an enumeration of the names of all cells whose values must
    ///     be recalculated, assuming that the contents of the cell referred
    ///     to by name has changed.  The cell names are enumerated in an order
    ///     in which the calculations should be done.
    ///   </para>
    ///   <exception cref="CircularException">
    ///     If the cell referred to by name is involved in a circular dependency,
    ///     throws a CircularException.
    ///   </exception>
    ///   <exception cref="InvalidNameException">
    ///     If name is not a valid cell name, throws an InvalidNameException.
    ///   </exception>
    ///   <para>
    ///     For example, suppose that:
    ///   </para>
    ///   <list type="number">
    ///     <item>
    ///       A1 contains 5
    ///     </item>
    ///     <item>
    ///       B1 contains the formula A1 + 2.
    ///     </item>
    ///     <item>
    ///       C1 contains the formula A1 + B1.
    ///     </item>
    ///     <item>
    ///       D1 contains the formula A1 * 7.
    ///     </item>
    ///     <item>
    ///       E1 contains 15
    ///     </item>
    ///   </list>
    ///   <para>
    ///     If A1 has changed, then A1, B1, C1, and D1 must be recalculated,
    ///     and they must be recalculated in an order which has A1 first, and B1 before C1
    ///     (there are multiple such valid orders).
    ///     The method will produce one of those enumerations.
    ///   </para>
    ///   <para>
    ///      PLEASE NOTE THAT THIS METHOD DEPENDS ON THE METHOD GetDirectDependents.
    ///      IT WON'T WORK UNTIL GetDirectDependents IS IMPLEMENTED CORRECTLY.
    ///   </para>
    /// </summary>
    /// <param name="location"> The location of the cell.</param>
    /// <returns>
    ///    Returns an enumeration of the names of all cells whose values must
    ///    be recalculated.
    /// </returns>
    private IEnumerable<CellLocation> GetCellsToRecalculate(CellLocation location)
    {
        LinkedList<CellLocation> changed = new();
        HashSet<CellLocation> visited = [];
        Visit(location, location, visited, changed);

        return changed;
    }

    /// <summary>
    ///     Returns an enumeration, without duplicates, of the names of all cells whose
    ///     values depend directly on the value of the cell at the given location.
    /// </summary>
    /// <param name="location">This is the location of the cell to query.</param>
    /// <returns>
    ///     <para>
    ///         Returns an enumeration, without duplicates, of the names of all cells
    ///         that contain formulas containing the cell at the given location.
    ///     </para>
    ///     <para>For example, suppose that: </para>
    ///     <list type="bullet">
    ///         <item>A1 contains 3</item>
    ///         <item>B1 contains the formula A1 * A1</item>
    ///         <item>C1 contains the formula B1 + A1</item>
    ///         <item>D1 contains the formula B1 - C1</item>
    ///     </list>
    ///     <para>The direct dependents of A1 are B1 and C1.</para>
    /// </returns>
    private IEnumerable<CellLocation> GetDirectDependents(CellLocation location)
        => _dependencyGraph.GetDependents(location.ToCanonicalString()).Select(CellLocation.FromString);

    /// <summary>
    ///     <para>
    ///         We want to visit the dependents of the given name in a depth first manner, and add them to the changed
    ///         list in post-order, so that we get a valid topological ordering of the cells to recalculate. We also
    ///         want to keep track of visited nodes, so that we don't get stuck in an infinite loop if there is a
    ///         circular dependency.
    ///     </para>
    ///     <para>
    ///         If we encounter the start node again, then we know that there is a circular dependency, and we throw a
    ///         CircularException.
    ///     </para>
    /// </summary>
    /// <param name="start">The cell location to start visiting at</param>
    /// <param name="name">The cell location to visit.</param>
    /// <param name="visited">The set of visited nodes</param>
    /// <param name="changed">The list of changed nodes, in post-order</param>
    /// <exception cref="CircularException">Thrown when a circular dependency in cell visitation is detected.</exception>
    private void Visit(CellLocation start, CellLocation name, ISet<CellLocation> visited,
        LinkedList<CellLocation> changed)
    {
        visited.Add(name);
        foreach (var n in GetDirectDependents(name))
        {
            if (n.Equals(start))
            {
                throw new CircularException();
            }


            if (!visited.Contains(n))
            {
                Visit(start, n, visited, changed);
            }
        }

        changed.AddFirst(name);
    }

    /// <summary>
    ///   <para>
    ///     Return the value of the named cell, as defined by
    ///     <see cref="GetCellValue(string)"/>.
    ///   </para>
    /// </summary>
    /// <param name="name"> The cell in question. </param>
    /// <returns>
    ///   <see cref="GetCellValue(string)"/>
    /// </returns>
    /// <exception cref="InvalidNameException">
    ///   If the provided name is invalid, throws an InvalidNameException.
    /// </exception>
    public object this[string name] => GetCellValue(name);


    /// <summary>
    /// True if this spreadsheet has been changed since it was
    /// created or saved (whichever happened most recently),
    /// False otherwise.
    /// </summary>
    public bool Changed { get; private set; }


    /// <summary>
    /// Saves this spreadsheet to a file
    /// </summary>
    /// <example>
    ///     <para>
    ///         For example, consider a spreadsheet that contains a cell "A1" with contents being the double 5.0, and a
    ///         cell "B3" with contents being the Formula("A1+2"), and a cell "C4" with the contents "hello". The output
    ///         JSON should look like the following:
    ///     </para>
    ///     <code>
    ///         {
    ///             "Cells": {
    ///                 "A1": {
    ///                     "StringForm": "5"
    ///                 },
    ///                 "B3": {
    ///                     "StringForm": "=A1+2"
    ///                 },
    ///                 "C4": {
    ///                     "StringForm": "hello"
    ///                 }
    ///             }
    ///         }
    ///     </code>
    /// </example>
    /// <param name="filename"> The name (with path) of the file to save to.</param>
    /// <exception cref="SpreadsheetReadWriteException">
    ///   If there are any problems opening, writing, or closing the file,
    ///   the method should throw a SpreadsheetReadWriteException with an
    ///   explanatory message.
    /// </exception>
    public void Save(string filename)
    {
        // We just need to serialize the non-empty cells, since by definition any cell not in the dictionary is empty.
        // We can serialize the cells as a dictionary from cell name to cell contents, where the cell contents is
        // represented as a string (either the number as a string, the text, or the formula with an '=' prepended).
        var data = _cells.ToDictionary(
            kvp => kvp.Key.ToCanonicalString(),
            kvp =>
            {
                var cell = kvp.Value;
                return cell.Kind switch
                {
                    CellKind.Text => cell.AsText(),
                    CellKind.Number => cell.AsNumber().ToString(CultureInfo.InvariantCulture),
                    CellKind.Formula => "=" + cell.AsFormula().ToString(),
                    _ => throw new InvalidOperationException("Invalid cell kind.")
                };
            });

        // We can wrap the data in an outer object to match the format specified in the example.
        var wrapper = new { Cells = data };

        // Now we can serialize the wrapper object to JSON and write it to the file.
        try
        {
            var opt = new JsonSerializerOptions
                { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            var json = JsonSerializer.Serialize(wrapper, opt);
            File.WriteAllText(filename, json);
        }
        catch (Exception e) when (e is IOException || e is JsonException)
        {
            throw new SpreadsheetReadWriteException($"Error saving spreadsheet: {e.Message}");
        }

        // Finally, we can mark the spreadsheet as not changed, since we just saved it.
        Changed = false;
    }

    /// <summary>
    ///   <para>
    ///     Return the value of the named cell.
    ///   </para>
    /// </summary>
    /// <param name="name"> The cell in question. </param>
    /// <returns>
    ///   Returns the value (as opposed to the contents) of the named cell.  The return
    ///   value should be either a string, a double, or a CS3500.Formula.FormulaError.
    /// </returns>
    /// <exception cref="InvalidNameException">
    ///   If the provided name is invalid, throws an InvalidNameException.
    /// </exception>
    public object GetCellValue(string name)
    {
        var location = LocationOfReference(name);
        var cell = _cells.GetValueOrDefault(location, new Cell(string.Empty));

        try
        {
            return cell.Kind switch
            {
                CellKind.Text => cell.AsText(),
                CellKind.Number => cell.AsNumber(),
                CellKind.Formula => cell.AsFormula().Evaluate((varName) =>
                {
                    var value = GetCellValue(varName);
                    return value is not double d
                        ? throw new FormulaFormatException($"Variable {varName} does not evaluate to a number.")
                        : d;
                }),
                _ => throw new InvalidOperationException("Invalid cell kind.")
            };
        }
        catch (FormulaFormatException e)
        {
            return new FormulaError(e.Message);
        }
    }

    /// <summary>
    ///   <para>
    ///     Set the contents of the named cell to be the provided string
    ///     which will either represent (1) a string, (2) a number, or
    ///     (3) a formula (based on the prepended '=' character).
    ///   </para>
    ///   <para>
    ///     Rules of parsing the input string:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///       <para>
    ///         If 'content' parses as a double, the contents of the named
    ///         cell becomes that double.
    ///       </para>
    ///     </item>
    ///     <item>
    ///         If the string does not begin with an '=', the contents of the
    ///         named cell becomes 'content'.
    ///     </item>
    ///     <item>
    ///       <para>
    ///         If 'content' begins with the character '=', an attempt is made
    ///         to parse the remainder of content into a Formula f using the Formula
    ///         constructor.  There are then three possibilities:
    ///       </para>
    ///       <list type="number">
    ///         <item>
    ///           If the remainder of content cannot be parsed into a Formula, a
    ///           CS3500.Formula.FormulaFormatException is thrown.
    ///         </item>
    ///         <item>
    ///           Otherwise, if changing the contents of the named cell to be f
    ///           would cause a circular dependency, a CircularException is thrown,
    ///           and no change is made to the spreadsheet.
    ///         </item>
    ///         <item>
    ///           Otherwise, the contents of the named cell becomes f.
    ///         </item>
    ///       </list>
    ///     </item>
    ///   </list>
    /// </summary>
    /// <returns>
    ///   <para>
    ///     The method returns a list consisting of the name plus the names
    ///     of all other cells whose value depends, directly or indirectly,
    ///     on the named cell. The order of the list should be any order
    ///     such that if cells are re-evaluated in that order, their dependencies
    ///     are satisfied by the time they are evaluated.
    ///   </para>
    ///   <example>
    ///     For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
    ///     list {A1, B1, C1} is returned.
    ///   </example>
    /// </returns>
    /// <exception cref="InvalidNameException">
    ///     If name is invalid, throws an InvalidNameException.
    /// </exception>
    /// <exception cref="CircularException">
    ///     If a formula resulted in a circular dependency, throws CircularException.
    /// </exception>
    public IList<string> SetContentsOfCell(string name, string content)
    {
        var location = LocationOfReference(name);
        var cell = content switch
        {
            _ when double.TryParse(content, out var number) => new Cell(number),
            _ when content.StartsWith('=') => new Cell(new Formula(content.Substring(1))),
            _ => new Cell(content)
        };

        SetCellContents(location, cell);
        Changed = true;

        return GetCellsToRecalculate(location)
            .Select(c => CellLocation.Canonicalize(c.ColumnIndex, c.RowIndex))
            .ToList();
    }
}

/// <summary>
///     <para>
///       Thrown to indicate that a change to a cell will cause a circular dependency.
///     </para>
/// </summary>
public class CircularException : Exception
{
}

/// <summary>
///     <para>
///       Thrown to indicate that a name parameter was invalid.
///     </para>
/// </summary>
public class InvalidNameException : Exception
{
}

/// <summary>
///     <para>
///         Thrown to indicate that a read or write attempt has failed with
///         an expected error message informing the user of what went wrong.
///     </para>
/// </summary>
public class SpreadsheetReadWriteException : Exception
{
    /// <summary>
    ///   <para>
    ///     Creates the exception with a message defining what went wrong.
    ///   </para>
    /// </summary>
    /// <param name="msg"> An informative message to the user. </param>
    public SpreadsheetReadWriteException(string msg)
        : base(msg)
    {
    }
}