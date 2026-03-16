using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DotnetFunctionsIsolatedNet8;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration;

var dumpConfig = args.Contains("--dump-config", StringComparer.Ordinal);

var builder = new HostBuilder();
builder.ConfigureFunctionsWorkerDefaults();

// Varlock integrates into the existing IConfiguration chain.
//
// In local development, Azure Functions loads local.settings.json through the Functions host.
// For this proof path (dotnet run --dump-config), we explicitly load local.settings.json to
// demonstrate honest coexistence: both sources remain active, and Varlock's configuration
// provider adds to the chain.
//
// By default, configuration providers added later override earlier values for the same key.
// When a key exists in both local.settings.json and Varlock's .env.schema, Varlock wins by
// provider order, but keys unique to local.settings.json (like FUNCTIONS_ONLY_KEY) remain available.
builder.ConfigureAppConfiguration((context, config) =>
{
  // Explicitly load local.settings.json Values section to prove coexistence
  var localSettingsPath = Path.Combine(context.HostingEnvironment.ContentRootPath, "local.settings.json");
  if (File.Exists(localSettingsPath))
  {
    var jsonText = File.ReadAllText(localSettingsPath);
    var doc = JsonDocument.Parse(jsonText);
    if (doc.RootElement.TryGetProperty("Values", out var valuesElement))
    {
      var valuesDict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
      foreach (var prop in valuesElement.EnumerateObject())
      {
        valuesDict[prop.Name] = prop.Value.GetString();
      }
      config.AddInMemoryCollection(valuesDict);
    }
  }
  
  config.AddVarlock((source) =>
  {
    source.WorkingDirectory = context.HostingEnvironment.ContentRootPath;
  });
});

if (dumpConfig)
{
  using var host = builder.Build();
  var config = host.Services.GetRequiredService<IConfiguration>();
  var snapshot = FunctionsConfigSnapshot.From(config);
  Console.WriteLine(JsonSerializer.Serialize(snapshot));
  return;
}

using var appHost = builder.Build();
await appHost.RunAsync();
