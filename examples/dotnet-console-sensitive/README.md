# Sensitive Values Console Example

This example shows the manual .NET sensitivity boundary: `@sensitive` marks the key, the resolved graph still carries the plaintext value in-process, and `VarlockRedactionHelper.Redact()` only changes what this sample writes to stdout.

## What It Shows

- `@sensitive` decorator in `.env.schema` marks sensitive keys
- `VarlockResolvedGraph.Items` still exposes the raw value plus `IsSensitive` metadata inside the process
- `VarlockRedactionHelper.Redact()` returns `[REDACTED]` for sensitive keys on the display path
- Accessing the `VarlockResolvedGraph` from `VarlockConfigurationProvider.Graph`
- Iterating items with `IsSensitive` flag for conditional display

## When to Use This Pattern

When your application needs a manual display or logging step that checks Varlock sensitivity metadata before writing configuration values to stdout, logs, or telemetry.

## Run

```bash
dotnet run
```
