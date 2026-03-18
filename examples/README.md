# .NET Proof Examples

This directory holds the runnable proof artifacts and DX specimens for Varlock's `.NET` support work.

Current automated proof in `bun run proof:dotnet` covers:

- baseline console and framework examples under `examples/dotnet-console/`, `examples/dotnet-aspnet-mvc/`, `examples/dotnet-worker/`, `examples/dotnet-functions-isolated/`, `examples/dotnet-blazor-server/`, `examples/dotnet-winforms/`, and `examples/dotnet-blazor-wasm-public/`
- first console sibling batch: direct load, typed config, sensitive handling, Serilog redaction, and reload
- second console sibling batch: custom schema path, custom working directory, provider-level environment fallback, optional startup, and custom runtime injection

The console baseline still carries the executable-acquisition proof paths:

1. repo-local lookup to `packages/varlock/bin/cli.js`
2. proof-only package-local `node_modules/varlock/bin/cli.js`
3. proof-only local `node_modules/.bin/varlock`
4. opt-in `PATH` lookup guarded by `VARLOCK_DOTNET_PROOF_FORCE_PATH_LOOKUP=1`

`bun run proof:dotnet` also performs explicit `dotnet build` checks before the runtime assertions. Offline acquisition, version-handshake specimens, and wider plugin-layout proof remain planned.

Run a specimen from the repository root after `bun install`:

```bash
dotnet run --project examples/dotnet-console
dotnet run --project examples/dotnet-console-custom-schema-path
dotnet run --project examples/dotnet-aspnet-mvc -- --dump-config
```