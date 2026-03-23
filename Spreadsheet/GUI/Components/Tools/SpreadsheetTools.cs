// <copyright file="SpreadsheetTools.cs" company="UofU-CS3500">
// Copyright (c) 2026 UofU-CS3500. All rights reserved.
// </copyright>
// Written by Professor Ahmad Alsaleem and Hung Phan Quoc Viet for CS 3500, Spring 2026 

using System.Text;

namespace GUI.Components.Tools;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Formula;
using Spreadsheet;

/// <summary>
///     <para>
///         Provides a set of tools that allow an AI model to interact with a <see cref="Spreadsheet"/> instance.
///     </para>
/// </summary>
/// <remarks>
///     This class is designed to be used with <see cref="Microsoft.Extensions.AI.AIFunctionFactory"/>
///     to expose spreadsheet capabilities to an LLM.
/// </remarks>
/// <param name="sheet">The spreadsheet instance this toolset will operate on.</param>
public class SpreadsheetTools(Spreadsheet sheet)
{
    /// <summary>
    ///     <para>
    ///         Updates the content of a specific cell in the spreadsheet.
    ///     </para>
    /// </summary>
    /// <param name="cellName">The cell coordinate (e.g., "A1", "B10").</param>
    /// <param name="value">The new content or formula to place in the cell.</param>
    /// <returns>
    ///     A status message indicating the operation was successful.
    /// </returns>
    [Description("Sets the contents of a spreadsheet cell.")]
    public string SetCellContent(string cellName, string value)
    {
        sheet.SetContentsOfCell(cellName, value);
        return "Success";
    }
    
    /// <summary>
    ///     <para>
    ///         Gets a cells contents for the user, so that if a cell has a formula
    ///         the user can see the formula and how it equated to the displayed value.
    ///     </para>
    /// </summary>
    /// <param name="cellName"></param>
    /// <returns>
    ///     The user-facing contents of the requested cell.
    /// </returns>
    [Description("Gets the contents of a spreadsheet cell and displays it the user.")]
    public string GetCellContentInfo(string cellName)
    {
        return "Success, the contents of the cell are: " + sheet.GetCellContents(cellName);
    }
    
    /// <summary>
    ///     <para>
    ///         Gets all the names of non-empty cells for the user, so they can keep track
    ///         of what they have put into the spreadsheet.
    ///     </para>
    /// </summary>
    /// <param name="cellName"></param>
    /// <returns>
    ///     A comma-separated list of non-empty cells, or a message indicating the sheet is empty.
    /// </returns>
    [Description("Gets the names of all non-empty spreadsheet cells.")]
    public string GetActiveCells()
    {
        var cells = sheet.GetNamesOfAllNonemptyCells().OrderBy(c => c).ToList();
        if (cells.Count == 0)
            return "The spreadsheet is empty.";
        return "Non-empty cells: " + string.Join(", ", cells);
    }

    /// <summary>
    ///     <para>
    ///         Gets the evaluated value of a spreadsheet cell as a string.
    ///     </para>
    ///     <para>
    ///         For formula cells, returns the computed result. For text/number cells, returns the value.
    ///     </para>
    /// </summary>
    /// <param name="cellName">The cell coordinate (e.g., "A1").</param>
    /// <returns>
    ///     The evaluated value of the cell, or an error message if the cell could not be evaluated.
    /// </returns>
    [Description("Gets the evaluated value of a spreadsheet cell.")]
    public string GetCellValue(string cellName)
    {
        try
        {
            var value = sheet.GetCellValue(cellName);
            return value switch
            {
                string s => $"Cell {cellName} value: {s}",
                double d => $"Cell {cellName} value: {d}",
                FormulaError fe => $"Cell {cellName} error: {fe.Reason}",
                _ => $"Cell {cellName} value: {value}"
            };
        }
        catch (Exception ex)
        {
            return $"Error reading cell {cellName}: {ex.Message}";
        }
    }

    /// <summary>
    ///     <para>
    ///         Gets all cell values in a rectangular range (e.g., "A1:C5").
    ///     </para>
    ///     <para>
    ///         Returns a formatted string with each cell's value.
    ///     </para>
    /// </summary>
    /// <param name="rangeStart">Starting cell (e.g., "A1").</param>
    /// <param name="rangeEnd">Ending cell (e.g., "C5").</param>
    /// <returns>
    ///     A formatted string containing all cell values in the range.
    /// </returns>
    [Description("Gets all cell values in a rectangular range of cells (e.g., A1:C5), formatted as a plaintext grid.")]
    public string GetCellRange(string rangeStart, string rangeEnd)
    {
        try
        {
            var startCol = ParseColumnIndex(rangeStart);
            var startRow = ParseRowIndex(rangeStart);
            var endCol = ParseColumnIndex(rangeEnd);
            var endRow = ParseRowIndex(rangeEnd);
            return FormatGrid(startCol, startRow, endCol, endRow);
        }
        catch (Exception ex)
        {
            return $"Error reading range {rangeStart}:{rangeEnd}: {ex.Message}";
        }
    }

    /// <summary>
    ///     <para>
    ///         Returns the entire populated area of the spreadsheet as a plaintext grid,
    ///         auto-detecting the bounding box of all non-empty cells.
    ///     </para>
    /// </summary>
    [Description("Returns the entire spreadsheet as a plaintext grid showing all non-empty cell values with row and column headers.")]
    public string GetSpreadsheetSnapshot()
    {
        var cells = sheet.GetNamesOfAllNonemptyCells().ToList();
        if (cells.Count == 0)
            return "The spreadsheet is empty.";

        var cols = cells.Select(ParseColumnIndex);
        var rows = cells.Select(ParseRowIndex);
        return FormatGrid(cols.Min(), rows.Min(), cols.Max(), rows.Max());
    }

    /// <summary>
    ///     <para>
    ///         Gets the immediate dependencies of a cell (cells that this cell depends on in formulas).
    ///     </para>
    /// </summary>
    /// <param name="cellName">The cell name (e.g., "A1").</param>
    /// <returns>
    ///     A formatted list of cells that the given cell depends on.
    /// </returns>
    [Description("Gets the cells that a given cell depends on (cells referenced in its formula).")]
    public string GetCellDependencies(string cellName)
    {
        try
        {
            var deps = sheet.GetCellDependencies(cellName).ToList();
            if (deps.Count == 0)
                return $"Cell {cellName} has no dependencies (is not a formula or does not reference other cells).";

            var result = new StringBuilder();
            result.AppendLine($"Cell {cellName} depends on: {string.Join(", ", deps)}");
            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error getting dependencies for {cellName}: {ex.Message}";
        }
    }

    /// <summary>
    ///     <para>
    ///         Gets the immediate dependents of a cell (cells whose formulas reference this cell).
    ///     </para>
    /// </summary>
    /// <param name="cellName">The cell name (e.g., "A1").</param>
    /// <returns>
    ///     A formatted list of cells that depend on the given cell.
    /// </returns>
    [Description("Gets the cells that depend on a given cell (cells whose formulas reference it).")]
    public string GetCellDependents(string cellName)
    {
        try
        {
            var deps = sheet.GetCellDependents(cellName).ToList();
            if (deps.Count == 0)
                return $"No cells depend on {cellName}.";

            var result = new StringBuilder();
            result.AppendLine($"Cells that depend on {cellName}: {string.Join(", ", deps)}");
            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error getting dependents for {cellName}: {ex.Message}";
        }
    }

    /// <summary>
    ///     <para>
    ///         Formats a rectangular range of cells as a plaintext grid with column-letter headers and row-number labels.
    ///     </para>
    /// </summary>
    private string FormatGrid(int startCol, int startRow, int endCol, int endRow)
    {
        const int ColWidth = 11;
        var sb = new StringBuilder();

        // Header: leading row-number column, then column letters
        sb.Append(string.Empty.PadRight(5));
        for (var c = startCol; c <= endCol; c++)
            sb.Append(((char)('A' + c)).ToString().PadRight(ColWidth));
        sb.AppendLine();

        for (var r = startRow; r <= endRow; r++)
        {
            sb.Append((r + 1).ToString().PadRight(5));
            for (var c = startCol; c <= endCol; c++)
            {
                var cellName = $"{(char)('A' + c)}{r + 1}";
                var raw = sheet.GetCellValue(cellName);
                var display = raw switch
                {
                    string s => s,
                    double d => d.ToString(),
                    FormulaError fe => $"#ERR({fe.Reason})",
                    _ => raw?.ToString() ?? string.Empty
                };
                sb.Append(display.PadRight(ColWidth));
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     <para>
    ///         Helper method to parse column index from Excel cell notation (A=0, B=1, ..., Z=25).
    ///     </para>
    /// </summary>
    private static int ParseColumnIndex(string cellName)
    {
        if (string.IsNullOrEmpty(cellName) || !char.IsLetter(cellName[0]))
            throw new ArgumentException($"Invalid cell name: {cellName}");
        
        var col = char.ToUpperInvariant(cellName[0]) - 'A';
        if (col is < 0 or > 25)
            throw new ArgumentException($"Invalid cell name: {cellName}");

        return col;
    }

    /// <summary>
    ///     <para>
    ///         Helper method to parse row index from Excel cell notation (1-based → 0-based, so row 1 = index 0).
    ///     </para>
    /// </summary>
    private static int ParseRowIndex(string cellName)
    {
        if (string.IsNullOrEmpty(cellName) || !int.TryParse(cellName.Substring(1), out var row) || row < 1)
            throw new ArgumentException($"Invalid cell name: {cellName}");
        return row - 1;  // Convert 1-based row to 0-based index
    }
}