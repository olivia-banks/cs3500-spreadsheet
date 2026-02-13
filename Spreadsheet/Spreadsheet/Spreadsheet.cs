namespace Spreadsheet;

using Formula.Cell;
using Formula;
using DependencyGraph;

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
    ///         This method converts a cell name string into a CellLocation object. It is used internally to parse cell
    ///         names in the public methods that take string names, and it also serves as a single point of truth for
    ///         how we parse and validate cell names. If the name is invalid, it throws an InvalidNameException.
    ///     </para>
    /// </summary>
    /// <param name="name">The cell name string to parse.</param>
    /// <returns>The CellLocation corresponding to the given name.</returns>
    private CellLocation locationOfReference(string name)
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
        => GetCellContents(locationOfReference(name));

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
    public IList<CellLocation> SetCellContents(CellLocation location, Cell cell)
    {
        try
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
        catch (ArgumentException)
        {
            throw new InvalidNameException();
        }
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
    public IList<string> SetCellContents(string name, double number)
        => SetCellContents(locationOfReference(name), new Cell(number))
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
    public IList<string> SetCellContents(string name, string text)
        => SetCellContents(locationOfReference(name), new Cell(text))
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
    public IList<string> SetCellContents(string name, Formula formula)
        => SetCellContents(locationOfReference(name), new Cell(formula))
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
}