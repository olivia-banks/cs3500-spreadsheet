using System.Text;

namespace Formula.Util;

/// <summary>
///     <para>
///          Converts zero-based column and row indices into their canonical cell reference form.
///         See <see cref="Canonicalize"/>.
///     </para>
/// </summary>
public static class CellReferenceCanonicalizer
{
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
}