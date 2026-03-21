using System;
using System.Collections.Generic;
using GUI.Components;
using GUI.Components.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace GUI
{
    public class Program
    {
        public static void Main( string[] args )
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Add AI service to our app.
            var ollamaEndpoint = builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434";
            var modelToUse = DiscoverOllamaModel(ollamaEndpoint);
            builder.Services.AddChatClient(new OllamaChatClient(new Uri(ollamaEndpoint), modelToUse));
            builder.Services.AddScoped<SpreadsheetAIService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if ( !app.Environment.IsDevelopment() )
            {
                app.UseExceptionHandler( "/Error" );
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }

        private static string DiscoverOllamaModel(string endpoint)
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(endpoint) };
            using var response = httpClient.GetAsync("/api/tags").GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Failed to query Ollama models from {endpoint}/api/tags (status {(int)response.StatusCode}).");
            }

            using var stream = response.Content.ReadAsStream();
            var payload = JsonSerializer.Deserialize<OllamaTagsResponse>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            var models = payload?.Models?
                .Select(m => m.Name ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray()
                ?? [];

            if (models.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No Ollama models discovered at {endpoint}. Pull at least one model (e.g., `ollama pull qwen3:0.6B`).");
            }

            if (models.Length > 1)
            {
                Console.WriteLine($"Multiple Ollama models discovered. Defaulting to '{models[0]}'. Available: {string.Join(", ", models)}");
            }

            return models[0];
        }

        private sealed class OllamaTagsResponse
        {
            public List<OllamaModelInfo>? Models { get; set; }
        }

        private sealed class OllamaModelInfo
        {
            public string? Name { get; set; }
        }
    }
}
