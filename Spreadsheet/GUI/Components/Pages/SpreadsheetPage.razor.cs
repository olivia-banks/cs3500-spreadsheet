using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CircularException = Spreadsheet.CircularException;
using FormulaError = Formula.FormulaError;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SpreadsheetDoc = Spreadsheet.Spreadsheet;

namespace GUI.Components.Pages;

public partial class SpreadsheetPage : IAsyncDisposable
{
    private const int Rows = 50;
    private const int Cols = 26;

    private SpreadsheetDoc _sheet = new();
    private readonly HashSet<string> _findMatches = new();

    private readonly List<SampleDocument> _sampleDocuments =
    [
        new("Q3 Variance Report", "Jul 14, 2025"),
        new("Headcount Model FY25", "Jul 9, 2025"),
        new("Budget Consolidation v4", "Jun 28, 2025"),
        new("Actuals vs Plan - H1", "Jun 15, 2025")
    ];

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private DotNetObjectReference<SpreadsheetPage>? _dotNetRef;
    private IJSObjectReference? _jsModule;
    private bool _needsFocusEditInput;

    private CancellationTokenSource? _notificationCts;

    private int SelectedRow { get; set; }
    private int SelectedCol { get; set; }

    private int EditingRow { get; set; } = -1;
    private int EditingCol { get; set; } = -1;

    private bool SaveModalOpen { get; set; }
    private bool LoadModalOpen { get; set; }
    private bool AboutModalOpen { get; set; }

    private bool FindVisible { get; set; }
    private bool Editing { get; set; }
    private bool IsSaved { get; set; } = true;
    private bool NotificationVisible { get; set; }

    private double Zoom { get; set; } = 1;

    private string DocumentName { get; set; } = "Q3 Variance Report";
    private string SaveName { get; set; } = "Q3 Variance Report";
    private string FormulaInput { get; set; } = string.Empty;
    private string EditingValue { get; set; } = string.Empty;
    private string FindQuery { get; set; } = string.Empty;
    private string? OpenMenuName { get; set; }
    private string NotificationMessage { get; set; } = string.Empty;
    private string? PendingOpenDocument { get; set; }

    private DateTimeOffset? LastSavedAt { get; set; }

    private string SelectedCellRef => $"{ColumnLabel(SelectedCol)}{SelectedRow + 1}";
    private string SelectedCellDisplay => string.IsNullOrWhiteSpace(CellValue(SelectedRow, SelectedCol)) ? "-" : CellValue(SelectedRow, SelectedCol);
    private string ZoomPercentText => $"{(int)Math.Round(Zoom * 100)}%";
    private string ZoomStyle => $"transform: scale({Zoom.ToString("0.0", CultureInfo.InvariantCulture)}); transform-origin: top left;";
    private IReadOnlyList<SampleDocument> SampleDocuments => _sampleDocuments;

    private string FindCountText => string.IsNullOrWhiteSpace(FindQuery)
        ? string.Empty
        : _findMatches.Count > 0
            ? $"{_findMatches.Count} found"
            : "No matches";

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

    protected override void OnInitialized()
    {
        Seed();
        SyncFormulaInputWithSelection();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./spreadsheetGrid.js");
            await _jsModule.InvokeVoidAsync("initialize", _dotNetRef);
            await _jsModule.InvokeVoidAsync("autoResizeAiInput");
        }

        if (_needsFocusEditInput && _jsModule is not null)
        {
            _needsFocusEditInput = false;
            await _jsModule.InvokeVoidAsync("focusCellInput");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            try { await _jsModule.InvokeVoidAsync("cleanup"); } catch { }
            await _jsModule.DisposeAsync();
        }

        _dotNetRef?.Dispose();
    }

    /// <summary>Called from the document-level JS keydown listener in spreadsheetGrid.js.</summary>
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

    [JSInvokable]
    public Task CloseMenusFromJs()
    {
        return InvokeAsync(() =>
        {
            CloseMenus();
            StateHasChanged();
        });
    }

    private static string CellKey(int row, int col) => $"{row},{col}";

    private static string ColumnLabel(int col) => ((char)('A' + col)).ToString(CultureInfo.InvariantCulture);

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

    private string CellValue(int row, int col)
    {
        try
        {
            var value = _sheet.GetCellValue(ToSheetName(row, col));
            return value switch
            {
                string s => s,
                double d => d.ToString(CultureInfo.InvariantCulture),
                FormulaError fe => fe.Reason,
                _ => string.Empty
            };
        }
        catch
        {
            return string.Empty;
        }
    }

    private bool IsEditingCell(int row, int col) => Editing && row == EditingRow && col == EditingCol;

    private bool IsFindMatch(int row, int col) => _findMatches.Contains(CellKey(row, col));

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

    private void StartEdit(int row, int col)
    {
        SelectCell(row, col);
        Editing = true;
        EditingRow = row;
        EditingCol = col;
        EditingValue = CellRawInput(row, col);
        _needsFocusEditInput = true;
    }

    private void StartEditWithCharacter(string character)
    {
        Editing = true;
        EditingRow = SelectedRow;
        EditingCol = SelectedCol;
        EditingValue = character;
        _needsFocusEditInput = true;
    }

    private void OnEditChanged(ChangeEventArgs args)
    {
        EditingValue = args.Value?.ToString() ?? string.Empty;
    }

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

    private void CancelEdit()
    {
        Editing = false;
        EditingRow = -1;
        EditingCol = -1;
        EditingValue = string.Empty;
    }

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
        catch (Exception)
        {
            Notify("Invalid formula");
            return;
        }

        MarkUnsaved();
        if (!string.IsNullOrWhiteSpace(FindQuery))
        {
            RefreshFindMatches();
        }
    }

    private void ClearSelectedCell()
    {
        CancelEdit();
        SetCell(SelectedRow, SelectedCol, string.Empty);
        SyncFormulaInputWithSelection();
        Notify("Cell cleared");
    }

    private void MoveSelection(int rowOffset, int colOffset)
    {
        SelectCell(
            Math.Clamp(SelectedRow + rowOffset, 0, Rows - 1),
            Math.Clamp(SelectedCol + colOffset, 0, Cols - 1));
    }

    private void OnFormulaChanged(ChangeEventArgs args)
    {
        FormulaInput = args.Value?.ToString() ?? string.Empty;
    }

    private void HandleFormulaKeyDown(KeyboardEventArgs args)
    {
        if (args.Key != "Enter")
        {
            return;
        }

        SetCell(SelectedRow, SelectedCol, FormulaInput);
        SyncFormulaInputWithSelection();
    }

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

    private async Task HandleEditKeyDown(KeyboardEventArgs args)
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

        await Task.CompletedTask;
    }

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

    private void ZoomIn() => Zoom = Math.Min(2, Math.Round(Zoom + 0.1, 1));

    private void ZoomOut() => Zoom = Math.Max(0.5, Math.Round(Zoom - 0.1, 1));

    private void ResetZoom() => Zoom = 1;

    private void ToggleMenu(string menuName)
    {
        OpenMenuName = OpenMenuName == menuName ? null : menuName;
    }

    private void CloseMenus()
    {
        OpenMenuName = null;
    }

    private void ShowFind()
    {
        FindVisible = true;
        RefreshFindMatches();
        CloseMenus();
    }

    private void HideFind()
    {
        FindVisible = false;
        FindQuery = string.Empty;
        _findMatches.Clear();
    }

    private void OnFindInput(ChangeEventArgs args)
    {
        FindQuery = args.Value?.ToString() ?? string.Empty;
        RefreshFindMatches();
    }

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

    private void OpenModal(ModalKind kind)
    {
        CloseMenus();
        SaveModalOpen = kind == ModalKind.Save;
        LoadModalOpen = kind == ModalKind.Load;
        AboutModalOpen = kind == ModalKind.About;
        if (kind == ModalKind.Save)
        {
            SaveName = DocumentName;
        }
    }

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
        else
        {
            AboutModalOpen = false;
        }
    }

    private void ConfirmSave()
    {
        DocumentName = string.IsNullOrWhiteSpace(SaveName) ? "Untitled" : SaveName.Trim();
        SaveModalOpen = false;
        MarkSaved();
        Notify("Save dispatched");
    }

    private void OpenSample(string documentName)
    {
        PendingOpenDocument = documentName;
    }

    private void ConfirmOpen()
    {
        if (!string.IsNullOrWhiteSpace(PendingOpenDocument))
        {
            DocumentName = PendingOpenDocument;
            SaveName = PendingOpenDocument;
            Notify("Opening...");
        }

        LoadModalOpen = false;
    }

    private void OnDocumentNameChanged(ChangeEventArgs args)
    {
        DocumentName = args.Value?.ToString() ?? string.Empty;
        SaveName = DocumentName;
        MarkUnsaved();
    }

    private void OnSaveNameChanged(ChangeEventArgs args)
    {
        SaveName = args.Value?.ToString() ?? string.Empty;
    }

    private void NewDocument()
    {
        _sheet = new SpreadsheetDoc();
        _findMatches.Clear();
        FindQuery = string.Empty;
        SelectCell(0, 0);
        MarkUnsaved();
        Notify("New document");
    }

    private void MarkUnsaved()
    {
        IsSaved = false;
    }

    private void MarkSaved()
    {
        IsSaved = true;
        LastSavedAt = DateTimeOffset.UtcNow;
    }

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

    private void Seed()
    {
        SetCellNoDirty(0, 0, "MONTH");
        SetCellNoDirty(0, 1, "REVENUE");
        SetCellNoDirty(0, 2, "BUDGET");
        SetCellNoDirty(0, 3, "VARIANCE");
        SetCellNoDirty(0, 4, "VAR %");

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
        var revenue = new[] { 128400, 141200, 133800, 156700, 149000, 172300 };
        var budget = new[] { 130000, 135000, 140000, 150000, 155000, 160000 };

        for (var i = 0; i < months.Length; i++)
        {
            SetCellNoDirty(i + 1, 0, months[i]);
            SetCellNoDirty(i + 1, 1, revenue[i].ToString("N0", CultureInfo.InvariantCulture));
            SetCellNoDirty(i + 1, 2, budget[i].ToString("N0", CultureInfo.InvariantCulture));
            SetCellNoDirty(i + 1, 3, (revenue[i] - budget[i]).ToString("N0", CultureInfo.InvariantCulture));
            SetCellNoDirty(i + 1, 4, $"={ColumnLabel(3)}{i + 2}/{ColumnLabel(2)}{i + 2}*100");
        }

        SetCellNoDirty(7, 0, "TOTAL");
        SetCellNoDirty(7, 1, "=SUM(B2:B7)");
        SetCellNoDirty(7, 2, "=SUM(C2:C7)");
        SetCellNoDirty(7, 3, "=SUM(D2:D7)");
        SetCellNoDirty(7, 4, "-");

        SetCellNoDirty(9, 0, "AVERAGE");
        SetCellNoDirty(9, 1, "=AVERAGE(B2:B7)");
        SetCellNoDirty(9, 2, "=AVERAGE(C2:C7)");

        MarkSaved();
    }

    private void SetCellNoDirty(int row, int col, string value)
    {
        try
        {
            _sheet.SetContentsOfCell(ToSheetName(row, col), value);
        }
        catch
        {
            // Ignore seed errors
        }
    }

    private enum ModalKind
    {
        Save,
        Load,
        About
    }

    private sealed record SampleDocument(string Name, string DateText);
}
