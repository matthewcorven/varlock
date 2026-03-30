# Worker Service Example

This example shows the Generic Host seam only: `HostApplicationBuilder.AddVarlock()` loads configuration before the hosted service starts, and the worker consumes those values through `IConfiguration`. Reload and options semantics are covered elsewhere by the focused console siblings and the .NET test suite.

Run it from this directory:

```bash
dotnet run
```

For the machine-readable output used by automated validation:

```bash
dotnet run -- --dump-config
```