# Azure Functions Isolated Example

This example proves the isolated-worker integration seam only: Varlock is added to the Functions host configuration, and `local.settings.json` values that do not overlap remain available beside Varlock-backed keys. It does not double as a typed-generation, reload, or logging specimen.

Run it from this directory:

```bash
dotnet run
```

For the proof payload used by `bun run proof:dotnet`:

```bash
dotnet run -- --dump-config
```