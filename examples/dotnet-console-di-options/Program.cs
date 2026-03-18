using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Varlock.Extensions.Hosting; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.AddVarlock(); // 👈 Varlock: keep the current hosted setup, then register options manually from IConfiguration

builder.Services
    .AddOptions<AppOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.AppName = configuration["APP_NAME"] ?? string.Empty;
        options.HttpPort = int.Parse(configuration["HTTP_PORT"] ?? "0");
        options.FeatureEnabled = bool.Parse(configuration["FEATURE_ENABLED"] ?? "false");
    });

builder.Services.AddSingleton<OptionsReporter>();

using var app = builder.Build();

app.Services.GetRequiredService<OptionsReporter>().Print();

file sealed class OptionsReporter
{
    private readonly IOptionsMonitor<AppOptions> _options;

    public OptionsReporter(IOptionsMonitor<AppOptions> options)
    {
        _options = options;
    }

    public void Print()
    {
        var current = _options.CurrentValue;

        Console.WriteLine($"APP_NAME = {current.AppName}");
        Console.WriteLine($"HTTP_PORT = {current.HttpPort}");
        Console.WriteLine($"FEATURE_ENABLED = {current.FeatureEnabled}");
        Console.WriteLine("OPTIONS_PATTERN = manual-map");
    }
}

file sealed class AppOptions
{
    public string AppName { get; set; } = string.Empty;

    public int HttpPort { get; set; }

    public bool FeatureEnabled { get; set; }
}