using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Varlock.Extensions.Hosting; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.AddVarlock(); // 👈 Varlock

// 👈 Varlock: register options from IConfiguration for long-lived monitor access
builder.Services
    .AddOptions<AppOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.AppName = configuration["APP_NAME"] ?? string.Empty;
        options.MaxRetries = int.Parse(configuration["MAX_RETRIES"] ?? "0");
        options.Verbose = bool.Parse(configuration["VERBOSE"] ?? "false");
    });

using var app = builder.Build();

// 👈 IOptionsMonitor is singleton — resolve directly, no scope needed
var monitor = app.Services.GetRequiredService<IOptionsMonitor<AppOptions>>();
var current = monitor.CurrentValue;

Console.WriteLine($"APP_NAME = {current.AppName}");
Console.WriteLine($"MAX_RETRIES = {current.MaxRetries}");
Console.WriteLine($"VERBOSE = {current.Verbose}");
Console.WriteLine("OPTIONS_PATTERN = monitor-singleton");

file sealed class AppOptions
{
    public string AppName { get; set; } = string.Empty;

    public int MaxRetries { get; set; }

    public bool Verbose { get; set; }
}
