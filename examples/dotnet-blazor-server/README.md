# Blazor Server Example

This example shows the Blazor Server seam only: Varlock is added during host startup and component code reads the resulting configuration through `IConfiguration`. The page intentionally renders only non-sensitive configuration values. It intentionally does not duplicate typed generation, reload, or logging demos that already exist in the console siblings and package tests.

Run it from this directory:

```bash
dotnet run
```

For the machine-readable output used by automated validation:

```bash
dotnet run -- --dump-config
```