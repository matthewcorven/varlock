# Coercion Console Example

This example shows the current coercion boundary in the CLI-backed .NET path: `IConfiguration` still returns strings, while the injected `VarlockResolvedGraph` preserves boolean and numeric runtime values. It does not claim any new binder behavior beyond that resolved graph surface.

Run it from this directory:

```bash
dotnet run
```