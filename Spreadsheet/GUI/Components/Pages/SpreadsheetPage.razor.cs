using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CircularException = Spreadsheet.CircularException;
using FormulaError = Formula.FormulaError;
using FormulaFormatException = Formula.FormulaFormatException;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SpreadsheetDoc = Spreadsheet.Spreadsheet;

namespace GUI.Components.Pages;

/// <summary>
///     <para>
///         Main spreadsheet page state and interaction logic for grid editing, search, AI, and persistence.
///     </para>
/// </summary>
public partial class SpreadsheetPage : IAsyncDisposable
{
    /// <summary>
    ///     <para>
    ///         Total number of visible spreadsheet rows.
    ///     </para>
    /// </summary>
    private const int Rows = 50;

    /// <summary>
    ///     <para>
    ///         Total number of visible spreadsheet columns.
    ///     </para>
    /// </summary>
    private const int Cols = 26;

    /// <summary>
    ///     <para>
    ///         Default name assigned to a newly initialized document.
    ///     </para>
    /// </summary>
    private const string DefaultDocumentName = "Untitled";

    /// <summary>
    ///     <para>
    ///         Maximum browser-upload stream size accepted for spreadsheet import.
    ///     </para>
    /// </summary>
    private const long MaxUploadBytes = 5 * 1024 * 1024;

    /// <summary>
    ///     <para>
    ///         Maximum buffer size used while reading uploaded JSON content.
    ///     </para>
    /// </summary>
    private const int MaxFileReadBytes = 2 * 1024 * 1024;

    /// <summary>
    ///     <para>
    ///         Active backend spreadsheet document.
    ///     </para>
    /// </summary>
    private SpreadsheetDoc _sheet = new();

    /// <summary>
    ///     <para>
    ///         Set of cell keys currently matching the active find query.
    ///     </para>
    /// </summary>
    private readonly HashSet<string> _findMatches = new();

    /// <summary>
    ///     <para>
    ///         Local-storage documents currently loaded into the open dialog.
    ///     </para>
    /// </summary>
    private readonly List<LocalDocument> _localDocuments = [];

    /// <summary>
    ///     <para>
    ///         Built-in sample document metadata for template loading.
    ///     </para>
    /// </summary>
    private readonly List<SampleDocument> _sampleDocuments =
    [
        new("Q3 Variance Report", "Jul 14, 2025", SampleTemplateKey.Q3VarianceReport),
        new("Headcount Model FY25", "Jul 9, 2025", SampleTemplateKey.HeadcountModel),
        new("Budget Consolidation v4", "Jun 28, 2025", SampleTemplateKey.BudgetConsolidation),
        new("Actuals vs Plan - H1", "Jun 15, 2025", SampleTemplateKey.ActualsVsPlan)
    ];

    [Inject]
    /// <summary>
    ///     <para>
    ///         JS runtime used for module import and browser interop.
    ///     </para>
    /// </summary>
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    ///     <para>
    ///         DotNet callback reference passed to JavaScript.
    ///     </para>
    /// </summary>
    private DotNetObjectReference<SpreadsheetPage>? _dotNetRef;

    /// <summary>
    ///     <para>
    ///         JS module handle for spreadsheet page helpers.
    ///     </para>
    /// </summary>
    private IJSObjectReference? _jsModule;

    /// <summary>
    ///     <para>
    ///         Whether the cell edit input should be focused after render.
    ///     </para>
    /// </summary>
    private bool _needsFocusEditInput;

    /// <summary>
    ///     <para>
    ///         Cancellation source for replacing in-flight notification timers.
    ///     </para>
    /// </summary>
    private CancellationTokenSource? _notificationCts;

    /// <summary>
    ///     <para>
    ///         Zero-based row index of current selection.
    ///     </para>
    /// </summary>
    private int SelectedRow { get; set; }

    /// <summary>
    ///     <para>
    ///         Zero-based column index of current selection.
    ///     </para>
    /// </summary>
    private int SelectedCol { get; set; }

    /// <summary>
    ///     <para>
    ///         Zero-based row index currently being edited in-grid.
    ///     </para>
    /// </summary>
    private int EditingRow { get; set; } = -1;

    /// <summary>
    ///     <para>
    ///         Zero-based column index currently being edited in-grid.
    ///     </para>
    /// </summary>
    private int EditingCol { get; set; } = -1;

    /// <summary>
    ///     <para>
    ///         Whether the save modal is currently open.
    ///     </para>
    /// </summary>
    private bool SaveModalOpen { get; set; }

    /// <summary>
    ///     <para>
    ///         Whether the load modal is currently open.
    ///     </para>
    /// </summary>
    private bool LoadModalOpen { get; set; }

    /// <summary>
    ///     <para>
    ///         Whether the demo modal is currently open.
    ///     </para>
    /// </summary>
    private bool DemoModalOpen { get; set; }

    /// <summary>
    ///     <para>
    ///         Whether the about modal is currently open.
    ///     </para>
    /// </summary>
    private bool AboutModalOpen { get; set; }

    /// <summary>
    ///     <para>
    ///         Whether the find bar is visible.
    ///     </para>
    /// </summary>
    private bool FindVisible { get; set; }

    /// <summary>
    ///     <para>
    ///         Whether a cell is currently in edit mode.
    ///     </para>
    /// </summary>
    private bool Editing { get; set; }

    /// <summary>
    ///     <para>
    ///         Whether the current document is marked saved.
    ///     </para>
    /// </summary>
    private bool IsSaved { get; set; } = true;

    /// <summary>
    ///     <para>
    ///         Whether toast notification UI is currently visible.
    ///     </para>
    /// </summary>
    private bool NotificationVisible { get; set; }

    /// <summary>
    ///     <para>
    ///         Current zoom factor for grid rendering.
    ///     </para>
    /// </summary>
    private double Zoom { get; set; } = 1;

    /// <summary>
    ///     <para>
    ///         Active document display name.
    ///     </para>
    /// </summary>
    private string DocumentName { get; set; } = DefaultDocumentName;

    /// <summary>
    ///     <para>
    ///         Pending save name entered in the save modal.
    ///     </para>
    /// </summary>
    private string SaveName { get; set; } = DefaultDocumentName;

    /// <summary>
    ///     <para>
    ///         Current formula-bar text.
    ///     </para>
    /// </summary>
    private string FormulaInput { get; set; } = string.Empty;

    /// <summary>
    ///     <para>
    ///         Temporary edit text for in-grid editing.
    ///     </para>
    /// </summary>
    private string EditingValue { get; set; } = string.Empty;

    /// <summary>
    ///     <para>
    ///         Current query text used by find.
    ///     </para>
    /// </summary>
    private string FindQuery { get; set; } = string.Empty;

    /// <summary>
    ///     <para>
    ///         Name of currently open top-level menu, if any.
    ///     </para>
    /// </summary>
    private string? OpenMenuName { get; set; }

    /// <summary>
    ///     <para>
    ///         Current notification message content.
    ///     </para>
    /// </summary>
    private string NotificationMessage { get; set; } = string.Empty;

    /// <summary>
    ///     <para>
    ///         Currently selected built-in sample document for demo loading.
    ///     </para>
    /// </summary>
    private string? SelectedDemoDocumentName { get; set; }

    /// <summary>
    ///     <para>
    ///         Local-storage document currently selected in load modal.
    ///     </para>
    /// </summary>
    private string? SelectedLocalDocumentName { get; set; }

    /// <summary>
    ///     <para>
    ///         Render key for the load modal file input, used to clear selected file after import.
    ///     </para>
    /// </summary>
    private int LoadUploadInputKey { get; set; }

    /// <summary>
    ///     <para>
    ///         Selected save destination for save modal actions.
    ///     </para>
    /// </summary>
    private SaveDestination SelectedSaveDestination { get; set; } = SaveDestination.LocalStorage;

    /// <summary>
    ///     <para>
    ///         Timestamp for the most recent successful save operation.
    ///     </para>
    /// </summary>
    private DateTimeOffset? LastSavedAt { get; set; }

    /// <summary>
    ///     <para>
    ///         A1-style reference for the currently selected cell.
    ///     </para>
    /// </summary>
    private string SelectedCellRef => $"{ColumnLabel(SelectedCol)}{SelectedRow + 1}";

    /// <summary>
    ///     <para>
    ///         Display value for the selected cell used by the status bar.
    ///     </para>
    /// </summary>
    private string SelectedCellDisplay => string.IsNullOrWhiteSpace(CellValue(SelectedRow, SelectedCol)) ? "-" : CellValue(SelectedRow, SelectedCol);

    /// <summary>
    ///     <para>
    ///         Human-readable zoom percentage label.
    ///     </para>
    /// </summary>
    private string ZoomPercentText => $"{(int)Math.Round(Zoom * 100)}%";

    /// <summary>
    ///     <para>
    ///         Inline transform style applied to the grid for zooming.
    ///     </para>
    /// </summary>
    private string ZoomStyle => $"transform: scale({Zoom.ToString("0.0", CultureInfo.InvariantCulture)}); transform-origin: top left;";

    /// <summary>
    ///     <para>
    ///         Available built-in sample documents shown in open flows.
    ///     </para>
    /// </summary>
    private IReadOnlyList<SampleDocument> SampleDocuments => _sampleDocuments;

    /// <summary>
    ///     <para>
    ///         Sample-document row data shown in the demo modal selector.
    ///     </para>
    /// </summary>
    private IReadOnlyList<FileRowOption> SampleDocumentRows => _sampleDocuments
        .Select(sample => new FileRowOption(sample.Name, sample.DateText))
        .ToList();

    /// <summary>
    ///     <para>
    ///         Local documents discovered from browser local storage.
    ///     </para>
    /// </summary>
    private IReadOnlyList<LocalDocument> LocalDocuments => _localDocuments;

    /// <summary>
    ///     <para>
    ///         Current find-match counter text for the find toolbar.
    ///     </para>
    /// </summary>
    private string FindCountText => string.IsNullOrWhiteSpace(FindQuery)
        ? string.Empty
        : _findMatches.Count > 0
            ? $"{_findMatches.Count} found"
            : "No matches";

    /// <summary>
    ///     <para>
    ///         Relative last-saved label shown in the status bar.
    ///     </para>
    /// </summary>
    private string LastSavedLabel
    {
        get
        {
            if (LastSavedAt is null)
            {
                return "Not saved yet";
            }

            var elapsed = DateTimeOffset.UtcNow - LastSavedAt.Value;
            var seconds = (int)elapsed.TotalSeconds;
            if (seconds < 10)
            {
                return "Last saved just now";
            }

            if (seconds < 60)
            {
                return $"Last saved {seconds}s ago";
            }

            var minutes = (int)elapsed.TotalMinutes;
            return $"Last saved {minutes} min ago";
        }
    }

    /// <summary>
    ///     <para>
    ///         Initializes page state with a new empty document.
    ///     </para>
    /// </summary>
    protected override void OnInitialized()
    {
        _sheet = new SpreadsheetDoc();
        DocumentName = DefaultDocumentName;
        SaveName = DefaultDocumentName;
        ResetViewportState();
        IsSaved = true;
        LastSavedAt = null;
    }

    /// <summary>
    ///     <para>
    ///         Performs first-render JS initialization and deferred edit-input focus.
    ///     </para>
    /// </summary>
    /// <param name="firstRender">Whether this is the first render pass.</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./spreadsheetGrid.js");
            await _jsModule.InvokeVoidAsync("initialize", _dotNetRef);
            await _jsModule.InvokeVoidAsync("autoResizeAiInput");
            await RefreshLocalDocumentsAsync();
        }

        if (_needsFocusEditInput && _jsModule is not null)
        {
            _needsFocusEditInput = false;
            await _jsModule.InvokeVoidAsync("focusCellInput");
        }
    }

    /// <summary>
    ///     <para>
    ///         Releases JS interop resources used by the page.
    ///     </para>
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("cleanup");
            }
            catch
            {
                // Ignore cleanup failures during teardown.
            }

            try
            {
                await _jsModule.DisposeAsync();
            }
            catch
            {
                // Ignore JS disconnect/object disposal races during teardown.
            }
        }

        _dotNetRef?.Dispose();
    }

    /// <summary>
    ///     <para>
    ///         Called from the document-level JS keydown listener in <c>spreadsheetGrid.js</c>.
    ///     </para>
    /// </summary>
    /// <param name="key">The pressed key identifier.</param>
    /// <param name="ctrl">Whether Ctrl/Meta is pressed.</param>
    /// <param name="shift">Whether Shift is pressed.</param>
    /// <param name="alt">Whether Alt is pressed.</param>
    [JSInvokable]
    public Task HandleKeyFromJs(string key, bool ctrl, bool shift, bool alt)
    {
        return InvokeAsync(() =>
        {
            HandleGlobalKeyDown(new KeyboardEventArgs
            {
                Key = key,
                CtrlKey = ctrl,
                MetaKey = ctrl,
                ShiftKey = shift,
                AltKey = alt
            });
            StateHasChanged();
        });
    }

    /// <summary>
    ///     <para>
    ///         Called from JS to close open top-level menus when clicking outside.
    ///     </para>
    /// </summary>
    [JSInvokable]
    public Task CloseMenusFromJs()
    {
        return InvokeAsync(() =>
        {
            CloseMenus();
            StateHasChanged();
        });
    }

    /// <summary>
    ///     <para>
    ///         Produces a stable key for row/column pairs used by match tracking.
    ///     </para>
    /// </summary>
    private static string CellKey(int row, int col) => $"{row},{col}";

    /// <summary>
    ///     <para>
    ///         Converts a zero-based column index to its spreadsheet letter label.
    ///     </para>
    /// </summary>
    private static string ColumnLabel(int col) => ((char)('A' + col)).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    ///     <para>
    ///         Converts zero-based row/column coordinates to A1 notation.
    ///     </para>
    /// </summary>
    private static string ToSheetName(int row, int col) => $"{(char)('A' + col)}{row + 1}";

    // Returns the raw input string for a cell (formula text for formula cells, else the value text).
    // Used to pre-populate the edit input so the user sees/edits the formula, not the computed value.
    private string CellRawInput(int row, int col)
    {
        try
        {
            var contents = _sheet.GetCellContents(ToSheetName(row, col));
            return contents switch
            {
                null => string.Empty,
                string s => s,
                double d => d.ToString(CultureInfo.InvariantCulture),
                _ => "=" + contents.ToString()  // Formula
            };
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    ///     <para>
    ///         Returns the display value for a grid cell from the backend sheet.
    ///     </para>
    /// </summary>
    private string CellValue(int row, int col)
    {
        try
        {
            var value = _sheet.GetCellValue(ToSheetName(row, col));
            return value switch
            {
                string s => s,
                double d => d.ToString("#,##0.########", CultureInfo.CurrentCulture),
                FormulaError fe => fe.Reason,
                _ => string.Empty
            };
        }
        catch (Exception ex)
        {
            return string.IsNullOrWhiteSpace(ex.Message) ? "#ERROR" : $"#ERROR: {ex.Message}";
        }
    }

    /// <summary>
    ///     <para>
    ///         Determines whether the given coordinates are the currently edited cell.
    ///     </para>
    /// </summary>
    private bool IsEditingCell(int row, int col) => Editing && row == EditingRow && col == EditingCol;

    /// <summary>
    ///     <para>
    ///         Determines whether the given cell coordinates are part of current find results.
    ///     </para>
    /// </summary>
    private bool IsFindMatch(int row, int col) => _findMatches.Contains(CellKey(row, col));

    /// <summary>
    ///     <para>
    ///         Selects a cell and synchronizes formula input with that selection.
    ///     </para>
    /// </summary>
    private void SelectCell(int row, int col)
    {
        if (Editing)
        {
            CommitEdit();
        }

        SelectedRow = Math.Clamp(row, 0, Rows - 1);
        SelectedCol = Math.Clamp(col, 0, Cols - 1);
        SyncFormulaInputWithSelection();
    }

    /// <summary>
    ///     <para>
    ///         Enters in-cell edit mode for the specified coordinates.
    ///     </para>
    /// </summary>
    private void StartEdit(int row, int col)
    {
        SelectCell(row, col);
        Editing = true;
        EditingRow = row;
        EditingCol = col;
        EditingValue = CellRawInput(row, col);
        _needsFocusEditInput = true;
    }

    /// <summary>
    ///     <para>
    ///         Starts editing the selected cell prefilled with an initial typed character.
    ///     </para>
    /// </summary>
    private void StartEditWithCharacter(string character)
    {
        Editing = true;
        EditingRow = SelectedRow;
        EditingCol = SelectedCol;
        EditingValue = character;
        _needsFocusEditInput = true;
    }

    /// <summary>
    ///     <para>
    ///         Updates temporary edit text while editing a cell.
    ///     </para>
    /// </summary>
    private void OnEditChanged(ChangeEventArgs args)
    {
        EditingValue = args.Value?.ToString() ?? string.Empty;
    }

    /// <summary>
    ///     <para>
    ///         Commits current edit text to the selected cell.
    ///     </para>
    /// </summary>
    private void CommitEdit()
    {
        if (!Editing)
        {
            return;
        }

        SetCell(EditingRow, EditingCol, EditingValue);
        Editing = false;
        EditingRow = -1;
        EditingCol = -1;
        EditingValue = string.Empty;
        SyncFormulaInputWithSelection();
    }

    /// <summary>
    ///     <para>
    ///         Exits edit mode without applying pending edits.
    ///     </para>
    /// </summary>
    private void CancelEdit()
    {
        Editing = false;
        EditingRow = -1;
        EditingCol = -1;
        EditingValue = string.Empty;
    }

    /// <summary>
    ///     <para>
    ///         Writes a value to a cell and updates dirty/find state.
    ///     </para>
    /// </summary>
    private void SetCell(int row, int col, string? value)
    {
        try
        {
            _sheet.SetContentsOfCell(ToSheetName(row, col), value ?? string.Empty);
        }
        catch (CircularException)
        {
            Notify("Circular dependency detected");
            return;
        }
        catch (FormulaFormatException ex)
        {
            Notify(string.IsNullOrWhiteSpace(ex.Message) ? "Invalid formula" : ex.Message);
            return;
        }
        catch (Exception ex)
        {
            Notify(string.IsNullOrWhiteSpace(ex.Message) ? "Invalid formula" : ex.Message);
            return;
        }

        MarkUnsaved();
        if (!string.IsNullOrWhiteSpace(FindQuery))
        {
            RefreshFindMatches();
        }
    }

    /// <summary>
    ///     <para>
    ///         Clears the currently selected cell.
    ///     </para>
    /// </summary>
    private void ClearSelectedCell()
    {
        CancelEdit();
        SetCell(SelectedRow, SelectedCol, string.Empty);
        SyncFormulaInputWithSelection();
        Notify("Cell cleared");
    }

    /// <summary>
    ///     <para>
    ///         Moves the active selection by the provided row/column offsets.
    ///     </para>
    /// </summary>
    private void MoveSelection(int rowOffset, int colOffset)
    {
        SelectCell(
            Math.Clamp(SelectedRow + rowOffset, 0, Rows - 1),
            Math.Clamp(SelectedCol + colOffset, 0, Cols - 1));
    }

    /// <summary>
    ///     <para>
    ///         Updates formula bar text from user input.
    ///     </para>
    /// </summary>
    private void OnFormulaChanged(ChangeEventArgs args)
    {
        FormulaInput = args.Value?.ToString() ?? string.Empty;
    }

    /// <summary>
    ///     <para>
    ///         Commits formula bar text when Enter is pressed.
    ///     </para>
    /// </summary>
    private void HandleFormulaKeyDown(KeyboardEventArgs args)
    {
        if (args.Key != "Enter")
        {
            return;
        }

        SetCell(SelectedRow, SelectedCol, FormulaInput);
        SyncFormulaInputWithSelection();
    }

    /// <summary>
    ///     <para>
    ///         Refreshes formula bar text from the currently selected backend cell contents.
    ///     </para>
    /// </summary>
    private void SyncFormulaInputWithSelection()
    {
        try
        {
            var contents = _sheet.GetCellContents(ToSheetName(SelectedRow, SelectedCol));
            FormulaInput = contents switch
            {
                null => string.Empty,
                string s => s,
                double d => d.ToString(CultureInfo.InvariantCulture),
                _ => "=" + contents.ToString()  // Formula objects
            };
        }
        catch
        {
            FormulaInput = string.Empty;
        }
    }

    /// <summary>
    ///     <para>
    ///         Synchronizes selection/edit/find/saved UI state with current spreadsheet data.
    ///     </para>
    /// </summary>
    private void SyncUIWithSpreadsheet()
    {
        CancelEdit();

        SelectedRow = Math.Clamp(SelectedRow, 0, Rows - 1);
        SelectedCol = Math.Clamp(SelectedCol, 0, Cols - 1);

        SyncFormulaInputWithSelection();

        if (!string.IsNullOrWhiteSpace(FindQuery))
        {
            RefreshFindMatches();
        }
        else
        {
            _findMatches.Clear();
        }

        IsSaved = !_sheet.Changed;
        if (IsSaved)
        {
            LastSavedAt ??= DateTimeOffset.UtcNow;
        }
        else
        {
            LastSavedAt = null;
        }
    }

    /// <summary>
    ///     <para>
    ///         Handles keyboard behavior while in in-cell edit mode.
    ///     </para>
    /// </summary>
    private void HandleEditKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            CommitEdit();
            MoveSelection(1, 0);
        }
        else if (args.Key == "Tab")
        {
            CommitEdit();
            MoveSelection(0, args.ShiftKey ? -1 : 1);
        }
        else if (args.Key == "Escape")
        {
            CancelEdit();
        }
    }

    /// <summary>
    ///     <para>
    ///         Handles page-level keyboard shortcuts and navigation when not editing.
    ///     </para>
    /// </summary>
    private void HandleGlobalKeyDown(KeyboardEventArgs args)
    {
        if ((args.CtrlKey || args.MetaKey) && args.Key.Equals("f", StringComparison.OrdinalIgnoreCase))
        {
            Notify("This feature isn't implemented yet.");
            return;
        }

        if ((args.CtrlKey || args.MetaKey) && args.Key.Equals("s", StringComparison.OrdinalIgnoreCase))
        {
            OpenModal(ModalKind.Save);
            return;
        }

        if ((args.CtrlKey || args.MetaKey) && (args.Key == "+" || args.Key == "="))
        {
            ZoomIn();
            return;
        }

        if ((args.CtrlKey || args.MetaKey) && args.Key == "-")
        {
            ZoomOut();
            return;
        }

        if ((args.CtrlKey || args.MetaKey) && args.Key == "0")
        {
            ResetZoom();
            return;
        }

        if (Editing)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowUp":
                MoveSelection(-1, 0);
                break;
            case "ArrowDown":
                MoveSelection(1, 0);
                break;
            case "ArrowLeft":
                MoveSelection(0, -1);
                break;
            case "ArrowRight":
                MoveSelection(0, 1);
                break;
            case "Enter":
                StartEdit(SelectedRow, SelectedCol);
                break;
            case "Tab":
                MoveSelection(0, args.ShiftKey ? -1 : 1);
                break;
            case "Delete":
            case "Backspace":
                ClearSelectedCell();
                break;
            default:
                if (!args.CtrlKey && !args.MetaKey && !args.AltKey && args.Key.Length == 1)
                {
                    StartEditWithCharacter(args.Key);
                }

                break;
        }
    }

    /// <summary>
    ///     <para>
    ///         Increases sheet zoom level.
    ///     </para>
    /// </summary>
    private void ZoomIn() => Zoom = Math.Min(2, Math.Round(Zoom + 0.1, 1));

    /// <summary>
    ///     <para>
    ///         Decreases sheet zoom level.
    ///     </para>
    /// </summary>
    private void ZoomOut() => Zoom = Math.Max(0.5, Math.Round(Zoom - 0.1, 1));

    /// <summary>
    ///     <para>
    ///         Resets sheet zoom to 100%.
    ///     </para>
    /// </summary>
    private void ResetZoom() => Zoom = 1;

    /// <summary>
    ///     <para>
    ///         Opens or closes a top-level menu by name.
    ///     </para>
    /// </summary>
    private void ToggleMenu(string menuName)
    {
        OpenMenuName = OpenMenuName == menuName ? null : menuName;
    }

    /// <summary>
    ///     <para>
    ///         Closes all top-level menus.
    ///     </para>
    /// </summary>
    private void CloseMenus()
    {
        OpenMenuName = null;
    }

    /// <summary>
    ///     <para>
    ///         Hides the find UI and clears query/match state.
    ///     </para>
    /// </summary>
    private void HideFind()
    {
        FindVisible = false;
        FindQuery = string.Empty;
        _findMatches.Clear();
    }

    /// <summary>
    ///     <para>
    ///         Updates find query text and recomputes cell matches.
    ///     </para>
    /// </summary>
    private void OnFindInput(ChangeEventArgs args)
    {
        FindQuery = args.Value?.ToString() ?? string.Empty;
        RefreshFindMatches();
    }

    /// <summary>
    ///     <para>
    ///         Rebuilds the set of matched cells for the active find query.
    ///     </para>
    /// </summary>
    private void RefreshFindMatches()
    {
        _findMatches.Clear();
        if (string.IsNullOrWhiteSpace(FindQuery))
        {
            return;
        }

        foreach (var row in Enumerable.Range(0, Rows))
        {
            foreach (var col in Enumerable.Range(0, Cols))
            {
                var value = CellValue(row, col);
                if (value.Contains(FindQuery, StringComparison.OrdinalIgnoreCase))
                {
                    _findMatches.Add(CellKey(row, col));
                }
            }
        }
    }

    /// <summary>
    ///     <para>
    ///         Opens one modal and closes the others.
    ///     </para>
    /// </summary>
    private void OpenModal(ModalKind kind)
    {
        CloseMenus();
        SaveModalOpen = kind == ModalKind.Save;
        LoadModalOpen = kind == ModalKind.Load;
        DemoModalOpen = kind == ModalKind.Demo;
        AboutModalOpen = kind == ModalKind.About;
        if (kind == ModalKind.Save)
        {
            SaveName = DocumentName;
        }

        if (kind == ModalKind.Load)
        {
            _ = RefreshLocalDocumentsAsync();
        }

        if (kind == ModalKind.Demo)
        {
            SelectedDemoDocumentName ??= _sampleDocuments.FirstOrDefault()?.Name;
        }
    }

    /// <summary>
    ///     <para>
    ///         Closes the requested modal.
    ///     </para>
    /// </summary>
    private void CloseModal(ModalKind kind)
    {
        if (kind == ModalKind.Save)
        {
            SaveModalOpen = false;
        }
        else if (kind == ModalKind.Load)
        {
            LoadModalOpen = false;
        }
        else if (kind == ModalKind.Demo)
        {
            DemoModalOpen = false;
        }
        else
        {
            AboutModalOpen = false;
        }
    }

    /// <summary>
    ///     <para>
    ///         Persists the current sheet to local storage or downloads it as JSON.
    ///     </para>
    /// </summary>
    private async Task ConfirmSaveAsync()
    {
        DocumentName = string.IsNullOrWhiteSpace(SaveName) ? "Untitled" : SaveName.Trim();
        SaveName = DocumentName;

        try
        {
            var json = ExportSheetJson();

            if (_jsModule is not null)
            {
                if (SelectedSaveDestination == SaveDestination.LocalStorage)
                {
                    await _jsModule.InvokeVoidAsync("saveSheetToLocal", SaveName, json);
                    await RefreshLocalDocumentsAsync();
                    Notify($"Saved '{SaveName}' to local storage");
                }
                else
                {
                    var fileName = EnsureJsonFileName(SaveName);
                    await _jsModule.InvokeVoidAsync("downloadSheetFile", fileName, json);
                    Notify($"Downloaded '{fileName}'");
                }
            }
            else
            {
                Notify("Save is unavailable right now.");
                return;
            }
        }
        catch (Exception ex)
        {
            Notify($"Failed to save: {ex.Message}");
            return;
        }

        SaveModalOpen = false;
        _sheet.MarkAsSaved();
        MarkSaved();
    }

    /// <summary>
    ///     <para>
    ///         Selects which sample document should be loaded from the demo modal.
    ///     </para>
    /// </summary>
    private void SelectDemoDocument(string documentName)
    {
        SelectedDemoDocumentName = documentName;
    }

    /// <summary>
    ///     <para>
    ///         Loads the currently selected sample document from the demo modal.
    ///     </para>
    /// </summary>
    private void ConfirmLoadDemo()
    {
        if (string.IsNullOrWhiteSpace(SelectedDemoDocumentName))
        {
            Notify("Select a demo document first.");
            return;
        }

        var selectedSample = _sampleDocuments.FirstOrDefault(sample => sample.Name == SelectedDemoDocumentName);
        if (selectedSample is null)
        {
            Notify("Could not load that demo document.");
            return;
        }

        LoadTemplate(selectedSample.TemplateKey, selectedSample.Name, markSaved: true);
        DemoModalOpen = false;
        Notify($"Loaded demo '{selectedSample.Name}'");
    }

    /// <summary>
    ///     <para>
    ///         Opens the selected local-storage document into the active sheet.
    ///     </para>
    /// </summary>
    private async Task ConfirmOpenAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedLocalDocumentName))
        {
            Notify("Select a local document first, or use file upload.");
            return;
        }

        try
        {
            if (_jsModule is null)
            {
                Notify("Open is unavailable right now.");
                return;
            }

            var json = await _jsModule.InvokeAsync<string?>("loadSheetFromLocal", SelectedLocalDocumentName);
            if (string.IsNullOrWhiteSpace(json))
            {
                Notify("Could not read that local document.");
                return;
            }

            var loadedName = SelectedLocalDocumentName;
            LoadSpreadsheetFromJson(json, loadedName);
            Notify($"Loaded '{loadedName}'");
            LoadModalOpen = false;
        }
        catch (Exception ex)
        {
            Notify($"Failed to open document: {ex.Message}");
        }
    }

    /// <summary>
    ///     <para>
    ///         Marks a local document as selected in the load modal.
    ///     </para>
    /// </summary>
    private void SelectLocalDocument(string documentName)
    {
        SelectedLocalDocumentName = documentName;
    }

    /// <summary>
    ///     <para>
    ///         Deletes a local-storage document and refreshes the modal list.
    ///     </para>
    /// </summary>
    private async Task DeleteLocalDocumentAsync(string documentName)
    {
        if (_jsModule is null)
        {
            return;
        }

        await _jsModule.InvokeVoidAsync("deleteSheetFromLocal", documentName);
        if (SelectedLocalDocumentName == documentName)
        {
            SelectedLocalDocumentName = null;
        }

        await RefreshLocalDocumentsAsync();
        Notify($"Deleted local copy of {documentName}");
    }

    /// <summary>
    ///     <para>
    ///         Imports a spreadsheet JSON file selected from the browser file picker.
    ///     </para>
    /// </summary>
    private async Task OnUploadSpreadsheetFileAsync(InputFileChangeEventArgs args)
    {
        var file = args.File;
        if (file is null)
        {
            return;
        }

        try
        {
            using var stream = file.OpenReadStream(MaxUploadBytes);
            using var reader = new StreamReader(stream, Encoding.UTF8, true, MaxFileReadBytes, leaveOpen: false);
            var json = await reader.ReadToEndAsync();

            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
            var loadedName = string.IsNullOrWhiteSpace(fileNameWithoutExt) ? "Imported Sheet" : fileNameWithoutExt;

            LoadSpreadsheetFromJson(json, loadedName);
            LoadUploadInputKey++;
            LoadModalOpen = false;
            Notify($"Loaded '{loadedName}' from file");
        }
        catch (Exception ex)
        {
            Notify($"Failed to load file: {ex.Message}");
        }
    }

    /// <summary>
    ///     <para>
    ///         Handles changes to the visible document name.
    ///     </para>
    /// </summary>
    private void OnDocumentNameChanged(ChangeEventArgs args)
    {
        DocumentName = args.Value?.ToString() ?? string.Empty;
        SaveName = DocumentName;
        MarkUnsaved();
    }

    /// <summary>
    ///     <para>
    ///         Handles save-name input changes in the save modal.
    ///     </para>
    /// </summary>
    private void OnSaveNameChanged(ChangeEventArgs args)
    {
        SaveName = args.Value?.ToString() ?? string.Empty;
    }

    /// <summary>
    ///     <para>
    ///         Creates a brand-new empty sheet and resets viewport state.
    ///     </para>
    /// </summary>
    private void NewDocument()
    {
        _sheet = new SpreadsheetDoc();
        DocumentName = "Untitled";
        SaveName = DocumentName;
        SelectedDemoDocumentName = null;
        ResetViewportState();
        MarkUnsaved();
        Notify("New document");
    }

    /// <summary>
    ///     <para>
    ///         Marks the current document as having unsaved changes.
    ///     </para>
    /// </summary>
    private void MarkUnsaved()
    {
        IsSaved = false;
    }

    /// <summary>
    ///     <para>
    ///         Marks the current document as saved and updates save timestamp.
    ///     </para>
    /// </summary>
    private void MarkSaved()
    {
        IsSaved = true;
        LastSavedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     <para>
    ///         Serializes the current sheet to JSON text.
    ///     </para>
    /// </summary>
    private string ExportSheetJson()
    {
        return _sheet.GetJsonString();
    }

    /// <summary>
    ///     <para>
    ///         Loads a sheet from JSON text and applies document-level UI state updates.
    ///     </para>
    /// </summary>
    private void LoadSpreadsheetFromJson(string json, string documentName)
    {
        _sheet.LoadFromJson(json);
        DocumentName = documentName;
        SaveName = documentName;
        ResetViewportState();
        MarkSaved();
        SyncUIWithSpreadsheet();
    }

    /// <summary>
    ///     <para>
    ///         Ensures a file name ends with the <c>.json</c> extension.
    ///     </para>
    /// </summary>
    private static string EnsureJsonFileName(string name)
    {
        var normalized = string.IsNullOrWhiteSpace(name) ? "spreadsheet" : name.Trim();
        return normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : normalized + ".json";
    }

    /// <summary>
    ///     <para>
    ///         Refreshes available local-storage documents for the load modal.
    ///     </para>
    /// </summary>
    private async Task RefreshLocalDocumentsAsync()
    {
        if (_jsModule is null)
        {
            return;
        }

        List<LocalDocument> docs;
        try
        {
            docs = await _jsModule.InvokeAsync<List<LocalDocument>>("listSheetsFromLocal");
        }
        catch (JSException)
        {
            // If the browser still has a stale JS module cached, don't crash the circuit.
            _localDocuments.Clear();
            SelectedLocalDocumentName = null;
            return;
        }

        _localDocuments.Clear();
        _localDocuments.AddRange(docs.OrderByDescending(d => d.SavedAt));

        if (string.IsNullOrWhiteSpace(SelectedLocalDocumentName) || !_localDocuments.Any(d => d.Name == SelectedLocalDocumentName))
        {
            SelectedLocalDocumentName = _localDocuments.FirstOrDefault()?.Name;
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     <para>
    ///         Displays a transient toast-style notification message.
    ///     </para>
    /// </summary>
    private async void Notify(string message)
    {
        _notificationCts?.Cancel();
        _notificationCts?.Dispose();
        _notificationCts = new CancellationTokenSource();

        NotificationMessage = message;
        NotificationVisible = true;
        StateHasChanged();

        try
        {
            await Task.Delay(2500, _notificationCts.Token);
            NotificationVisible = false;
            StateHasChanged();
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellation from rapid notify calls.
        }
    }

    /// <summary>
    ///     <para>
    ///         Loads one of the built-in sample templates into a fresh sheet.
    ///     </para>
    /// </summary>
    private void LoadTemplate(SampleTemplateKey templateKey, string documentName, bool markSaved)
    {
        var nextSheet = new SpreadsheetDoc();
        PopulateTemplate(nextSheet, templateKey);

        _sheet = nextSheet;
        DocumentName = documentName;
        SaveName = documentName;
        ResetViewportState();

        if (markSaved)
        {
            MarkSaved();
        }
        else
        {
            MarkUnsaved();
        }
    }

    /// <summary>
    ///     <para>
    ///         Resets selection, find state, edit state, and menus to default viewport values.
    ///     </para>
    /// </summary>
    private void ResetViewportState()
    {
        _findMatches.Clear();
        FindQuery = string.Empty;
        FindVisible = false;
        CancelEdit();
        CloseMenus();
        SelectedRow = 0;
        SelectedCol = 0;
        SyncFormulaInputWithSelection();
    }

    /// <summary>
    ///     <para>
    ///         Dispatches template population based on the selected template key.
    ///     </para>
    /// </summary>
    private void PopulateTemplate(SpreadsheetDoc sheet, SampleTemplateKey templateKey)
    {
        switch (templateKey)
        {
            case SampleTemplateKey.Q3VarianceReport:
                PopulateQ3VarianceReport(sheet);
                break;
            case SampleTemplateKey.HeadcountModel:
                PopulateHeadcountModel(sheet);
                break;
            case SampleTemplateKey.BudgetConsolidation:
                PopulateBudgetConsolidation(sheet);
                break;
            case SampleTemplateKey.ActualsVsPlan:
                PopulateActualsVsPlan(sheet);
                break;
        }
    }

    /// <summary>
    ///     <para>
    ///         Populates the Q3 variance report sample.
    ///     </para>
    /// </summary>
    private void PopulateQ3VarianceReport(SpreadsheetDoc sheet)
    {
        SetCellNoDirty(sheet, 0, 0, "MONTH");
        SetCellNoDirty(sheet, 0, 1, "REVENUE");
        SetCellNoDirty(sheet, 0, 2, "BUDGET");
        SetCellNoDirty(sheet, 0, 3, "VARIANCE");
        SetCellNoDirty(sheet, 0, 4, "VAR %");

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
        var revenue = new[] { "128400", "141200", "133800", "156700", "149000", "172300" };
        var budget = new[] { "130000", "135000", "140000", "150000", "155000", "160000" };

        for (var i = 0; i < months.Length; i++)
        {
            var row = i + 1;
            SetCellNoDirty(sheet, row, 0, months[i]);
            SetCellNoDirty(sheet, row, 1, revenue[i]);
            SetCellNoDirty(sheet, row, 2, budget[i]);
            SetCellNoDirty(sheet, row, 3, $"=B{row + 1}-C{row + 1}");
            SetCellNoDirty(sheet, row, 4, $"=D{row + 1}/C{row + 1}*100");
        }

        SetCellNoDirty(sheet, 7, 0, "TOTAL");
        SetCellNoDirty(sheet, 7, 1, "=B2+B3+B4+B5+B6+B7");
        SetCellNoDirty(sheet, 7, 2, "=C2+C3+C4+C5+C6+C7");
        SetCellNoDirty(sheet, 7, 3, "=D2+D3+D4+D5+D6+D7");
        SetCellNoDirty(sheet, 7, 4, "=D8/C8*100");

        SetCellNoDirty(sheet, 9, 0, "AVERAGE");
        SetCellNoDirty(sheet, 9, 1, "=(B2+B3+B4+B5+B6+B7)/6");
        SetCellNoDirty(sheet, 9, 2, "=(C2+C3+C4+C5+C6+C7)/6");
    }

    /// <summary>
    ///     <para>
    ///         Populates the headcount model sample.
    ///     </para>
    /// </summary>
    private void PopulateHeadcountModel(SpreadsheetDoc sheet)
    {
        SetCellNoDirty(sheet, 0, 0, "ROLE");
        SetCellNoDirty(sheet, 0, 1, "HEADCOUNT");
        SetCellNoDirty(sheet, 0, 2, "AVG COST");
        SetCellNoDirty(sheet, 0, 3, "ANNUAL COST");

        var roles = new[] { "Engineering", "Product", "Design", "Sales", "Support" };
        var headcount = new[] { "18", "6", "4", "10", "7" };
        var avgCost = new[] { "142000", "136000", "128000", "118000", "91000" };

        for (var i = 0; i < roles.Length; i++)
        {
            var row = i + 1;
            SetCellNoDirty(sheet, row, 0, roles[i]);
            SetCellNoDirty(sheet, row, 1, headcount[i]);
            SetCellNoDirty(sheet, row, 2, avgCost[i]);
            SetCellNoDirty(sheet, row, 3, $"=B{row + 1}*C{row + 1}");
        }

        SetCellNoDirty(sheet, 7, 0, "TOTAL");
        SetCellNoDirty(sheet, 7, 1, "=B2+B3+B4+B5+B6");
        SetCellNoDirty(sheet, 7, 3, "=D2+D3+D4+D5+D6");
    }

    /// <summary>
    ///     <para>
    ///         Populates the budget consolidation sample.
    ///     </para>
    /// </summary>
    private void PopulateBudgetConsolidation(SpreadsheetDoc sheet)
    {
        SetCellNoDirty(sheet, 0, 0, "TEAM");
        SetCellNoDirty(sheet, 0, 1, "Q1");
        SetCellNoDirty(sheet, 0, 2, "Q2");
        SetCellNoDirty(sheet, 0, 3, "Q3");
        SetCellNoDirty(sheet, 0, 4, "Q4");
        SetCellNoDirty(sheet, 0, 5, "FY TOTAL");

        var teams = new[] { "Platform", "Growth", "Data", "Infrastructure" };
        var q1 = new[] { "820000", "540000", "410000", "630000" };
        var q2 = new[] { "845000", "590000", "435000", "655000" };
        var q3 = new[] { "870000", "620000", "460000", "670000" };
        var q4 = new[] { "900000", "640000", "480000", "695000" };

        for (var i = 0; i < teams.Length; i++)
        {
            var row = i + 1;
            SetCellNoDirty(sheet, row, 0, teams[i]);
            SetCellNoDirty(sheet, row, 1, q1[i]);
            SetCellNoDirty(sheet, row, 2, q2[i]);
            SetCellNoDirty(sheet, row, 3, q3[i]);
            SetCellNoDirty(sheet, row, 4, q4[i]);
            SetCellNoDirty(sheet, row, 5, $"=B{row + 1}+C{row + 1}+D{row + 1}+E{row + 1}");
        }

        SetCellNoDirty(sheet, 6, 0, "TOTAL");
        SetCellNoDirty(sheet, 6, 1, "=B2+B3+B4+B5");
        SetCellNoDirty(sheet, 6, 2, "=C2+C3+C4+C5");
        SetCellNoDirty(sheet, 6, 3, "=D2+D3+D4+D5");
        SetCellNoDirty(sheet, 6, 4, "=E2+E3+E4+E5");
        SetCellNoDirty(sheet, 6, 5, "=F2+F3+F4+F5");
    }

    /// <summary>
    ///     <para>
    ///         Populates the actuals-vs-plan sample.
    ///     </para>
    /// </summary>
    private void PopulateActualsVsPlan(SpreadsheetDoc sheet)
    {
        SetCellNoDirty(sheet, 0, 0, "METRIC");
        SetCellNoDirty(sheet, 0, 1, "PLAN");
        SetCellNoDirty(sheet, 0, 2, "ACTUAL");
        SetCellNoDirty(sheet, 0, 3, "DELTA");
        SetCellNoDirty(sheet, 0, 4, "DELTA %");

        var metrics = new[] { "Pipeline", "Bookings", "Renewals", "Expansion", "Churn" };
        var plan = new[] { "2500000", "1800000", "920000", "410000", "150000" };
        var actual = new[] { "2640000", "1735000", "948000", "436000", "131000" };

        for (var i = 0; i < metrics.Length; i++)
        {
            var row = i + 1;
            SetCellNoDirty(sheet, row, 0, metrics[i]);
            SetCellNoDirty(sheet, row, 1, plan[i]);
            SetCellNoDirty(sheet, row, 2, actual[i]);
            SetCellNoDirty(sheet, row, 3, $"=C{row + 1}-B{row + 1}");
            SetCellNoDirty(sheet, row, 4, $"=D{row + 1}/B{row + 1}*100");
        }
    }

    /// <summary>
    ///     <para>
    ///         Writes a sample cell value while suppressing template-time errors.
    ///     </para>
    /// </summary>
    private static void SetCellNoDirty(SpreadsheetDoc sheet, int row, int col, string value)
    {
        try
        {
            sheet.SetContentsOfCell(ToSheetName(row, col), value);
        }
        catch
        {
            // Ignore sample template errors so the UI can still render partial sample data.
        }
    }

    /// <summary>
    ///     <para>
    ///         Identifies which modal dialog is currently being opened or closed.
    ///     </para>
    /// </summary>
    private enum ModalKind
    {
        Save,
        Load,
        Demo,
        About
    }

    /// <summary>
    ///     <para>
    ///         Save target selected by the user in the save modal.
    ///     </para>
    /// </summary>
    private enum SaveDestination
    {
        LocalStorage,
        Download
    }

    /// <summary>
    ///     <para>
    ///         Keys for built-in sample spreadsheet templates.
    ///     </para>
    /// </summary>
    private enum SampleTemplateKey
    {
        Q3VarianceReport,
        HeadcountModel,
        BudgetConsolidation,
        ActualsVsPlan
    }

    /// <summary>
    ///     <para>
    ///         Metadata for a sample document shown in the open menu.
    ///     </para>
    /// </summary>
    /// <param name="Name">Display name of the sample document.</param>
    /// <param name="DateText">Display date text for recency.</param>
    /// <param name="TemplateKey">Template identifier used for population.</param>
    private sealed record SampleDocument(string Name, string DateText, SampleTemplateKey TemplateKey);
}
