using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.DotNet;
using Varlock.Extensions.Hosting; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.AddVarlock(); // 👈 Varlock: resolve a sensitive value through exec() while keeping the example scoped to a local command seam

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var graph = app.Services.GetRequiredService<VarlockResolvedGraph>();
// Raw secret is still available through IConfiguration inside this process.
var rawToken = configuration["SERVICE_TOKEN"] ?? string.Empty;

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"]}");
// Display output goes through VarlockRedactionHelper, so the secret prints as [REDACTED].
Console.WriteLine($"SERVICE_TOKEN = {VarlockRedactionHelper.Redact(graph, "SERVICE_TOKEN", rawToken)}");
Console.WriteLine($"SERVICE_TOKEN_PRESENT = {!string.IsNullOrEmpty(rawToken)}");
Console.WriteLine($"SERVICE_TOKEN_IS_SENSITIVE = {graph.Items["SERVICE_TOKEN"].IsSensitive}");
Console.WriteLine("EXEC_SOURCE = local-bun-command");