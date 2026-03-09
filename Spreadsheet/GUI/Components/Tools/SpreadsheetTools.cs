// <copyright file="SpreadsheetTools.cs" company="UofU-CS3500">
// Copyright (c) 2026 UofU-CS3500. All rights reserved.
// </copyright>
// Written by Professor Ahmad Alsaleem and Hung Phan Quoc Viet for CS 3500, Spring 2026 

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
    /// TODO: Implement another method that performs a task the AI model can interact with
    /// TODO: Update the method signature and this comment as appropriate.
    /// </summary>
    /// <param name="cellName"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [Description("to be completed")]
    public string AnotherMethods(string cellName)
    {
        throw new NotImplementedException();
    }
}