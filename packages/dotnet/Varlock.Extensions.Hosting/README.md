# Varlock.Extensions.Hosting

`HostApplicationBuilder` and `WebApplicationBuilder` integration for Varlock. Use this in modern .NET hosted applications like ASP.NET Core, Worker Services, and Generic Host applications.

This package provides clean `AddVarlock()` extensions that handle Varlock setup in the standard .NET dependency injection container.

## Installation

```bash
dotnet add package Varlock.Extensions.Hosting
```

## Basic usage

For **ASP.NET Core MVC/Minimal APIs** (requires .NET 10+):

```csharp
using Varlock.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add Varlock directly on the builder (net10.0+ only)
builder.AddVarlock();

// Continue with standard setup
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.MapControllers();
app.Run();
```

For **ASP.NET Core on earlier target frameworks** (netstandard2.0 compatible):

```csharp
using Varlock.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add Varlock to the configuration pipeline
builder.Configuration.AddVarlock(source =>
{
    source.ReloadOnChange = true;
});

// Continue with standard setup
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.MapControllers();
app.Run();
```

For **Worker Services / Generic Host**:

```csharp
using Varlock.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Add Varlock to the configuration pipeline
builder.Configuration.AddVarlock(source =>
{
    source.ReloadOnChange = true;
});

// Add your services
builder.Services.AddHostedService<MyBackgroundService>();

var host = builder.Build();
await host.RunAsync();
```

## WebApplicationBuilder.AddVarlock() (net10.0+)

On .NET 10 and later, `WebApplicationBuilder` gains direct `AddVarlock()` extension methods that mirror the existing `HostApplicationBuilder` overloads:

```csharp
// Parameterless — uses default options
builder.AddVarlock();

// With configuration delegate
builder.AddVarlock(source =>
{
    source.SchemaPath = ".env.schema";
    source.ReloadOnChange = true;
    source.Optional = false;
});
```

These are thin sugar over `builder.Configuration.AddVarlock()` — they do not alter provider order, options, or runtime semantics. The `builder.Configuration.AddVarlock()` call remains available on all target frameworks.

> **Note:** The `WebApplicationBuilder` overloads require the `net10.0` target framework. On earlier frameworks, use `builder.Configuration.AddVarlock()` directly.

## Configuration options

The `AddVarlock()` method accepts the same configuration options as [`Varlock.Extensions.Configuration`](/packages/dotnet/):

```csharp
builder.Configuration.AddVarlock(source =>
{
    source.SchemaPath = ".env.schema";
    source.WorkingDirectory = builder.Environment.ContentRootPath;
    source.Optional = false;
    source.ReloadOnChange = true;
    source.ExecutablePath = "/usr/local/bin/varlock";
    source.Precedence = VarlockPrecedence.OverrideExisting;
});
```

For hosted options binding convenience, generic overloads are available on both builders:

```csharp
builder.AddVarlock<MyAppOptions>();

// Or with source configuration
builder.AddVarlock<MyAppOptions>(source =>
{
    source.ReloadOnChange = true;
});
```

These overloads are convenience wrappers over `AddVarlock(...)` plus standard `services.Configure<TConfig>(builder.Configuration)` binding.

## Accessing configuration

Use standard .NET patterns for configuration access:

```csharp
// Injected IConfiguration
public class MyService
{
    private readonly IConfiguration _config;
    
    public MyService(IConfiguration config)
    {
        _config = config;
    }
}

// IOptionsSnapshot<T> for request-scoped access (ASP.NET Core)
public class MyController : ControllerBase
{
    private readonly IOptionsSnapshot<AppConfig> _options;
    
    public MyController(IOptionsSnapshot<AppConfig> options)
    {
        _options = options;
    }
}

// IOptionsMonitor<T> for long-lived access with reload (Worker Services)
public class MyBackgroundService : BackgroundService
{
    private readonly IOptionsMonitor<AppConfig> _options;
    
    public MyBackgroundService(IOptionsMonitor<AppConfig> options)
    {
        _options = options;
        
        // Subscribe to reload notifications
        _options.OnChange(newValue =>
        {
            Console.WriteLine("Configuration reloaded");
        });
    }
}
```

## Reload behavior

When `ReloadOnChange = true` in hosted applications:

- **Worker Services / Generic Host:** Configuration reloads while the host is running. Use `IOptionsMonitor<T>` to subscribe to change notifications.
- **ASP.NET Core:** Each new request gets fresh configuration via `IOptionsSnapshot<T>`. Use `IOptionsMonitor<T>` for long-lived services that need reload notifications.
- **Failed reloads:** The last-known-good configuration is preserved. Applications continue running with the previous good state.

See [Configuration & coexistence](/integrations/dotnet/configuration/) for detailed reload semantics.

## Troubleshooting

See [Troubleshooting & diagnostics](/integrations/dotnet/troubleshooting/) for common issues and solutions.

Common items:
- Schema not found or executable not located
- Required keys missing from environment
- Configuration reload failures
- Version mismatches between CLI and package

## Related packages

- **[Varlock.Extensions.Configuration](/packages/dotnet/)** — Core `IConfigurationBuilder` integration if you're not using `HostApplicationBuilder`
- **[Varlock.DotNet](/packages/dotnet/)** — Low-level bridge for non-hosted scenarios
- **[Varlock.MSBuild](/integrations/dotnet/typed-options/)** — Build-time C# type generation
- **[Varlock.Serilog](/integrations/dotnet/security-and-logging/)** — Serilog redaction for sensitive values

## Next steps

- **[Getting started guide](/integrations/dotnet/getting-started/)** — Complete setup walkthrough
- **[Configuration & coexistence](/integrations/dotnet/configuration/)** — Understanding precedence and reload
- **[Typed access](/integrations/dotnet/typed-options/)** — Generate C# types for type-safe configuration
