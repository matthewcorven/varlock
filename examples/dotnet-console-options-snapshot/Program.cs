using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Varlock.Extensions.Hosting; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.AddVarlock(); // 👈 Varlock

// 👈 Varlock: register options from IConfiguration for scoped snapshot access
builder.Services
    .AddOptions<AppOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.AppName = configuration["APP_NAME"] ?? string.Empty;
        options.HttpPort = int.Parse(configuration["HTTP_PORT"] ?? "0");
        options.FeatureEnabled = bool.Parse(configuration["FEATURE_ENABLED"] ?? "false");
    });

using var app = builder.Build();

// 👈 IOptionsSnapshot is scoped — create a scope to resolve it
using var scope = app.Services.CreateScope();
var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AppOptions>>();
var current = snapshot.Value;

Console.WriteLine($"APP_NAME = {current.AppName}");
Console.WriteLine($"HTTP_PORT = {current.HttpPort}");
Console.WriteLine($"FEATURE_ENABLED = {current.FeatureEnabled}");
Console.WriteLine("OPTIONS_PATTERN = snapshot-scoped");

file sealed class AppOptions
{
    public string AppName { get; set; } = string.Empty;

    public int HttpPort { get; set; }

    public bool FeatureEnabled { get; set; }
}
