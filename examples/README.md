# .NET Examples

This directory holds the runnable Varlock `.NET` examples.

## Console examples

The console set starts with `dotnet-console` as the narrow happy-path baseline, then branches into focused siblings:

- loading and typing: `dotnet-console-direct-load`, `dotnet-console-typed-config`, `dotnet-console-coercion`, `dotnet-console-validation`
- runtime setup and lookup: `dotnet-console-custom-schema-path`, `dotnet-console-custom-working-dir`, `dotnet-console-custom-runtime`, `dotnet-console-explicit-executable`, `dotnet-console-environment-name`, `dotnet-console-optional`
- reload and options patterns: `dotnet-console-reload`, `dotnet-console-di-options`, `dotnet-console-options-snapshot`, `dotnet-console-options-monitor`
- sensitive and public-only boundaries: `dotnet-console-sensitive`, `dotnet-console-public-only`, `dotnet-console-leak-prevention`
- logging, composition, and command-backed values: `dotnet-console-serilog`, `dotnet-console-composition`, `dotnet-console-exec`

## Hosted and UI examples

The hosted and UI set stays narrower than the console siblings and focuses on the main entry points:

- `dotnet-aspnet-mvc` shows `WebApplicationBuilder.AddVarlock()` plus coexistence with `appsettings.json` and User Secrets
- `dotnet-worker` shows `HostApplicationBuilder.AddVarlock()` inside a hosted service setup
- `dotnet-functions-isolated` shows configuration layering with `local.settings.json`
- `dotnet-blazor-server` shows server-side component access through `IConfiguration`
- `dotnet-winforms` shows direct `VarlockCliRuntime.Load()` usage in desktop code
- `dotnet-blazor-wasm-public` shows the build-time `publicOnly=true` boundary

## Shared reference material

Shared reference material lives under `examples/dotnet-shared/`:

- `.env.schema.reference` is the cheat sheet for the `@env-spec` decorator and schema-pattern surface the current `.NET` examples exercise
- `dotnet-shared/` is not a runnable project and is not part of the automated validation run; it exists to keep the example-local schema guidance close to the specimens

## Cloud provider recipes

Azure Key Vault, AWS Secrets Manager and Parameter Store, and Google Secret Manager examples currently live in the website docs rather than in the runnable example tree.

- `packages/varlock-website/src/content/docs/integrations/dotnet/cloud-providers.mdx` shows the `.NET` + Varlock patterns for Azure, AWS, and GCP
- these are documentation recipes, not automated proof apps, because they require live cloud accounts and credentials

## Automation notes

`bun run proof:dotnet` validates the baseline console example, the hosted and UI examples above, and the focused console siblings. It also performs explicit `dotnet build` checks before the runtime assertions.

The console baseline exercises these executable-acquisition paths during automated validation:

1. repo-local lookup to `packages/varlock/bin/cli.js`
2. test-only package-local `node_modules/varlock/bin/cli.js`
3. test-only local `node_modules/.bin/varlock`
4. opt-in `PATH` lookup guarded by `VARLOCK_DOTNET_PROOF_FORCE_PATH_LOOKUP=1`

Offline acquisition, version-handshake specimens, and wider plugin-layout validation remain planned.

Run a specimen from the repository root after `bun install`:

```bash
dotnet run --project examples/dotnet-console
dotnet run --project examples/dotnet-console-coercion
dotnet run --project examples/dotnet-console-custom-schema-path
dotnet run --project examples/dotnet-console-explicit-executable
dotnet build --project examples/dotnet-console-public-only && dotnet run --project examples/dotnet-console-public-only
dotnet run --project examples/dotnet-aspnet-mvc -- --dump-config
```
