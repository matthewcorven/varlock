using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using DotnetAspNetMvcNet8;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Varlock.Extensions.Configuration;

var reloadProof = args.Contains("--reload-proof", StringComparer.Ordinal);
var reloadFailProof = args.Contains("--reload-fail-proof", StringComparer.Ordinal);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddVarlock((source) =>
{
  source.WorkingDirectory = builder.Environment.ContentRootPath;
  source.ReloadOnChange = reloadProof || reloadFailProof;
});

builder.Services.AddControllersWithViews();

if (reloadProof || reloadFailProof)
{
  builder.Services.Configure<VarlockAppOptions>(builder.Configuration);
}

var snapshot = AppConfigSnapshot.From(builder.Configuration);
if (args.Contains("--dump-config", StringComparer.Ordinal))
{
  Console.WriteLine(JsonSerializer.Serialize(snapshot));
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