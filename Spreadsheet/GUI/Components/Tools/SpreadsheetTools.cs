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
/// Provides a set of tools that allow an AI model to interact with a <see cref="Spreadsheet"/> instance.
/// </summary>
/// <remarks>
/// This class is designed to be used with <see cref="Microsoft.Extensions.AI.AIFunctionFactory"/> 
/// to expose spreadsheet capabilities to an LLM.
/// </remarks>
/// <param name="sheet">The spreadsheet instance this toolset will operate on.</param>
public class SpreadsheetTools(Spreadsheet sheet)
{
    /// <summary>
    /// Updates the content of a specific cell in the spreadsheet.
    /// </summary>
    /// <param name="cellName">The cell coordinate (e.g., "A1", "B10").</param>
    /// <param name="value">The new content or formula to place in the cell.</param>
    /// <returns>A status message indicating the operation was successful.</returns>
    [Description("Sets the contents of a spreadsheet cell.")]
    public string SetCellContent(string cellName, string value)
    {
        sheet.SetContentsOfCell(cellName, value);
        return "Success";
    }
    
    /// <summary>
    /// Gets a cells contents for the user, so that if a cell has a formula
    /// the user can see the formula and how it equated to the displayed value
    /// </summary>
    /// <param name="cellName"></param>
    /// <returns></returns>
    [Description("Gets the contents of a spreadsheet cell and displays it the user.")]
    public string GetCellContentInfo(string cellName)
    {
        return "Success, the contents of the cell are: " + sheet.GetCellContents(cellName);
    }
    
    /// <summary>
    /// Gets all the names of non-empty cells for the user, so they can keep track
    /// of what they have put into the spreadsheet
    /// </summary>
    /// <param name="cellName"></param>
    /// <returns></returns>
    [Description("Gets the names of all non-empty spreadsheet cells and displays it the user.")]
    public string GetActiveCells()
    {
        StringBuilder allActiveCells = new StringBuilder();
        foreach (string cellName in sheet.GetNamesOfAllNonemptyCells())
        {
            allActiveCells.Append(cellName);
        }
        return "Success, these are all the filled cells: " + allActiveCells.ToString();
    }

    /// <summary>
    /// Gets the evaluated value of a spreadsheet cell as a string.
    /// For formula cells, returns the computed result. For text/number cells, returns the value.
    /// </summary>
    /// <param name="cellName">The cell coordinate (e.g., "A1").</param>
    /// <returns>The evaluated value of the cell, or an error message if the cell could not be evaluated.</returns>
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
    /// Gets all cell values in a rectangular range (e.g., "A1:C5").
    /// Returns a formatted string with each cell's value.
    /// </summary>
    /// <param name="rangeStart">Starting cell (e.g., "A1").</param>
    /// <param name="rangeEnd">Ending cell (e.g., "C5").</param>
    /// <returns>A formatted string containing all cell values in the range.</returns>
    [Description("Gets all cell values in a rectangular range of cells (e.g., A1:C5).")]
    public string GetCellRange(string rangeStart, string rangeEnd)
    {
        try
        {
            var results = new StringBuilder();
            results.AppendLine($"Values in range {rangeStart}:{rangeEnd}:");

            // Parse start and end cell names (e.g., "A1", "C5")
            var startCol = ParseColumnIndex(rangeStart);
            var startRow = ParseRowIndex(rangeStart);
            var endCol = ParseColumnIndex(rangeEnd);
            var endRow = ParseRowIndex(rangeEnd);

            for (var r = startRow; r <= endRow; r++)
            {
                for (var c = startCol; c <= endCol; c++)
                {
                    var cellName = $"{(char)('A' + c)}{r + 1}";
                    var value = sheet.GetCellValue(cellName);
                    var valueStr = value switch
                    {
                        string s => s,
                        double d => d.ToString(),
                        FormulaError fe => $"Error: {fe.Reason}",
                        _ => value.ToString()
                    };
                    results.Append($"{cellName}={valueStr} | ");
                }
                results.AppendLine();
            }

            return results.ToString();
        }
        catch (Exception ex)
        {
            return $"Error reading range {rangeStart}:{rangeEnd}: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the immediate dependencies of a cell (cells that this cell depends on in formulas).
    /// </summary>
    /// <param name="cellName">The cell name (e.g., "A1").</param>
    /// <returns>A formatted list of cells that the given cell depends on.</returns>
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
    /// Gets the immediate dependents of a cell (cells whose formulas reference this cell).
    /// </summary>
    /// <param name="cellName">The cell name (e.g., "A1").</param>
    /// <returns>A formatted list of cells that depend on the given cell.</returns>
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
    /// Helper method to parse column index from Excel cell notation (A=0, B=1, ..., Z=25).
    /// </summary>
    private static int ParseColumnIndex(string cellName)
    {
        if (string.IsNullOrEmpty(cellName) || !char.IsLetter(cellName[0]))
            throw new ArgumentException($"Invalid cell name: {cellName}");
        return cellName[0] - 'A';
    }

    /// <summary>
    /// Helper method to parse row index from Excel cell notation (1-based → 0-based, so row 1 = index 0).
    /// </summary>
    private static int ParseRowIndex(string cellName)
    {
        if (string.IsNullOrEmpty(cellName) || !int.TryParse(cellName.Substring(1), out var row) || row < 1)
            throw new ArgumentException($"Invalid cell name: {cellName}");
        return row - 1;  // Convert 1-based row to 0-based index
    }
}