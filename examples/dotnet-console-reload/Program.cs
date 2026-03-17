using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Varlock.Extensions.Configuration; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

// 👈 Varlock: enable automatic reload when .env or .env.schema files change
builder.Configuration.AddVarlock(source =>
{
    source.ReloadOnChange = true;
});

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();

Console.WriteLine("Watching for .env changes (edit .env to see reload). Press Ctrl+C to exit.");
Console.WriteLine();

// 👈 Varlock: register a callback for configuration change notifications
var reloadToken = configuration.GetReloadToken();
ChangeToken.OnChange(
    () => configuration.GetReloadToken(),
    () =>
    {
        Console.WriteLine("[reloaded] Configuration changed:");
        Console.WriteLine($"  APP_NAME = {configuration["APP_NAME"]}");
        Console.WriteLine($"  MAX_RETRIES = {configuration["MAX_RETRIES"]}");
    });

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"]}");
Console.WriteLine($"MAX_RETRIES = {configuration["MAX_RETRIES"]}");
Console.WriteLine();
Console.WriteLine("Try editing .env and saving — values will reload automatically.");

// Keep the app alive to demonstrate file watching
await Task.Delay(Timeout.Infinite, app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
