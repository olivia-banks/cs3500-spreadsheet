// <copyright file="SpreadsheetAIService.cs" company="UofU-CS3500">
// Copyright (c) 2026 UofU-CS3500. All rights reserved.
// </copyright>
// Written by Professor Ahmad Alsaleem and Hung Phan Quoc Viet for CS 3500, Spring 2026 

namespace GUI.Components.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using GUI.Components.Tools;
using Microsoft.Extensions.AI;
using Spreadsheet;

/// <summary>
/// Provides AI-driven interaction capabilities for spreadsheet manipulation.
/// </summary>
public class SpreadsheetAIService
{
    private const string DefaultSystemPrompt = """
You are Be More, a spreadsheet assistant.

Goals:
- Be correct, concise, and practical.
- Prefer using available spreadsheet tools to inspect state before giving specific claims.

Tool behavior:
- When asked about existing data, use read tools first.
- Only modify cells when the user clearly requests a change.
- After modifications, summarize what changed (cell names and values/formulas).

Response style:
- Keep responses short unless the user asks for detail.
- If ambiguous, ask one focused follow-up question.
- Never invent cell values you have not read.
- Never use emoji or casual language.
""";

    /// <summary>
    /// The client used to communicate with the chat service.
    /// </summary>
    /// <remarks>
    /// This field is marked as <c>readonly</c> to ensure that the client instance 
    /// remains constant throughout the lifetime of this service, supporting 
    /// thread-safety and preventing accidental reassignment.
    /// </remarks>
    private readonly IChatClient _chatClient;
    private readonly string _systemPrompt;

    /// <summary>
    /// Gets the conversation history for the current session.
    /// </summary>
    public List<ChatMessage> ChatHistory { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the service is currently processing an AI request.
    /// </summary>
    public bool IsProcessing { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpreadsheetAIService"/> class.
    /// </summary>
    /// <param name="chatClient">The underlying chat client to use for AI responses.</param>
    public SpreadsheetAIService(IChatClient chatClient)
    {
        _chatClient = new ChatClientBuilder(chatClient)
            .UseFunctionInvocation()
            .Build();

        _systemPrompt = DefaultSystemPrompt;
    }

    /// <summary>
    /// Processes a natural language query and performs actions on the provided <see cref="Spreadsheet"/>.
    /// </summary>
    /// <param name="input">The user's natural language request.</param>
    /// <param name="activeSheet">The spreadsheet instance to be manipulated by the AI.</param>
    /// <returns>The assistant's response text.</returns>
    public async Task<string> ProcessQueryAsync(string input, Spreadsheet activeSheet)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        IsProcessing = true;

        try
        {
            if (ChatHistory.Count == 0)
            {
                ChatHistory.Add(new ChatMessage(ChatRole.System, _systemPrompt));
            }

            ChatHistory.Add(new ChatMessage(ChatRole.User, input));

            // Define the tools available to the AI based on the current sheet context
            var tools = new SpreadsheetTools(activeSheet);
            var options = new ChatOptions
            {
                Tools = [
                    AIFunctionFactory.Create(tools.SetCellContent),
                    AIFunctionFactory.Create(tools.GetCellContentInfo),
                    AIFunctionFactory.Create(tools.GetActiveCells),
                    AIFunctionFactory.Create(tools.GetCellValue),
                    AIFunctionFactory.Create(tools.GetCellRange),
                    AIFunctionFactory.Create(tools.GetCellDependencies),
                    AIFunctionFactory.Create(tools.GetCellDependents)
                ]
            };

            // Request response from AI; if it needs data, it will call the tools provided above
            var response = await _chatClient.GetResponseAsync(ChatHistory, options);

            // Save the AI's response to the history and return its text
            var botMessage = response.Messages.Count >= 3 ? response.Messages[2] : response.Messages[0];
            ChatHistory.Add(botMessage);
            return botMessage.Text ?? string.Empty;
        }
        finally
        {
            IsProcessing = false;
        }
    }
}