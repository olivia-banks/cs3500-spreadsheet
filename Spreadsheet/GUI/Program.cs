using System;
using GUI.Components;
using GUI.Components.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

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
            var ollamaEndpoint = builder.Configuration["AI:Ollama:Endpoint"] ?? "http://localhost:11434";
            var configuredModels = builder.Configuration
                .GetSection("AI:Ollama:Models")
                .Get<string[]>()?
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToArray()
                ?? [];

            var selectedModel = builder.Configuration["AI:Ollama:SelectedModel"];
            string modelToUse;

            if (!string.IsNullOrWhiteSpace(selectedModel))
            {
                modelToUse = selectedModel;
            }
            else if (configuredModels.Length == 1)
            {
                // If there is exactly one configured model, default to it automatically.
                modelToUse = configuredModels[0];
            }
            else if (configuredModels.Length > 1)
            {
                throw new InvalidOperationException(
                    "Multiple AI models are configured. Set AI:Ollama:SelectedModel to choose one.");
            }
            else
            {
                throw new InvalidOperationException(
                    "No AI models configured. Set AI:Ollama:Models in appsettings.json.");
            }

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
    }
}
