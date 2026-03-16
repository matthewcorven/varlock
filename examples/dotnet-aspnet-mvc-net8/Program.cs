using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using DotnetAspNetMvcNet8;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Varlock.DotNet;
using Varlock.Extensions.Configuration;
using Varlock.Serilog;

var reloadProof = args.Contains("--reload-proof", StringComparer.Ordinal);
var reloadFailProof = args.Contains("--reload-fail-proof", StringComparer.Ordinal);
var snapshotProof = args.Contains("--snapshot-proof", StringComparer.Ordinal);
var optionsProof = args.Contains("--options-proof", StringComparer.Ordinal);
var serilogProof = args.Contains("--serilog-proof", StringComparer.Ordinal);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddVarlock((source) =>
{
  source.WorkingDirectory = builder.Environment.ContentRootPath;
  source.ReloadOnChange = reloadProof || reloadFailProof || snapshotProof;
});

builder.Services.AddControllersWithViews();

if (reloadProof || reloadFailProof || snapshotProof || optionsProof)
{
  builder.Services.Configure<VarlockAppOptions>(builder.Configuration);
}

var snapshot = AppConfigSnapshot.From(builder.Configuration);
if (args.Contains("--dump-config", StringComparer.Ordinal))
{
  Console.WriteLine(JsonSerializer.Serialize(snapshot));
  return;
}

if (optionsProof)
{
  var app = builder.Build();
  var config = app.Services.GetRequiredService<IConfiguration>();
  var options = app.Services.GetRequiredService<IOptions<VarlockAppOptions>>();
  Console.WriteLine(JsonSerializer.Serialize(AppConfigSnapshot.From(options.Value, config)));
  return;
}

if (serilogProof)
{
  var graph = new VarlockCliRuntime().Load(new VarlockLoadOptions
  {
    WorkingDirectory = builder.Environment.ContentRootPath,
  });
  var sink = new CollectingSerilogSink();
  var secretToken = GetRequiredString(graph, "SECRET_TOKEN");

  using var logger = new LoggerConfiguration()
    .Destructure.WithVarlockRedaction(graph)
    .Enrich.WithVarlockMetadata(graph)
    .WriteTo.Sink(sink)
    .CreateLogger();

  logger.Information(
    "SERILOG_PROOF {@Config} {SECRET_TOKEN}",
    new Dictionary<string, object?>(StringComparer.Ordinal)
    {
      ["SECRET_TOKEN"] = secretToken,
      ["APP_NAME"] = GetRequiredString(graph, "APP_NAME"),
      ["secret_token"] = secretToken,
    },
    secretToken);

  var logEvent = sink.SingleEvent();
  Console.WriteLine("SERILOG_PROOF:" + JsonSerializer.Serialize(new
  {
    graphRedactLogs = graph.RedactLogs,
    eventRedactLogs = GetRequiredBoolean(logEvent, "VarlockRedactLogs"),
    destructuredSecretToken = GetRequiredDictionaryString(logEvent, "Config", "SECRET_TOKEN"),
    destructuredAppName = GetRequiredDictionaryString(logEvent, "Config", "APP_NAME"),
    destructuredCaseMismatchSecretToken = GetRequiredDictionaryString(logEvent, "Config", "secret_token"),
    scalarSecretToken = GetRequiredScalarString(logEvent, "SECRET_TOKEN"),
  }));
  return;
}

if (reloadProof)
{
  var app = builder.Build();
  var config = app.Services.GetRequiredService<IConfiguration>();
  var monitor = app.Services.GetRequiredService<IOptionsMonitor<VarlockAppOptions>>();

  var reloadFired = 0;
  var reloadEvent = new ManualResetEventSlim(false);

  using var subscription = monitor.OnChange(_ =>
  {
    Interlocked.Increment(ref reloadFired);
    reloadEvent.Set();
  });

  var initialSnapshot = AppConfigSnapshot.From(config);
  Console.WriteLine("RELOAD_PROOF_INITIAL:" + JsonSerializer.Serialize(initialSnapshot));
  Console.Out.Flush();

  // Modify .env.schema to trigger a successful reload.
  var schemaPath = Path.Combine(builder.Environment.ContentRootPath, ".env.schema");
  var originalContent = File.ReadAllText(schemaPath);
  var modifiedContent = originalContent.Replace(
    "APP_NAME=varlock-web",
    "APP_NAME=varlock-reloaded",
    StringComparison.Ordinal);
  File.WriteAllText(schemaPath, modifiedContent);

  try
  {
    var gotReload = reloadEvent.Wait(TimeSpan.FromSeconds(15));
    if (gotReload)
    {
      Thread.Sleep(150);
      var reloadedSnapshot = AppConfigSnapshot.From(config);
      Console.WriteLine("RELOAD_PROOF_RELOADED:" + JsonSerializer.Serialize(reloadedSnapshot));
      Console.WriteLine("RELOAD_PROOF_COUNT:" + reloadFired);
      Console.WriteLine("RELOAD_PROOF_MONITOR_APP_NAME:" + monitor.CurrentValue.APP_NAME);
    }
    else
    {
      Console.WriteLine("RELOAD_PROOF_TIMEOUT");
    }
  }
  finally
  {
    File.WriteAllText(schemaPath, originalContent);
  }

  return;
}

if (snapshotProof)
{
  var app = builder.Build();
  var config = app.Services.GetRequiredService<IConfiguration>();
  var monitor = app.Services.GetRequiredService<IOptionsMonitor<VarlockAppOptions>>();

  var reloadFired = 0;
  var reloadEvent = new ManualResetEventSlim(false);

  using var subscription = monitor.OnChange(_ =>
  {
    Interlocked.Increment(ref reloadFired);
    reloadEvent.Set();
  });

  using var scopeA = app.Services.CreateScope();
  var requestA = scopeA.ServiceProvider.GetRequiredService<IOptionsSnapshot<VarlockAppOptions>>();
  Console.WriteLine("SNAPSHOT_PROOF_SCOPE_A_INITIAL:" + JsonSerializer.Serialize(AppConfigSnapshot.From(requestA.Value, config)));
  Console.Out.Flush();

  var schemaPath = Path.Combine(builder.Environment.ContentRootPath, ".env.schema");
  var originalContent = File.ReadAllText(schemaPath);
  var modifiedContent = originalContent.Replace(
    "APP_NAME=varlock-web",
    "APP_NAME=varlock-snapshot-reloaded",
    StringComparison.Ordinal);
  File.WriteAllText(schemaPath, modifiedContent);

  try
  {
    var gotReload = reloadEvent.Wait(TimeSpan.FromSeconds(15));
    if (gotReload)
    {
      Thread.Sleep(150);

      using var scopeB = app.Services.CreateScope();
      var requestB = scopeB.ServiceProvider.GetRequiredService<IOptionsSnapshot<VarlockAppOptions>>();
      Console.WriteLine("SNAPSHOT_PROOF_SCOPE_B_AFTER:" + JsonSerializer.Serialize(AppConfigSnapshot.From(requestB.Value, config)));
      Console.WriteLine("SNAPSHOT_PROOF_SCOPE_A_STILL:" + JsonSerializer.Serialize(AppConfigSnapshot.From(requestA.Value, config)));
      Console.WriteLine("SNAPSHOT_PROOF_RELOAD_COUNT:" + reloadFired);
      Console.WriteLine("SNAPSHOT_PROOF_MONITOR_APP_NAME:" + monitor.CurrentValue.APP_NAME);

      File.WriteAllText(schemaPath, "BROKEN_SYNTAX{{{not-a-valid-schema");
      Thread.Sleep(2000);

      using var scopeC = app.Services.CreateScope();
      var requestC = scopeC.ServiceProvider.GetRequiredService<IOptionsSnapshot<VarlockAppOptions>>();
      Console.WriteLine("SNAPSHOT_PROOF_SCOPE_C_AFTER_FAILED:" + JsonSerializer.Serialize(AppConfigSnapshot.From(requestC.Value, config)));
      Console.WriteLine("SNAPSHOT_PROOF_FINAL_RELOAD_COUNT:" + reloadFired);
    }
    else
    {
      Console.WriteLine("SNAPSHOT_PROOF_TIMEOUT");
    }
  }
  finally
  {
    File.WriteAllText(schemaPath, originalContent);
  }

  return;
}

if (reloadFailProof)
{
  var app = builder.Build();
  var config = app.Services.GetRequiredService<IConfiguration>();
  var monitor = app.Services.GetRequiredService<IOptionsMonitor<VarlockAppOptions>>();

  var reloadFired = 0;

  using var subscription = monitor.OnChange(_ =>
  {
    Interlocked.Increment(ref reloadFired);
  });

  var initialSnapshot = AppConfigSnapshot.From(config);
  Console.WriteLine("RELOAD_FAIL_PROOF_INITIAL:" + JsonSerializer.Serialize(initialSnapshot));
  Console.Out.Flush();

  // Corrupt .env.schema to cause the CLI bridge to fail on reload.
  var schemaPath = Path.Combine(builder.Environment.ContentRootPath, ".env.schema");
  var originalContent = File.ReadAllText(schemaPath);
  File.WriteAllText(schemaPath, "BROKEN_SYNTAX{{{not-a-valid-schema");

  try
  {
    // Wait for debounce (300 ms) + reload attempt.
    Thread.Sleep(2000);

    var afterSnapshot = AppConfigSnapshot.From(config);
    Console.WriteLine("RELOAD_FAIL_PROOF_AFTER:" + JsonSerializer.Serialize(afterSnapshot));
    Console.WriteLine("RELOAD_FAIL_PROOF_COUNT:" + reloadFired);
    Console.WriteLine("RELOAD_FAIL_PROOF_MONITOR_APP_NAME:" + monitor.CurrentValue.APP_NAME);
  }
  finally
  {
    File.WriteAllText(schemaPath, originalContent);
  }

  return;
}

var app2 = builder.Build();
app2.MapControllers();
app2.Run();

static string GetRequiredString(VarlockResolvedGraph graph, string key)
{
  if (!graph.Items.TryGetValue(key, out var item))
  {
    throw new InvalidOperationException($"Expected Varlock item '{key}' to be present.");
  }

  return item.Value as string
    ?? throw new InvalidOperationException($"Expected Varlock item '{key}' to resolve to a string.");
}

static bool GetRequiredBoolean(LogEvent logEvent, string propertyName)
{
  if (!logEvent.Properties.TryGetValue(propertyName, out var propertyValue))
  {
    throw new InvalidOperationException($"Expected Serilog property '{propertyName}' to be present.");
  }

  return propertyValue is ScalarValue { Value: bool boolValue }
    ? boolValue
    : throw new InvalidOperationException($"Expected Serilog property '{propertyName}' to be a boolean scalar.");
}

static string GetRequiredScalarString(LogEvent logEvent, string propertyName)
{
  if (!logEvent.Properties.TryGetValue(propertyName, out var propertyValue))
  {
    throw new InvalidOperationException($"Expected Serilog property '{propertyName}' to be present.");
  }

  return propertyValue is ScalarValue { Value: string stringValue }
    ? stringValue
    : throw new InvalidOperationException($"Expected Serilog property '{propertyName}' to be a string scalar.");
}

static string GetRequiredDictionaryString(LogEvent logEvent, string propertyName, string key)
{
  if (!logEvent.Properties.TryGetValue(propertyName, out var propertyValue))
  {
    throw new InvalidOperationException($"Expected Serilog property '{propertyName}' to be present.");
  }

  if (propertyValue is not DictionaryValue dictionaryValue)
  {
    throw new InvalidOperationException($"Expected Serilog property '{propertyName}' to be a destructured dictionary.");
  }

  foreach (var entry in dictionaryValue.Elements)
  {
    if (entry.Key.Value is string entryKey && string.Equals(entryKey, key, StringComparison.Ordinal))
    {
      return entry.Value is ScalarValue { Value: string stringValue }
        ? stringValue
        : throw new InvalidOperationException($"Expected dictionary key '{key}' to resolve to a string scalar.");
    }
  }

  throw new InvalidOperationException($"Expected dictionary key '{key}' to be present in Serilog property '{propertyName}'.");
}

sealed class CollectingSerilogSink : ILogEventSink
{
  private readonly List<LogEvent> _events = new();

  public void Emit(LogEvent logEvent)
  {
    _events.Add(logEvent);
  }

  public LogEvent SingleEvent()
  {
    return _events.Count == 1
      ? _events[0]
      : throw new InvalidOperationException($"Expected exactly one Serilog event but captured {_events.Count}.");
  }
}
