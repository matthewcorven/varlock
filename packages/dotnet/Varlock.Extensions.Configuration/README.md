# Varlock.Extensions.Configuration

Standard `IConfigurationBuilder` integration for Varlock's CLI bridge. This is the recommended entry point for most .NET applications.

## Installation

```bash
dotnet add package Varlock.Extensions.Configuration
```

## Basic usage

Add Varlock to your configuration pipeline:

```csharp
using Varlock.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Varlock after appsettings.json, so Varlock values override duplicates
builder.Configuration.AddVarlock(source =>
{
    // Optional: customize the lookup behavior
    source.SchemaPath = ".env.schema";  // Defaults to .env.schema in content root
    source.Optional = false;             // Fail if schema is missing
    source.ReloadOnChange = true;        // Watch for schema/env changes
});

var app = builder.Build();
app.Run();
```

## How it integrates

By default, Varlock loads in the middle of your configuration precedence chain:

1. `appsettings.json` — Loaded first
2. `appsettings.{Environment}.json` — Loaded second (environment-specific overrides)
3. **Varlock** — Loaded third (overrides keys from both appsettings files)
4. User Secrets — Loaded fourth (override Varlock values during development)
5. Environment variables — Loaded last (override everything)

This means a key set in both `appsettings.json` and your `.env.schema` will use the Varlock value, unless User Secrets or environment variables override it.

## Configuration options

```csharp
builder.Configuration.AddVarlock(source =>
{
    // Schema location (relative to ContentRootPath)
    source.SchemaPath = ".env.schema";
    
    // Working directory for schema resolution (defaults to ContentRootPath)
    source.WorkingDirectory = builder.Environment.ContentRootPath;
    
    // Fail fast if schema is missing
    source.Optional = false;
    
    // Watch for file changes and reload configuration
    source.ReloadOnChange = true;
    
    // Explicit executable path (overrides automatic lookup)
    source.ExecutablePath = "/usr/local/bin/varlock";
    
    // Control precedence with other configuration sources
    source.Precedence = VarlockPrecedence.OverrideExisting;  // Default
    // Or use VarlockPrecedence.FallbackWhenNotSet to only fill missing keys
});
```

## Accessing configuration at runtime

Use standard .NET patterns:

```csharp
// Direct IConfiguration
public class MyService
{
    private readonly IConfiguration _config;
    
    public MyService(IConfiguration config)
    {
        _config = config;
    }
    
    public void UseConfig()
    {
        string appName = _config["APP_NAME"];
    }
}

// IOptions<T> for request-scoped access (in web apps)
public class MyController : ControllerBase
{
    private readonly IOptionsSnapshot<AppConfig> _options;
    
    public MyController(IOptionsSnapshot<AppConfig> options)
    {
        _options = options;
    }
}

// IOptionsMonitor<T> for long-lived access with reload notifications (in workers)
public class MyWorker : BackgroundService
{
    private readonly IOptionsMonitor<AppConfig> _options;
    
    public MyWorker(IOptionsMonitor<AppConfig> options)
    {
        _options = options;
        
        // Subscribe to reload events
        _options.OnChange(newValue =>
        {
            Console.WriteLine("Configuration reloaded");
        });
    }
}
```

## Reload behavior

When `ReloadOnChange = true`, Varlock monitors your `.env.schema` and `.env` files for changes:

- Configuration is reloaded in-process without restarting the application
- `IOptionsMonitor<T>` subscribers are notified of changes
- `IOptionsSnapshot<T>` returns fresh values on next request (in scoped contexts)
- Failed reloads preserve the last-known-good configuration (graceful degradation)

See [Configuration & coexistence](/integrations/dotnet/configuration/) for reload semantics and error handling.

## Troubleshooting

Common issues and solutions:

| Issue | Solution |
| --- | --- |
| "varlock executable not found" | Install `varlock` as a dev dependency: `npm install --save-dev varlock` or `bun add --dev varlock` |
| "@required key missing: DATABASE_URL" | Add the missing key to your `.env` file |
| "Schema not found" | Ensure `.env.schema` exists in your project root, or set `source.SchemaPath` to the correct location |
| Duplicate key conflicts | Review precedence with `--dump-config` flag or check the [configuration guide](/integrations/dotnet/configuration/) |

See [Troubleshooting & diagnostics](/integrations/dotnet/troubleshooting/) for more help.

## For hosted applications

If you're using `HostApplicationBuilder`, `Host.CreateApplicationBuilder()`, or other modern .NET hosting patterns, use [`Varlock.Extensions.Hosting`](/integrations/dotnet/getting-started/) instead. It provides a cleaner API designed for hosted scenarios.

## Related packages

- **[Varlock.DotNet](/packages/dotnet/)** — Low-level bridge if you need direct CLI control
- **[Varlock.Extensions.Hosting](/integrations/dotnet/getting-started/)** — Cleaner API for `HostApplicationBuilder`
- **[Varlock.MSBuild](/integrations/dotnet/typed-options/)** — Build-time C# type generation
- **[Varlock.Serilog](/integrations/dotnet/security-and-logging/)** — Serilog redaction

## Next steps

- **[Getting started guide](/integrations/dotnet/getting-started/)** — Step-by-step setup
- **[Configuration & coexistence](/integrations/dotnet/configuration/)** — Understanding precedence and reload
- **[Typed access](/integrations/dotnet/typed-options/)** — Generate C# types from your schema