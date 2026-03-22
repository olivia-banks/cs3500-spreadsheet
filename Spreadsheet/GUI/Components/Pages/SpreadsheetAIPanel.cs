using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using GUI.Components.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace GUI.Components.Pages;

/// <summary>
///     <para>
///         AI panel behavior for chat input, message state, and AI request dispatch.
///     </para>
/// </summary>
public partial class SpreadsheetPage
{
    /// <summary>
    ///     <para>
    ///         In-memory chat transcript shown in the AI panel.
    ///     </para>
    /// </summary>
    private readonly List<ChatMessage> _aiMessages =
    [
        new(false, "Hello! I can help with formula design, data structure questions, and spreadsheet logic. Select a cell and ask me anything.")
    ];

    [Inject]
    /// <summary>
    ///     <para>
    ///         Service that coordinates model interaction and spreadsheet tool-calling.
    ///     </para>
    /// </summary>
    private SpreadsheetAIService AIService { get; set; } = default!;

    /// <summary>
    ///     <para>
    ///         Gets or sets whether the AI side panel is collapsed.
    ///     </para>
    /// </summary>
    private bool AIHidden { get; set; }

    /// <summary>
    ///     <para>
    ///         Gets or sets whether the assistant is currently generating a response.
    ///     </para>
    /// </summary>
    private bool AITyping { get; set; }

    /// <summary>
    ///     <para>
    ///         Gets or sets the AI panel width in pixels when visible.
    ///     </para>
    /// </summary>
    private double AIPanelWidth { get; set; } = 300;

    /// <summary>
    ///     <para>
    ///         Gets or sets the current input text in the AI textbox.
    ///     </para>
    /// </summary>
    private string AIInput { get; set; } = string.Empty;

    /// <summary>
    ///     <para>
    ///         Inline width style for the AI panel, including hidden-state sizing.
    ///     </para>
    /// </summary>
    private string AIPanelStyle => $"width: {(AIHidden ? 0 : AIPanelWidth).ToString("0", CultureInfo.InvariantCulture)}px; min-width: {(AIHidden ? 0 : AIPanelWidth).ToString("0", CultureInfo.InvariantCulture)}px;";

    /// <summary>
    ///     <para>
    ///         Indicates whether the send action is currently enabled.
    ///     </para>
    /// </summary>
    private bool CanSendAI => !AITyping && !string.IsNullOrWhiteSpace(AIInput);

    /// <summary>
    ///     <para>
    ///         Read-only chat messages projected into the AI panel UI.
    ///     </para>
    /// </summary>
    private IReadOnlyList<ChatMessage> AIMessages => _aiMessages;

    /// <summary>
    ///     <para>
    ///         Toggles AI panel visibility.
    ///     </para>
    /// </summary>
    private void ToggleAI() => AIHidden = !AIHidden;

    /// <summary>
    ///     <para>
    ///         Opens the AI panel and appends a short help prompt.
    ///     </para>
    /// </summary>
    private void OpenAIHelp()
    {
        AIHidden = false;
        _aiMessages.Add(new ChatMessage(false, "I can help with:\n- Formula syntax and structure\n- Data layout advice\n- Spreadsheet concepts\n\nWhat would you like to know?"));
        CloseMenus();
    }

    /// <summary>
    ///     <para>
    ///         Sends the AI prompt when the user presses Enter without Shift.
    ///     </para>
    /// </summary>
    /// <param name="args">Keyboard metadata for the input event.</param>
    private async Task HandleAIKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter" && !args.ShiftKey)
        {
            await SendAI();
        }
    }

    /// <summary>
    ///     <para>
    ///         Updates local input state and triggers textarea auto-resize.
    ///     </para>
    /// </summary>
    /// <param name="args">Input change event payload.</param>
    private async Task OnAIInputChanged(ChangeEventArgs args)
    {
        AIInput = args.Value?.ToString() ?? string.Empty;
        if (_jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("autoResizeAiInput");
        }
    }

    /// <summary>
    ///     <para>
    ///         Sends the current question to the AI service and appends the response.
    ///     </para>
    /// </summary>
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

    /// <summary>
    ///     <para>
    ///         Represents a chat message in the AI transcript.
    ///     </para>
    /// </summary>
    /// <param name="IsUser">Whether the message originated from the user.</param>
    /// <param name="Text">The message text displayed in the panel.</param>
    private sealed record ChatMessage(bool IsUser, string Text);
}
