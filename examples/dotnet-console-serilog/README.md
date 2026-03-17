# Serilog Console Example

Demonstrates Varlock's Serilog integration — automatic redaction of `@sensitive` values in structured logs.

## What It Shows

- `Destructure.WithVarlockRedaction(graph)` — redacts sensitive property values in destructured `{@Object}` templates
- `Enrich.WithVarlockMetadata(graph)` — adds `VarlockRedactLogs` enrichment property to log events
- Sensitive values appear as `[REDACTED]` in Serilog output, non-sensitive values pass through

## When to Use This Pattern

When you use Serilog for structured logging and want to prevent `@sensitive` configuration values from leaking into log output, log aggregators (Seq, Datadog, Splunk), or other sinks.

## Run

```bash
dotnet run
```
