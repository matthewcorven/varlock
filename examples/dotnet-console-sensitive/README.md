# Sensitive Values Console Example

Demonstrates `@sensitive` flagging and `VarlockRedactionHelper` for safe logging.

## What It Shows

- `@sensitive` decorator in `.env.schema` marks secrets
- `VarlockRedactionHelper.Redact()` returns `[REDACTED]` for sensitive keys
- Accessing the `VarlockResolvedGraph` from `VarlockConfigurationProvider.Graph`
- Iterating items with `IsSensitive` flag for conditional display

## When to Use This Pattern

When your application logs configuration values and you need to ensure secrets (database URLs, API keys, tokens) are never written to stdout, log files, or telemetry.

## Run

```bash
dotnet run
```
