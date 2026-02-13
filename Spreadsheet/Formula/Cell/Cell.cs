namespace Formula.Cell;

/// <summary>
///     <para>
///         A cell is a value that can be either a number, text, or a formula. It is immutable, and can be used to
///         represent the value of a cell in a spreadsheet by proxy.
///     </para>
/// </summary>
public readonly struct Cell
{
    /// <summary>
    ///     <para>
    ///         A numeric value. This is the most common type of cell, and can be used to represent any numeric value, including
    ///         integers, decimals, and even special values like NaN and Infinity.
    ///     </para>
    /// </summary>
    private readonly double _number;
    
    /// <summary>
    ///     <para>
    ///         A text value. This can be used to represent any string value, including empty strings and strings
    ///         with special characters.
    ///     </para>
    /// </summary>
    private readonly string? _text;
    
    /// <summary>
    ///     <para>
    ///         A formula. This can be used to represent a formula that can be evaluated to produce a value. The formula
    ///         can reference other cells, and can be used to perform calculations based on the values of those cells.
    ///     </para>
    /// </summary>
    private readonly Formula? _formula;

    /// <summary>
    ///     <para>
    ///         The kind of cell. This is used to determine the inner typing of the cell's value, in the absense of
    ///         language backed discriminated unions.
    ///     </para>
    /// </summary>
    public CellKind Kind { get; }

    public Cell(double number)
    {
        _number = number;
        _text = null;
        _formula = null;
        Kind = CellKind.Number;
    }

    public Cell(string text)
    {
        _number = default;
        _text = text;
        _formula = null;
        Kind = CellKind.Text;
    }

    public Cell(Formula formula)
    {
        _number = default;
        _text = null;
        _formula = formula;
        Kind = CellKind.Formula;
    }

    public double AsNumber() =>
        Kind == CellKind.Number
            ? _number
            : throw new InvalidOperationException();

    public string AsText() =>
        Kind == CellKind.Text
            ? _text!
            : throw new InvalidOperationException();

    public Formula AsFormula() =>
        Kind == CellKind.Formula
            ? _formula!
            : throw new InvalidOperationException();
    
    public object AsObject() =>
        Kind switch
        {
            CellKind.Number => AsNumber(),
            CellKind.Text => AsText(),
            CellKind.Formula => AsFormula(),
            _ => throw new InvalidOperationException()
        };
}

public enum CellKind
{
    Number,
    Text,
    Formula
}
