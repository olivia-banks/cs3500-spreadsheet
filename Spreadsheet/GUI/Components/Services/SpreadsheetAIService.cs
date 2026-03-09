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
    /// <summary>
    /// The client used to communicate with the chat service.
    /// </summary>
    /// <remarks>
    /// This field is marked as <c>readonly</c> to ensure that the client instance 
    /// remains constant throughout the lifetime of this service, supporting 
    /// thread-safety and preventing accidental reassignment.
    /// </remarks>
    private readonly IChatClient _chatClient;

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
    }

    /// <summary>
    /// Processes a natural language query and performs actions on the provided <see cref="Spreadsheet"/>.
    /// </summary>
    /// <param name="input">The user's natural language request.</param>
    /// <param name="activeSheet">The spreadsheet instance to be manipulated by the AI.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessQueryAsync(string input, Spreadsheet activeSheet)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        IsProcessing = true;

        try
        {
            ChatHistory.Add(new ChatMessage(ChatRole.User, input));

            // Define the tools available to the AI based on the current sheet context
            var tools = new SpreadsheetTools(activeSheet);
            var options = new ChatOptions
            {
                Tools = [
                    AIFunctionFactory.Create(tools.SetCellContent),
                   // TODO: You can add more tools to be included from SpreadsheetTools.cs
                ]
            };

            // Request response from AI; if it needs data, it will call the tools provided above
            var response = await _chatClient.GetResponseAsync(ChatHistory, options);

            // Save the AI's response to the history
            if (response.Messages.Count >= 3)
                ChatHistory.Add(response.Messages[2]);
            else
                ChatHistory.Add(response.Messages[0]);
        }
        finally
        {
            IsProcessing = false;
        }
    }
}