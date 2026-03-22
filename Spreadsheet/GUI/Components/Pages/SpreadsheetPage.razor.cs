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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SpreadsheetDoc = Spreadsheet.Spreadsheet;

namespace GUI.Components.Pages;

public partial class SpreadsheetPage : IAsyncDisposable
{
    private const int Rows = 50;
    private const int Cols = 26;
    private const string DefaultDocumentName = "Q3 Variance Report";
    private const long MaxUploadBytes = 5 * 1024 * 1024;
    private const int MaxFileReadBytes = 2 * 1024 * 1024;

    private SpreadsheetDoc _sheet = new();
    private readonly HashSet<string> _findMatches = new();
    private readonly List<LocalDocument> _localDocuments = [];

    private readonly List<SampleDocument> _sampleDocuments =
    [
        new("Q3 Variance Report", "Jul 14, 2025", SampleTemplateKey.Q3VarianceReport),
        new("Headcount Model FY25", "Jul 9, 2025", SampleTemplateKey.HeadcountModel),
        new("Budget Consolidation v4", "Jun 28, 2025", SampleTemplateKey.BudgetConsolidation),
        new("Actuals vs Plan - H1", "Jun 15, 2025", SampleTemplateKey.ActualsVsPlan)
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

    private string DocumentName { get; set; } = DefaultDocumentName;
    private string SaveName { get; set; } = DefaultDocumentName;
    private string FormulaInput { get; set; } = string.Empty;
    private string EditingValue { get; set; } = string.Empty;
    private string FindQuery { get; set; } = string.Empty;
    private string? OpenMenuName { get; set; }
    private string NotificationMessage { get; set; } = string.Empty;
    private string? PendingOpenDocument { get; set; }
    private string? SelectedLocalDocumentName { get; set; }

    private SaveDestination SelectedSaveDestination { get; set; } = SaveDestination.LocalStorage;

    private DateTimeOffset? LastSavedAt { get; set; }

    private string SelectedCellRef => $"{ColumnLabel(SelectedCol)}{SelectedRow + 1}";
    private string SelectedCellDisplay => string.IsNullOrWhiteSpace(CellValue(SelectedRow, SelectedCol)) ? "-" : CellValue(SelectedRow, SelectedCol);
    private string ZoomPercentText => $"{(int)Math.Round(Zoom * 100)}%";
    private string ZoomStyle => $"transform: scale({Zoom.ToString("0.0", CultureInfo.InvariantCulture)}); transform-origin: top left;";
    private IReadOnlyList<SampleDocument> SampleDocuments => _sampleDocuments;
    private IReadOnlyList<LocalDocument> LocalDocuments => _localDocuments;

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
        LoadTemplate(SampleTemplateKey.Q3VarianceReport, DefaultDocumentName, markSaved: true);
    }

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

        if (kind == ModalKind.Load)
        {
            _ = RefreshLocalDocumentsAsync();
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
        MarkSaved();
    }

    private void OpenSample(string documentName)
    {
        PendingOpenDocument = documentName;
    }

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

    private void SelectLocalDocument(string documentName)
    {
        SelectedLocalDocumentName = documentName;
    }

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
            LoadModalOpen = false;
            Notify($"Loaded '{loadedName}' from file");
        }
        catch (Exception ex)
        {
            Notify($"Failed to load file: {ex.Message}");
        }
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
        DocumentName = "Untitled";
        SaveName = DocumentName;
        PendingOpenDocument = null;
        ResetViewportState();
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

    private string ExportSheetJson()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"bmos-{Guid.NewGuid():N}.json");
        try
        {
            _sheet.Save(tempPath);
            return File.ReadAllText(tempPath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private void LoadSpreadsheetFromJson(string json, string documentName)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"bmos-load-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempPath, json);
            _sheet = new SpreadsheetDoc(tempPath);
            DocumentName = documentName;
            SaveName = documentName;
            ResetViewportState();
            MarkSaved();
            SyncUIWithSpreadsheet();
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static string EnsureJsonFileName(string name)
    {
        var normalized = string.IsNullOrWhiteSpace(name) ? "spreadsheet" : name.Trim();
        return normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : normalized + ".json";
    }

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

    private enum ModalKind
    {
        Save,
        Load,
        About
    }

    private enum SaveDestination
    {
        LocalStorage,
        Download
    }

    private enum SampleTemplateKey
    {
        Q3VarianceReport,
        HeadcountModel,
        BudgetConsolidation,
        ActualsVsPlan
    }

    private sealed record SampleDocument(string Name, string DateText, SampleTemplateKey TemplateKey);
}
