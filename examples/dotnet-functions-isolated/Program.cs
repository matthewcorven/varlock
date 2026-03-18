using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Text.Json;
using Varlock.Extensions.Configuration; // 👈 Varlock

var builder = FunctionsApplication.CreateBuilder(args);

var localSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "local.settings.json");
if (File.Exists(localSettingsPath))
{
    using var stream = File.OpenRead(localSettingsPath);
    using var document = JsonDocument.Parse(stream);

    if (document.RootElement.TryGetProperty("Values", out var valuesElement)
        && valuesElement.ValueKind == JsonValueKind.Object)
    {
        var localSettingsValues = new Dictionary<string, string?>();

        foreach (var property in valuesElement.EnumerateObject())
        {
            localSettingsValues[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.ToString();
        }

        builder.Configuration.AddInMemoryCollection(localSettingsValues);
    }
}

builder.Configuration.AddVarlock(); // 👈 Varlock: join the Functions configuration pipeline
builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

if (args.Contains("--dump-config"))
{
    var payload = new FunctionsConfigPayload(
        builder.Configuration["APP_NAME"] ?? string.Empty,
        builder.Configuration["FUNCTIONS_ONLY_KEY"] ?? string.Empty);

    Console.WriteLine(JsonSerializer.Serialize(payload));
    return;
}

builder.Build().Run();

sealed record FunctionsConfigPayload(string AppName, string FunctionsOnlyKey);
