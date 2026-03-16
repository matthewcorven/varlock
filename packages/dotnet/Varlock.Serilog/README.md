# Varlock.Serilog

Serilog-specific destructuring redaction and metadata enrichment for Varlock configuration values.

This package integrates with Serilog's structured logging to automatically redact sensitive configuration values during log serialization while preserving non-sensitive metadata.

## Installation

```bash
dotnet add package Varlock.Serilog
```

Also ensure you have `Serilog` installed:

```bash
dotnet add package Serilog
```

## Basic usage

Add Varlock redaction to your Serilog configuration:

```csharp
using Varlock.Extensions.Configuration;
using Varlock.Serilog;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load configuration with Varlock
var varlockGraph = (VarlockConfigurationSource)builder.Configuration
    .AddVarlock(source => { })
    .First();

// Configure Serilog with Varlock redaction
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .Destructure.With(new VarlockRedactionDestructurer(varlockGraph))
    .Enrich.With(new VarlockMetadataEnricher(varlockGraph))
    .CreateLogger();

builder.Services.AddSerilog();

var app = builder.Build();
app.Run();
```

## How redaction works

When Serilog logs an object that contains sensitive configuration values:

1. **Detection:** The redaction destructurer identifies keys marked as `@sensitive` in your schema
2. **Redaction:** Sensitive values are replaced with `[REDACTED]` during destructuring
3. **Preservation:** Non-sensitive properties remain visible for debugging

Example:

```csharp
var config = new { ApiKey = "secret123", AppName = "my-app" };
logger.Information("Config: {@config}", config);

// Output: Config: { ApiKey = [REDACTED], AppName = "my-app" }
```

## Sensitive metadata

Define sensitive keys in your schema with `@sensitive`:

```javascript
/**
 * @env-spec
 * @defaultSensitive false
 */

APP_NAME = @string @default("my-app")
API_KEY = @string @sensitive
DATABASE_PASSWORD = @string @sensitive
```

The redaction destructurer automatically redacts values for keys marked `@sensitive`.

## Important limitations

### Exact key matching

Redaction matches keys **exactly and case-sensitively**. If your configuration key is `API_KEY`, the destructurer looks for that exact name.

```javascript
API_KEY = @string @sensitive    // Redacts "API_KEY"
```

```csharp
var config = new { api_key = "secret" };  // Different case: NOT redacted
logger.Information("Config: {@config}", config);  // Output: { api_key = "secret" }
```

### Serilog destructuring only

Redaction applies **only to Serilog destructuring** (`@` destructure syntax). It does not:

- Redact string-template message parameters
- Affect other logging libraries or console output
- Redact environment variables accessed directly
- Apply globally to all process output

To redact non-Serilog channels, see [Security & logging](/integrations/dotnet/security-and-logging/) for manual patterns.

### Example with mixed redaction:

```csharp
var apiKey = "secret123";
var config = new { API_KEY = "secret123" };

// NOT redacted (string template parameter)
logger.Information("Key is: {key}", apiKey);  // Output: Key is: secret123

// Redacted (Serilog destructure + sensitive key match)
logger.Information("Config: {@config}", config);  // Output: Config: { API_KEY = [REDACTED] }

// NOT redacted (accessed from environment directly, not through Varlock)
logger.Information("Env key: {key}", Environment.GetEnvironmentVariable("API_KEY"));
```

## Metadata enrichment

The `VarlockMetadataEnricher` adds `VarlockRedactLogs` to the Serilog `LogContext` for tracking:

```csharp
.Enrich.With(new VarlockMetadataEnricher(varlockGraph))
```

This enriches all log entries with metadata indicating Varlock redaction is active. It does **not** cause automatic redaction by itself; you must also use `VarlockRedactionDestructurer`.

## Complete example

```csharp
using Varlock.Extensions.Configuration;
using Varlock.Serilog;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load Varlock config
var config = builder.Configuration;
config.AddVarlock(source => { });

// Set up Serilog with redaction
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss}] [{Level}] {Message:lj}{NewLine}{Exception}")
    .Destructure.With(new VarlockRedactionDestructurer(config))
    .Enrich.With(new VarlockMetadataEnricher(config))
    .CreateLogger();

builder.Services.AddSerilog();

var app = builder.Build();

// Example usage
var logger = app.Services.GetRequiredService<ILogger<Program>>();

var credentials = new
{
    DATABASE_URL = config["DATABASE_URL"],  // Sensitive
    APP_NAME = config["APP_NAME"]            // Not sensitive
};

logger.LogInformation("Application settings: {@credentials}", credentials);
// Output: APPLICATION SETTINGS: { DATABASE_URL = [REDACTED], APP_NAME = "my-app" }

app.Run();
```

## For non-Serilog applications

If you're not using Serilog, you must redact sensitive values manually. See [Security & logging](/integrations/dotnet/security-and-logging/) for guidance and helper patterns.

## Related packages

- **[Varlock.Extensions.Configuration](/integrations/dotnet/getting-started/)** — Runtime configuration provider
- **[Varlock.Extensions.Hosting](/integrations/dotnet/getting-started/)** — Hosting integration

## Next steps

- **[Security & logging guide](/integrations/dotnet/security-and-logging/)** — Full security story and manual redaction patterns
- **[Getting started](/integrations/dotnet/getting-started/)** — Complete setup walkthrough
