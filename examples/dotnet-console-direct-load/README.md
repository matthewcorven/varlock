# Direct Load Console Example

This example shows direct `VarlockCliRuntime.Load()` usage without going through `IConfiguration` or the hosting model.

## What It Shows

- `VarlockCliRuntime.Load()` with default `VarlockLoadOptions`
- Iterating `VarlockResolvedGraph.Items` to read key/value pairs
- Checking `IsSensitive` to conditionally mask values
- Reading `Sources`, `RedactLogs`, and `PreventLeaks` from the graph

## When to Use This Pattern

When you need direct access to the resolved graph without the overhead of `IConfiguration` or dependency injection — for scripts, tools, or custom integration scenarios.

## Run

```bash
dotnet run
```
