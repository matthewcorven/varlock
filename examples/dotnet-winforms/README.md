# WinForms Example

This example proves the legacy desktop seam only: a WinForms app can call `VarlockCliRuntime.Load()` directly, read the resolved graph, and surface the result in UI code without DI or hosting helpers. It does not double as a sensitivity, reload, or typed-binding example.

Run it from this directory on Windows:

```bash
dotnet run
```

For the proof payload used by `bun run proof:dotnet`:

```bash
dotnet run -- --dump-config
```