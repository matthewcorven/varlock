using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.DotNet;
using Varlock.Extensions.Hosting; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.AddVarlock(); // 👈 Varlock: surface PreventLeaks metadata and keep the proof explicit about its non-enforcing boundary

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var graph = app.Services.GetRequiredService<VarlockResolvedGraph>();
var rawSecret = configuration["SECRET_TOKEN"] ?? string.Empty;

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"]}");
Console.WriteLine($"PREVENT_LEAKS = {graph.PreventLeaks}");
Console.WriteLine($"SECRET_TOKEN_PRESENT = {!string.IsNullOrEmpty(rawSecret)}");
Console.WriteLine($"SECRET_TOKEN_IS_SENSITIVE = {graph.Items["SECRET_TOKEN"].IsSensitive}");
Console.WriteLine($"DISPLAY_SECRET_TOKEN = {VarlockRedactionHelper.Redact(graph, "SECRET_TOKEN", rawSecret)}");
Console.WriteLine("LEAK_PREVENTION_BOUNDARY = metadata-only");