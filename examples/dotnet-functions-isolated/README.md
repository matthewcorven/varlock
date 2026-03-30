# Azure Functions Isolated Example

This example shows the isolated-worker integration seam only: Varlock is added to the Functions host configuration, and `local.settings.json` values that do not overlap remain available beside Varlock-backed keys. The sample response stays limited to non-sensitive values. It does not double as a typed-generation, reload, or logging specimen.

Run it from this directory:

```bash
dotnet run
```

For the machine-readable output used by automated validation:

```bash
dotnet run -- --dump-config
```