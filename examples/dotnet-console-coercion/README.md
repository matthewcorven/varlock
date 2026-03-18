# Coercion Console Example

This example proves the narrow CLI-bridge coercion surface that the current .NET packages already expose: `IConfiguration` still returns strings, while the injected `VarlockResolvedGraph` preserves boolean and numeric runtime values. It does not claim any new binder behavior beyond those already-proven graph types.

Run it from this directory:

```bash
dotnet run
```