using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using GUI.Components.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace GUI.Components.Pages;

public partial class SpreadsheetPage
{
    private readonly List<ChatMessage> _aiMessages =
    [
        new(false, "Hello! I can help with formula design, data structure questions, and spreadsheet logic. Select a cell and ask me anything.")
    ];

    [Inject]
    private SpreadsheetAIService AIService { get; set; } = default!;

    private bool AIHidden { get; set; }
    private bool AITyping { get; set; }
    private bool ResizingAI { get; set; }

    private double AIPanelWidth { get; set; } = 300;
    private double LastResizeClientX { get; set; }

    private string AIInput { get; set; } = string.Empty;

    private string AIPanelStyle => $"width: {(AIHidden ? 0 : AIPanelWidth).ToString("0", CultureInfo.InvariantCulture)}px; min-width: {(AIHidden ? 0 : AIPanelWidth).ToString("0", CultureInfo.InvariantCulture)}px;";
    private bool CanSendAI => !AITyping && !string.IsNullOrWhiteSpace(AIInput);
    private IReadOnlyList<ChatMessage> AIMessages => _aiMessages;

    private void ToggleAI() => AIHidden = !AIHidden;

    private void StartResizeAI(MouseEventArgs args)
    {
        if (AIHidden)
        {
            return;
        }

        ResizingAI = true;
        LastResizeClientX = args.ClientX;
    }

    private void ResizeAIPanel(MouseEventArgs args)
    {
        if (!ResizingAI)
        {
            return;
        }

        if (args.Buttons == 0)
        {
            StopResizeAI();
            return;
        }

        var delta = LastResizeClientX - args.ClientX;
        LastResizeClientX = args.ClientX;
        AIPanelWidth = Math.Clamp(AIPanelWidth + delta, 240, 560);
    }

    private void StopResizeAI()
    {
        ResizingAI = false;
    }

    private void OpenAIHelp()
    {
        AIHidden = false;
        _aiMessages.Add(new ChatMessage(false, "I can help with:\n- Formula syntax and structure\n- Data layout advice\n- Spreadsheet concepts\n\nWhat would you like to know?"));
        CloseMenus();
    }

    private async Task HandleAIKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter" && !args.ShiftKey)
        {
            await SendAI();
        }
    }

    private async Task OnAIInputChanged(ChangeEventArgs args)
    {
        AIInput = args.Value?.ToString() ?? string.Empty;
        if (_jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("autoResizeAiInput");
        }
    }

    private async Task SendAI()
    {
        if (AITyping)
        {
            return;
        }

        var question = AIInput.Trim();
        if (question.Length == 0)
        {
            return;
        }

        AIInput = string.Empty;
        _aiMessages.Add(new ChatMessage(true, question));
        AITyping = true;
        StateHasChanged();

        if (_jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("autoResizeAiInput");
        }

        string response;
        try
        {
            response = await AIService.ProcessQueryAsync(question, _sheet);
            if (string.IsNullOrWhiteSpace(response))
            {
                response = "I processed your request.";
            }

            SyncUIWithSpreadsheet();
        }
        catch (Exception ex)
        {
            response = $"Appologies, I encountered an error: {ex.Message}";
        }

        _aiMessages.Add(new ChatMessage(false, response));
        AITyping = false;
        StateHasChanged();
    }

    private sealed record ChatMessage(bool IsUser, string Text);
}
