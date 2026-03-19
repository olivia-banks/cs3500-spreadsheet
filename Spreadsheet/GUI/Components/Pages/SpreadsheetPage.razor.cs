// <copyright file="Spreadsheet.cs" company="UofU-CS3500">
// Copyright (c) 2026 UofU-CS3500. All rights reserved.
// </copyright>
// Written by Joe Zachary for CS 3500, September 2013
// Update by Profs Kopta and de St. Germain, Fall 2021, Fall 2024
//     - Updated return types
//     - Updated documentation
// Update by Prof Alsaleem and Hung Phan, Spring 2026
//     - Updated the assignment to include the AI Agent component for Spring 2026.

using System;
using System.IO;
using System.Threading.Tasks;
using Formula.Cell;

namespace GUI.Components.Pages;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics;
using Services;
using Spreadsheet;

/// <summary>
/// TODO: Fill in
/// </summary>
public partial class SpreadsheetPage
{
    /// <summary>
    /// Based on your computer, you could shrink/grow this value based on performance.
    /// </summary>
    private const int ROWS = 50;

    /// <summary>
    /// Number of columns, which will be labeled A-Z.
    /// </summary>
    private const int COLS = 26;

    /// <summary>
    /// Provides an easy way to convert from an index to a letter (0 -> A)
    /// </summary>
    private char[] Alphabet { get; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    /// <summary>
    /// Gets or sets the name of the file to be saved
    /// </summary>
    private string FileSaveName { get; set; } = "Spreadsheet.sprd";

    /// <summary>
    ///   <para> Gets or sets the data for all of the cells in the spreadsheet GUI. </para>
    ///   <remarks>Backing Store for HTML</remarks>
    /// </summary>
    private string[,] CellsBackingStore { get; set; } = new string[ROWS, COLS];
    
    /// <summary>
    /// The spreadsheet object
    /// </summary>
    private Spreadsheet spreadsheet = new();
    
    /// <summary>
    /// This field is responsible for hiding/showing the chat for the AI agent
    /// </summary>
    private bool _isCollapsed;

    /// <summary>
    /// This field is responsible for hiding/showing the input box for manually putting in values to the
    /// spreadsheet.
    /// </summary>
    private bool _cellTextBoxVisability = true;

    /// <summary>
    /// Gets or sets the AI service responsible for processing natural language
    /// queries and applying the resulting changes to the spreadsheet model.
    /// </summary>
    /// <remarks>
    /// This service is injected via dependency injection and is used to
    /// interpret user chat input, modify the underlying <see cref="Spreadsheet"/>
    /// object, and maintain AI interaction history.
    ///
    /// We set it to null! to suppress nullable warning,
    /// this property is set by Blazor's dependency injection.
    /// </remarks>
    [Inject]
    private SpreadsheetAIService AIService { get; set; } = null!;

    /// <summary>
    /// The user's input for the AI device
    /// </summary>
    private string UserInput { get; set; } = "";
    
    /// <summary>
    /// The user's input from the manual cell input text box
    /// </summary>
    private string UserManualInput { get; set; } = "";
    
    /// <summary>
    /// This represents the currently selected cell by the user
    /// by default it is set to A1 unless changed
    /// </summary>
    private string _activeCell = "A1";

    /// <summary>
    /// Handler for when a cell is clicked
    /// </summary>
    /// <param name="row">The row component of the cell's coordinates</param>
    /// <param name="col">The column component of the cell's coordinates</param>
    private void CellClicked( int row, int col )
    {
        _cellTextBoxVisability = false;
        int actualRow = row + 1;
        _activeCell = "" + Alphabet[col] + actualRow;
        if (spreadsheet.GetNamesOfAllNonemptyCells().Contains(_activeCell))
        {
            UserManualInput = spreadsheet.GetCellContents(_activeCell).ToString();
        }
    }
    
    /// <summary>
    /// Saves the current spreadsheet, by providing a download of a file
    /// containing the json representation of the spreadsheet.
    /// </summary>
    private async void SaveFile()
    {
        await JSRuntime.InvokeVoidAsync( "downloadFile", FileSaveName, 
            "replace this with the json representation of the current spreadsheet" );
    }

    /// <summary>
    /// This method will run when the file chooser is used, for loading a file.
    /// Uploads a file containing a json representation of a spreadsheet, and 
    /// replaces the current sheet with the loaded one.
    /// </summary>
    /// <param name="args">The event arguments, which contains the selected file name</param>
    private async void HandleFileChooser( EventArgs args )
    {
        try
        {
            string fileContent = string.Empty;

            InputFileChangeEventArgs eventArgs = args as InputFileChangeEventArgs ?? throw new Exception("unable to get file name");
            if ( eventArgs.FileCount == 1 )
            {
                var file = eventArgs.File;
                if ( file is null )
                {
                    return;
                }

                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                // fileContent will contain the contents of the loaded file
                fileContent = await reader.ReadToEndAsync();

                // TODO: Use the loaded fileContent to replace the current spreadsheet

                StateHasChanged();
            }
        }
        catch ( Exception e )
        {
            Debug.WriteLine( "An error occurred while loading the file..." + e );
        }
    }
    
    
    /// <summary>
    /// Processes the user's input through the AI service and updates the UI state.
    /// </summary>
    private async Task SubmitChat()
    {
        // We pass the work to the service; it handles the boolean and the history
        await AIService.ProcessQueryAsync(UserInput, spreadsheet);
        SyncUIWithSpreadsheet();
        UserInput = ""; // Reset the local text box
    }
    
    /// <summary>
    /// Takes in a string from the user and then adds it as the contents to the
    /// currently active cell in the spreadsheet
    /// </summary>
    private async Task SubmitCellInfo()
    {
        spreadsheet.SetContentsOfCell(_activeCell, UserManualInput);
        SyncUIWithSpreadsheet();
        _cellTextBoxVisability = true;
        UserManualInput = ""; // Reset the local text box
    }

    /// <summary>
    /// Synchronizes the entire UI grid by polling the latest calculated values from the spreadsheet and filling CellsBackingStore .
    /// It will need to call StateHasChanged to make sure that everything is being display on the page.
    /// </summary>
    private void SyncUIWithSpreadsheet()
    {
        foreach (CellLocation cellSpot in spreadsheet.GetLocationsOfAllNonemptyCells())
        {
            CellsBackingStore[cellSpot.RowIndex, cellSpot.ColumnIndex] = spreadsheet.GetCellValue(cellSpot.ToCanonicalString()) + "";
        }
        // Now tell Blazor the data in the array has changed
        StateHasChanged();
    }
}
