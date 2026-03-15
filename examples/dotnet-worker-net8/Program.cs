using System;
using System.Text.Json;
using DotnetWorkerNet8;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Varlock.Extensions.Hosting;

var reloadProof = args.Contains("--reload-proof", StringComparer.Ordinal);
var reloadFailProof = args.Contains("--reload-fail-proof", StringComparer.Ordinal);
var dumpConfig = args.Contains("--dump-config", StringComparer.Ordinal);

var builder = Host.CreateApplicationBuilder(args);

builder.AddVarlock((source) =>
{
  source.WorkingDirectory = builder.Environment.ContentRootPath;
  source.ReloadOnChange = reloadProof || reloadFailProof;
});

builder.Services.Configure<VarlockWorkerOptions>(builder.Configuration);
builder.Services.AddSingleton(new WorkerProofOptions(
  DumpConfig: dumpConfig,
  ReloadProof: reloadProof,
  ReloadFailProof: reloadFailProof));
builder.Services.AddHostedService<Worker>();

using var host = builder.Build();

if (dumpConfig)
{
  var monitor = host.Services.GetRequiredService<IOptionsMonitor<VarlockWorkerOptions>>();
  Console.WriteLine(JsonSerializer.Serialize(WorkerConfigSnapshot.From(monitor.CurrentValue)));
  return;
}

await host.RunAsync();
