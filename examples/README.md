# .NET Proof Examples

This directory holds the first runnable proof artifacts for Varlock's `.NET` support proposal.

Current slice only proves:

- `examples/dotnet-console-net8/`: direct `Varlock.DotNet` runtime usage from a console app.
- `examples/dotnet-aspnet-mvc-net8/`: startup-only `Varlock.Extensions.Configuration` usage from an ASP.NET Core app layered over `appsettings.json`.

These examples currently exercise four executable-acquisition paths:

1. the example working directory walks up to `packages/varlock/bin/cli.js` without an explicit `ExecutablePath`
2. the `bun run proof:dotnet` harness temporarily drops `node_modules/varlock/bin/cli.js` into the console example and proves package-local acquisition against the real app before cleaning up
3. the `bun run proof:dotnet` harness temporarily drops `node_modules/.bin/varlock` into the console example and proves local `.bin` acquisition when the package-local layout is absent
4. the `bun run proof:dotnet` harness prepends a temporary `varlock` entry to `PATH` and sets `VARLOCK_DOTNET_PROOF_FORCE_PATH_LOOKUP=1`, which disables local lookup only for that proof run and proves the opt-in `PATH` branch against the real app

The automated proof path in `bun run proof:dotnet` currently covers all four cases. Offline acquisition, version-handshake specimens, and wider plugin-layout proof remain planned.

That same proof command now also performs an explicit `dotnet build` for both checked-in examples before the runtime assertions, which is the current narrow P1-A2 build-flow proof row.

The ASP.NET proof also exercises User Secrets coexistence in Development: it writes a User Secrets-only key plus an overlapping `APP_NAME`, then verifies that `AddVarlock(...)` preserves the extra User Secrets value while overriding overlapping keys by provider order.

Run either example from the repository root after `bun install`:

```bash
dotnet run --project examples/dotnet-console-net8
dotnet run --project examples/dotnet-aspnet-mvc-net8 -- --dump-config
```