using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.DotNet;
using Varlock.Extensions.Hosting; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.AddVarlock(); // 👈 Varlock: load through the hosted configuration path and expose the resolved graph in DI

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var graph = app.Services.GetRequiredService<VarlockResolvedGraph>();

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"]}");
Console.WriteLine($"MAX_CONNECTIONS = {configuration["MAX_CONNECTIONS"]}");
Console.WriteLine($"FEATURE_ENABLED = {configuration["FEATURE_ENABLED"]}");
Console.WriteLine($"REQUEST_TIMEOUT_SECONDS = {configuration["REQUEST_TIMEOUT_SECONDS"]}");
Console.WriteLine($"MAX_CONNECTIONS_TYPE = {graph.Items["MAX_CONNECTIONS"].Value?.GetType().Name}");
Console.WriteLine($"FEATURE_ENABLED_TYPE = {graph.Items["FEATURE_ENABLED"].Value?.GetType().Name}");
Console.WriteLine($"REQUEST_TIMEOUT_SECONDS_TYPE = {graph.Items["REQUEST_TIMEOUT_SECONDS"].Value?.GetType().Name}");