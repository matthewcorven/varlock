# Serilog Console Example

This example shows the Serilog boundary: `WithVarlockRedaction(graph)` redacts sensitive properties when Serilog destructures `{@Object}` payloads, while the object being logged still holds plaintext values in-process.

## What It Shows

- `Destructure.WithVarlockRedaction(graph)` — redacts sensitive property values in destructured `{@Object}` templates
- `Enrich.WithVarlockMetadata(graph)` — adds `VarlockRedactLogs` enrichment property to log events
- Sensitive values appear as `[REDACTED]` in destructured Serilog output, while direct access to the object or graph remains plaintext unless you mask it separately

## When to Use This Pattern

When you already use Serilog structured logging and want destructured event payloads to respect Varlock sensitivity metadata without changing the underlying configuration objects.

## Run

```bash
dotnet run
```
