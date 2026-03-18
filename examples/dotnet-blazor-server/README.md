# Blazor Server Example

This example proves the Blazor Server seam only: Varlock is added during host startup and component code reads the resulting configuration through `IConfiguration`. It intentionally does not duplicate typed generation, reload, or logging demos that already exist in the console siblings and package tests.

Run it from this directory:

```bash
dotnet run
```

For the proof payload used by `bun run proof:dotnet`:

```bash
dotnet run -- --dump-config
```