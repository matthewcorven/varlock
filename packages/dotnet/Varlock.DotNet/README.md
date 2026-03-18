# Varlock.DotNet

Low-level .NET bridge primitives for invoking the Varlock CLI and consuming its machine-readable configuration graph.

This package provides the minimal surface for directly executing the Varlock CLI, parsing its schema, and working with the machine-readable configuration output. Most applications should use [`Varlock.Extensions.Configuration`](/integrations/dotnet/getting-started/) for standard .NET integration patterns instead.

## What this package provides

- **CLI bridge:** Locate and invoke the `varlock` CLI executable with version handshake
- **Machine-readable contracts:** C# types for the CLI output envelope and configuration graph
- **Error handling:** `VarlockException` with structured error categories and source attribution
- **Low-level API:** Direct access to configuration data for advanced or non-hosted scenarios

## Installation

```bash
dotnet add package Varlock.DotNet
```

## Basic usage

```csharp
using Varlock.DotNet;

// Locate and invoke the CLI
var bridge = new VarlockBridge();
var result = await bridge.LoadAsync(new VarlockLoadRequest
{
    SchemaPath = ".env.schema",
    WorkingDirectory = "/path/to/project"
});

if (!result.Success)
{
    // Handle error with structured category and message
    Console.WriteLine($"Error: {result.Error.ErrorCategory}");
    Console.WriteLine($"Message: {result.Error.Message}");
    return;
}

// Access resolved configuration
foreach (var item in result.ConfigurationGraph.Items)
{
    Console.WriteLine($"{item.Key} = {item.Value}");
}
```

## Executable lookup

The bridge uses the following lookup order to find the `varlock` CLI:

1. **Explicit path:** `ExecutablePath` if provided
2. **Package-local:** `node_modules/varlock/bin/cli.js` (npm/bun installed)
3. **Local `.bin`:** `node_modules/.bin/varlock` (fallback)
4. **Repository development:** `packages/varlock/bin/cli.js` (if you're developing inside the Varlock repo)
5. **PATH:** `varlock` command on system PATH (development/opt-in only; not recommended for production CI)

Set `ExecutablePath` explicitly to override the lookup order:

```csharp
var bridge = new VarlockBridge(new BridgeOptions
{
    ExecutablePath = "/usr/local/bin/varlock"
});
```

## Error handling

All errors are wrapped in `VarlockException` with a structured `ErrorCategory`:

```csharp
try
{
    var result = await bridge.LoadAsync(request);
}
catch (VarlockException ex)
{
    // ex.ErrorCategory: one of executable-not-found, executable-version-mismatch, 
    //                   schema-missing, schema-invalid, resolution-failed, etc.
    // ex.Message: human-readable error text
    // ex.Location: optional file path and line number for schema errors
    
    Console.WriteLine($"[{ex.ErrorCategory}] {ex.Message}");
}
```

See [Troubleshooting & diagnostics](/integrations/dotnet/troubleshooting/) for detailed error category reference.

## When to use this package directly

- **Non-hosted console applications** that don't use `HostApplicationBuilder`
- **Custom orchestration** where you need direct control over CLI invocation
- **Advanced integration** that doesn't fit the configuration-provider model
- **Direct configuration inspection** for debugging or tooling

For most applications, use [`Varlock.Extensions.Configuration`](/integrations/dotnet/getting-started/) instead. It provides standard `IConfiguration` integration and handles the bridge details for you.

## Quick start with `Env.Load()`

For simple non-hosted scenarios (console apps, scripts, utilities), the static `Env` class provides a convenience façade over the CLI bridge:

```csharp
using Varlock.DotNet;

// Load with defaults — discovers schema and executable automatically
var graph = Env.Load();

// Access resolved values
Console.WriteLine(graph["MY_API_KEY"]);
```

With configuration:

```csharp
var graph = Env.Load(options =>
{
    options.SchemaPath = ".env.schema";
    options.WorkingDirectory = "/path/to/project";
});
```

Async variant:

```csharp
var graph = await Env.LoadAsync();
```

> **Note:** `Env.Load()` is pure sugar over `VarlockCliRuntime` — it creates a new runtime instance on every call with no caching, singleton state, or DI registration. For hosted applications (ASP.NET Core, Worker Services), use [`Varlock.Extensions.Hosting`](/packages/dotnet/) with `AddVarlock()` instead, which integrates properly with `IConfiguration` and the host lifecycle.

### Available overloads

| Method | Description |
|--------|-------------|
| `Env.Load()` | Load with default options |
| `Env.Load(VarlockLoadOptions)` | Load with explicit options object |
| `Env.Load(Action<VarlockLoadOptions>)` | Load with configuration delegate |
| `Env.LoadAsync(CancellationToken)` | Async load with default options |
| `Env.LoadAsync(VarlockLoadOptions, CancellationToken)` | Async load with explicit options |

## Related packages

- **[Varlock.Extensions.Configuration](/integrations/dotnet/getting-started/)** — Integration with `IConfigurationBuilder` (recommended for most apps)
- **[Varlock.Extensions.Hosting](/integrations/dotnet/getting-started/)** — `HostApplicationBuilder` helpers for modern hosted applications
- **[Varlock.MSBuild](/integrations/dotnet/typed-options/)** — Build-time C# type generation
- **[Varlock.Serilog](/integrations/dotnet/security-and-logging/)** — Serilog redaction for sensitive values

## Next steps

- **[Getting started guide](/integrations/dotnet/getting-started/)** — Add Varlock to your application
- **[Typed access](/integrations/dotnet/typed-options/)** — Generate C# types for your configuration schema
- **[Troubleshooting](/integrations/dotnet/troubleshooting/)** — Debug bridge errors and version mismatches