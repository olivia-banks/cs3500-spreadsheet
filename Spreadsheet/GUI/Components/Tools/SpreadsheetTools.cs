// <copyright file="SpreadsheetTools.cs" company="UofU-CS3500">
// Copyright (c) 2026 UofU-CS3500. All rights reserved.
// </copyright>
// Written by Professor Ahmad Alsaleem and Hung Phan Quoc Viet for CS 3500, Spring 2026 

using System.Text;

namespace GUI.Components.Tools;

using System;
using System.ComponentModel;
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
}