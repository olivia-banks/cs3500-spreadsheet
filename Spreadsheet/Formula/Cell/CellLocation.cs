using System.Text;

namespace Formula.Cell;

public record CellLocation(int ColumnIndex, int RowIndex)
{
    /// <summary>
    ///     <para>
    ///         This method is intended to parse a canonical cell reference string (e.g., "A1", "B2", "AA10") and
    ///         convert it into a <see cref="CellLocation"/> instance with zero-based column and row indices.
    ///     </para>
    /// </summary>
    /// <param name="cellRepr">The <see cref="string"/> representing the location.</param>
    /// <returns>The <see cref="CellLocation"/> representing the parameters.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public static CellLocation FromString(string cellRepr)
    {
        
        // How do we parse cell references? Well, they start with one or more letters (A-Z, case-insensitive)
        // indicating the column, followed by one or more digits (0-9) indicating the row.
        //
        // For example, "A1" is column 0, row 0; "B2" is column 1, row 1; "AA10" is column 26, row 9.
        // We need to convert the letters to a zero-based column index and the digits to a zero-based row index.
        // 
        // For the column, we treat the letters as a base-26 number, where A=1, B=2, ..., Z=26, and we accumulate
        // the value accordingly. We normalize to capital letters, subtract from 'A' to get the zero-based index
        // of any letters, and then multiply the accumulated value by 26 for each new letter we encounter.
        //
        // While doing this, we keep track of our position in the string so that when we reach the digits, we can
        // say "okay, everything from here on to the end is the row number." This is then just parsed as an integer
        // without any special sauce.
        
        var columnStated = 0;
        var columnCursor = 0;

        var columnIndex = -1;
        var rowIndex = -1;
        
        try
        {
            // Get the column part.
            for (; char.IsLetter(cellRepr[columnCursor]); columnCursor++)
            {
                columnStated = checked(columnStated * 26 + (char.ToUpperInvariant(cellRepr[columnCursor]) - 'A' + 1));
            }

            // Subtract 1 to get zero-based index.
            columnIndex = checked(columnStated - 1);

            // Get row part (from where we left off to the end).
            if (!int.TryParse(cellRepr[columnCursor..], out rowIndex))
            {
                throw new ArgumentException("too many digits.");
            }

            // Subtract 1 to get zero-based index.
            rowIndex = checked(rowIndex - 1);
        }
        
        //catch (IndexOutOfRangeException)
        //{
        //    throw new FormulaFormatException(
        //        $"{span}: invalid cell reference `{cellRepr}'; expected letters followed by digits.");
        //}
        catch (OverflowException)
        {
            throw new ArgumentException("too large to be represented.");
        }
        
        return new CellLocation(columnIndex, rowIndex);
    }
    
    /// <summary>
    ///     <para>
    ///         This method converts zero-based column and row indices into their canonical cell reference form.
    ///         The code for doing this is not in-line with the <see cref="Expressions.ExpressionCanonicalizer"/>
    ///         since we need to use this inside <see cref="Formula.GetVariables"/> to comply with the assignment.
    ///     </para>
    ///     <para>
    ///         Speaking of assignment, this implements the canonical form outlined in PS2.
    ///     </para>
    /// </summary>
    /// <param name="columnIndex">The 0-based column index of the cell reference.</param>
    /// <param name="rowIndex">The 0-based row index of the cell reference.</param>
    /// <returns>The canonical string form.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static string Canonicalize(int columnIndex, int rowIndex)
    {
        if (columnIndex < 0 || rowIndex < 0)
            throw new InvalidOperationException("Cannot canonicalize a cell reference with negative indices.");

        // Efficiency!
        StringBuilder canonicalFormBuilder = new();
        
        // Convert column index to letters via base-26 representation with 'A' = 0, 'B' = 1, ..., 'Z' = 25.
        {
            var columnChars = new List<char>();

            do
            {
                var remainder = columnIndex % 26;
                columnChars.Add((char)('A' + remainder));
                columnIndex = columnIndex / 26 - 1;
            } while (columnIndex >= 0);

            columnChars.Reverse();
            canonicalFormBuilder.Append(new string(columnChars.ToArray()));
        }

        // Convert row index to 1-based number.
        canonicalFormBuilder.Append(rowIndex + 1);

        return canonicalFormBuilder.ToString();
    }
    
    /// <summary>
    ///     <para>
    ///         Converts the current <see cref="CellLocation"/> instance to its canonical string form using the static
    ///         <see cref="Canonicalize(int, int)"/> method.
    ///     </para>
    /// </summary>
    /// <returns></returns>
    public string ToCanonicalString() => Canonicalize(ColumnIndex, RowIndex);
}