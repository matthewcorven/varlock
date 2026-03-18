# .NET DX Overhaul Control Set

This contract defines the first-wave control nodes that must stay green before wider `.NET` DX overhaul fan-out continues. It is subordinate to `docs/proposals/dotnet-support.md`, the current support ledger, and the existing NO-GO decisions on native runtime, Roslyn/analyzer expansion, and `.NET`-native plugin work.

## Control nodes

### DX-A1 — Baseline example

- Owner: O'Brien
- Reviewer: Picard
- Allowed surface:
  - `examples/dotnet-console/**`
  - `scripts/test-dotnet-proof.ts` for the baseline specimen registration and assertions only
  - example-local README and schema/value files required to make `examples/dotnet-console/` runnable and reviewable
- Explicit non-goals:
  - no sibling example creation; that belongs to `DX-A2a` through `DX-A2c`
  - no new package surface, runtime API, DI helper, or hosting helper changes
  - no target-framework support claim widening; a newer example TFM is implementation detail until CI and ledger proof say otherwise
  - no security, Serilog, reload, type-generation, or plugin claims beyond the baseline happy-path startup load
- Required artifacts:
  - `examples/dotnet-console/Program.cs`
  - `examples/dotnet-console/README.md`
  - `examples/dotnet-console/.env.schema`
  - runnable safe example values alongside the baseline example
  - a baseline assertion block in `scripts/test-dotnet-proof.ts`
- Proof commands:
  - `dotnet build examples/dotnet-console/dotnet-console.csproj`
  - `bun run proof:dotnet`
- Acceptance criteria:
  - the baseline example is the narrow happy-path app and proves only default configuration loading, not advanced feature stories
  - the baseline remains runnable from its own directory with repo-local executable discovery still exercised by proof
  - the README says exactly what the example proves and what it does not claim
  - proof asserts exact resolved values and confirms the schema source is present in the loaded graph or equivalent baseline output
  - `DX-X1` can sync docs and ledger language from the checked artifacts without inventing caveats
- Abort triggers:
  - the lane needs a new support-contract decision or wants to recommend an onboarding flow broader than the existing baseline boundaries
  - the example needs package/API changes outside its own directory plus the baseline proof hook
  - the lane starts carrying reload, logging, sensitive, or typed-config claims that belong to later nodes
- Status: **green** — implemented and proven
- Ready-now verdict: green. All required artifacts exist and proof passes. The baseline example is a .NET 10 single-file top-level statements app using `Host.CreateApplicationBuilder` + `AddVarlock()`. Program.cs emits exact resolved values (APP_NAME, HTTP_PORT, FEATURE_ENABLED) plus provider type and schema path. README explicitly scopes claims. Proof script (`bun run proof:dotnet`) verifies exact values, repo-local/package-local/.bin/PATH executable discovery, and provider wiring. `dotnet build` and `dotnet run` succeed from the example directory.

### DX-B1 — `WebApplicationBuilder.AddVarlock()` entry point

- Owner: Data
- Reviewer: Picard
- Status: **green** — implemented and tested
- Allowed surface:
  - `packages/dotnet/Varlock.Extensions.Hosting/**`
  - `packages/dotnet/Varlock.DotNet.Tests/**` for hosting-surface tests only
  - `examples/dotnet-aspnet-mvc/**` only where needed to prove the new builder entry point end-to-end
  - `scripts/test-dotnet-proof.ts` only where needed to keep the ASP.NET proof aligned with the new call site
- Explicit non-goals:
  - no metapackage, DI registration, typed binding helper, reload-default, or diagnostics work
  - no new package split unless Picard explicitly approves a package-boundary change
  - no hosting-semantics change; this lane is a thin public-shape convenience over existing configuration behavior
  - no docs/default-onboarding rewrite outside the handoff to `DX-X1`
- Required artifacts:
  - `WebApplicationBuilder` overloads that mirror the existing `HostApplicationBuilder` overload pair
  - test coverage proving the overload count and delegation behavior on the hosting package surface
  - an ASP.NET proof path using `builder.AddVarlock()` instead of `builder.Configuration.AddVarlock()`
- Proof commands:
  - `dotnet test packages/dotnet/Varlock.DotNet.Tests/Varlock.DotNet.Tests.csproj`
  - `bun run proof:dotnet`
- Acceptance criteria:
  - the new entry point is a thin delegating API and does not alter provider order, options, or runtime semantics
  - the ASP.NET example or focused proof path compiles and runs through the new builder surface
  - the lane adds no new claim beyond “ASP.NET apps can use `builder.AddVarlock()` as thin sugar over the existing configuration path”
  - `DX-X1` receives exact caveat text if any hosting README or docs wording must change
- Abort triggers:
  - implementing the API requires a new package, a non-trivial TFM split, or a product-contract decision about default install surface
  - the lane starts absorbing DI registration, typed binding, or metapackage work from later nodes
  - the proof path depends on a baseline example contract that `DX-A1` has not yet stabilized
- Ready-now verdict: **green**. Implementation complete: two `WebApplicationBuilder.AddVarlock()` overloads in `VarlockWebApplicationBuilderExtensions.cs` (net10.0 only, thin delegation to `builder.Configuration.AddVarlock()`), tests passing in `HostingExtensionsTests.cs`. Docs and ledger synced by DX-X1.

### DX-B3 — Static `Varlock.DotNet.Env.Load()` convenience entry point

- Owner: Data
- Reviewer: Picard
- Allowed surface:
  - `packages/dotnet/Varlock.DotNet/**`
  - `packages/dotnet/Varlock.DotNet.Tests/**`
  - no example or docs files unless Picard explicitly re-scopes the node to bundle a proving specimen with it
- Explicit non-goals:
  - no hidden global caching, singleton runtime registration, or DI semantics
  - no change to executable lookup, handshake, error categories, or bridge contract behavior
  - no attempt to make this the default teaching path before a proving specimen and `DX-X1` doc sync exist
  - no bundling of `IVarlockRuntime` registration, typed binding, or broader convenience surface from `DX-B4` or `DX-B6`
- Required artifacts:
  - a new static façade in `packages/dotnet/Varlock.DotNet/` exposing synchronous and asynchronous `Load` helpers over the existing runtime
  - focused tests proving default-option behavior, configured-option behavior, and async parity against the current runtime semantics
  - explicit handoff note for `DX-X1` if package docs are to remain non-recommended until a direct-load specimen exists
- Proof commands:
  - `dotnet test packages/dotnet/Varlock.DotNet.Tests/Varlock.DotNet.Tests.csproj`
- Acceptance criteria:
  - the façade is pure sugar over the existing `VarlockCliRuntime` behavior and preserves existing error and lookup semantics
  - the API remains narrow enough that a reviewer can compare it directly to the underlying runtime calls
  - tests prove sync and async entry points without requiring a broader example restructure in the same lane
  - any public wording stays subordinate to existing low-level/non-hosted positioning until a later specimen proves a broader onboarding claim
- Abort triggers:
  - the lane needs a new direct-load example to justify itself as a recommended user path; that is a cross-node dependency and should be routed through `DX-A2a`
  - the façade starts accumulating policy, caching, DI, or configuration-provider behavior instead of staying a thin wrapper
  - docs try to promote the API ahead of `DX-X1` proof/accounting updates
- Ready-now verdict: **green** (library complete, tests pass). Static `Env.Load()` façade is implemented in `packages/dotnet/Varlock.DotNet/Env.cs` with 3 sync and 2 async overloads, tests passing in `EnvStaticApiTests.cs`. Specimen pending — not docs-ready as a recommended onboarding path until a proving direct-load example exists and DX-X1 syncs the claim.

### DX-X1 — Proof/docs/ledger sync

- Owner: O'Brien
- Reviewer: Picard
- Allowed surface:
  - `docs/proposals/dotnet-support-ledger.yml`
  - `docs/proposals/dotnet-dx-overhaul.md`
  - `docs/proposals/dotnet-dx-overhaul-proof-plan.md`
  - affected example READMEs under `examples/**/README.md`
  - affected package READMEs under `packages/dotnet/**/README.md`
  - `scripts/test-dotnet-proof.ts` and CI wiring only when synchronizing already-implemented claims with automated proof
- Explicit non-goals:
  - no behavior changes whose only purpose is to make docs true after the fact
  - no moving any claim from planned to proven without a repeatable automated proof path
  - no widening of target-framework, plugin, analyzer, native-runtime, or executable-distribution claims beyond already-approved boundaries
  - no new public claim creation without a named upstream implementation node
- Required artifacts:
  - ledger rows or caveat updates for every landed control-node claim
  - README caveat text that exactly matches the automated proof boundary
  - proof-harness and CI updates where a shipped claim needs a new automated assertion path
  - an explicit “still planned” trail for any convenience API or example not yet proven enough to recommend
- Proof commands:
  - `bun run proof:dotnet`
  - `dotnet test packages/dotnet/Varlock.DotNet.Tests/Varlock.DotNet.Tests.csproj`
- Acceptance criteria:
  - every shipped first-wave claim has a matching ledger state, README statement, and automated proof reference
  - no docs page points to an unproven example or API as the default/recommended path
  - caveats remain visible after proof lands when the supported boundary is narrower than the intuitive marketing claim
  - the control set can be reviewed from repo artifacts alone without reconstructing intent from commits or chat history
- Abort triggers:
  - an implementation node has not yet produced repeatable proof artifacts
  - a planned-to-proven status move would rely on reviewer memory instead of a checked command
  - proposed wording contradicts `docs/proposals/dotnet-support.md`, the support ledger, or existing P4 NO-GO decisions
- Ready-now verdict: green. This node should run from day one and never wait for overhaul closeout.

### DX-A2a — First sibling example batch

- Owner: O'Brien
- Reviewer: Picard
- Status: **green** — all five sibling examples exist and are buildable
- Allowed surface:
  - `examples/dotnet-console-direct-load/**`
  - `examples/dotnet-console-sensitive/**`
  - `examples/dotnet-console-reload/**`
  - `examples/dotnet-console-serilog/**`
  - `examples/dotnet-console-typed-config/**`
- Explicit non-goals:
  - no new package surface or runtime API changes
  - no docs-ready onboarding claims until DX-X1 syncs the proving boundary
- Ready-now verdict: **green**. All five sibling examples exist under `examples/`: `dotnet-console-direct-load`, `dotnet-console-sensitive`, `dotnet-console-reload`, `dotnet-console-serilog`, `dotnet-console-typed-config`. Each demonstrates a specific feature story beyond the baseline. `bun run proof:dotnet` exercises the examples that have proof hooks.

### DX-A2b — Second sibling example batch

- Owner: O'Brien
- Reviewer: Picard
- Status: **green** — all five second-batch sibling examples exist and are buildable
- Allowed surface:
  - `examples/dotnet-console-custom-schema-path/**`
  - `examples/dotnet-console-custom-working-dir/**`
  - `examples/dotnet-console-environment-name/**`
  - `examples/dotnet-console-optional/**`
  - `examples/dotnet-console-custom-runtime/**`
  - `scripts/test-dotnet-proof.ts` for targeted sibling proof registration and assertions only
- Explicit non-goals:
  - no new package surface or runtime API changes
  - no claim that `EnvironmentName` alone changes CLI-backed file selection; the example proves provider-level propagation into `VarlockLoadOptions` via an injected runtime seam only
  - no claim that `Optional = true` proves later file appearance or reload behavior
  - no claim that custom runtime injection stands in for executable lookup or bridge-contract proof
- Ready-now verdict: **green**. All five sibling examples exist under `examples/`: `dotnet-console-custom-schema-path`, `dotnet-console-custom-working-dir`, `dotnet-console-environment-name`, `dotnet-console-optional`, and `dotnet-console-custom-runtime`. `bun run proof:dotnet` builds and runs each specimen with targeted assertions covering the narrow SchemaPath, WorkingDirectory, EnvironmentName propagation, Optional startup, and injected `IVarlockRuntime` seams.

### DX-A2c — Third sibling example batch

- Owner: O'Brien
- Reviewer: Tuvok
- Status: **green** — all eight third-batch sibling examples exist and are buildable
- Allowed surface:
  - `examples/dotnet-console-coercion/**`
  - `examples/dotnet-console-validation/**`
  - `examples/dotnet-console-public-only/**`
  - `examples/dotnet-console-exec/**`
  - `examples/dotnet-console-composition/**`
  - `examples/dotnet-console-di-options/**`
  - `examples/dotnet-console-explicit-executable/**`
  - `examples/dotnet-console-leak-prevention/**`
  - `scripts/test-dotnet-proof.ts` for targeted sibling proof registration and assertions only
- Explicit non-goals:
  - no new package surface or runtime API changes
  - no claim that the public-only specimen adds runtime protection; it proves generated-file filtering only
  - no claim that the exec specimen proves a general secret-manager story; it is scoped to the checked-in local command seam
  - no claim that the DI/options specimen establishes `AddVarlock<T>()` or generated-type binder support; it proves the current manual mapping pattern only
  - no claim that the leak-prevention specimen proves automatic interception or enforcement; it proves metadata-only `PreventLeaks` surfacing plus manual helper output
- Ready-now verdict: **green**. All eight sibling examples exist under `examples/`: `dotnet-console-coercion`, `dotnet-console-validation`, `dotnet-console-public-only`, `dotnet-console-exec`, `dotnet-console-composition`, `dotnet-console-di-options`, `dotnet-console-explicit-executable`, and `dotnet-console-leak-prevention`. `bun run proof:dotnet` builds and runs each specimen with targeted assertions covering coercion, expected validation failure, public-only generated-file filtering, the local `exec()` command seam, schema reference composition, the manual DI/options pattern, explicit executable override, and metadata-only leak-prevention surfacing.

### DX-B2 — Varlock metapackage

- Owner: Data
- Reviewer: Picard
- Status: **green** — metapackage created and builds
- Allowed surface:
  - `packages/dotnet/Varlock/**`
- Explicit non-goals:
  - no source code; this is a dependency-only bundle
  - Varlock.Serilog deliberately excluded as opt-in
  - Varlock.SourceGeneration excluded (pulled transitively by MSBuild)
- Required artifacts:
  - `packages/dotnet/Varlock/Varlock.csproj` with ProjectReference to Varlock.DotNet, Varlock.Extensions.Configuration, Varlock.Extensions.Hosting, and Varlock.MSBuild
  - `packages/dotnet/Varlock/README.md` documenting bundled packages and opt-in Serilog exclusion
- Proof commands:
  - `dotnet build packages/dotnet/Varlock/Varlock.csproj`
- Acceptance criteria:
  - the metapackage builds with zero warnings and produces no output assembly (`IncludeBuildOutput=false`)
  - a consumer referencing only `Varlock` transitively obtains all four core packages
- Ready-now verdict: **green**. Metapackage csproj and README created. `dotnet build` succeeds with 0 warnings. No source code — pure dependency bundle.

### DX-B4 — DI registration for IVarlockRuntime

- Owner: Data
- Reviewer: Picard
- Status: **green** — implemented and tested
- Allowed surface:
  - `packages/dotnet/Varlock.Extensions.Hosting/**`
  - `packages/dotnet/Varlock.DotNet.Tests/**` for DI-specific tests only
- Explicit non-goals:
  - no scoped or transient lifetime; singletons only via TryAddSingleton
  - no generated-type aware binding semantics beyond standard `Configure<TConfig>(builder.Configuration)`
- Required artifacts:
  - `TryAddSingleton<IVarlockRuntime>(runtime)` registered when `AddVarlock()` is called
  - `TryAddSingleton<VarlockResolvedGraph>` factory registered using the graph from the configuration source
  - DI registration shared between `HostApplicationBuilder` and `WebApplicationBuilder` extensions via internal `RegisterServices` method
  - test coverage proving registration, TryAdd semantics (no overwrite of pre-registered services), null-runtime default, and WebApplicationBuilder parity
- Proof commands:
  - `dotnet test packages/dotnet/Varlock.DotNet.Tests/Varlock.DotNet.Tests.csproj --filter HostingExtensionsTests`
- Acceptance criteria:
  - `IVarlockRuntime` is injectable from DI after calling `AddVarlock()`
  - `VarlockResolvedGraph` is injectable and lazily resolved from the configuration source
  - TryAddSingleton semantics mean pre-registered services are not overwritten
  - WebApplicationBuilder and HostApplicationBuilder extensions both register the same DI services
- Ready-now verdict: **green**. Implementation complete in both hosting extension files. Internal `RegisterServices` method shared between both builder types. 5 new tests pass in `HostingExtensionsTests.cs`.

### DX-B6 — `AddVarlock<TConfig>()` convenience overloads

- Owner: Data
- Reviewer: Picard
- Status: **green** — implemented and tested
- Allowed surface:
  - `packages/dotnet/Varlock.Extensions.Hosting/**`
  - `packages/dotnet/Varlock.DotNet.Tests/**` for hosting/options tests only
- Explicit non-goals:
  - no generated-type specific binder behavior; this is standard options binding only
  - no reload semantic changes; existing `IOptionsMonitor<T>` behavior remains unchanged
- Required artifacts:
  - `HostApplicationBuilder.AddVarlock<TConfig>()` and `HostApplicationBuilder.AddVarlock<TConfig>(Action<VarlockConfigurationSource>)`
  - `WebApplicationBuilder.AddVarlock<TConfig>()` and `WebApplicationBuilder.AddVarlock<TConfig>(Action<VarlockConfigurationSource>)` on net10.0
  - generic overloads delegate to existing `AddVarlock(...)` and then call `services.Configure<TConfig>(configuration)`
  - tests proving overload surface and options binding for both host and web builder paths
- Proof commands:
  - `dotnet test packages/dotnet/Varlock.DotNet.Tests/Varlock.DotNet.Tests.csproj --filter HostingExtensionsTests`
- Acceptance criteria:
  - generic overloads exist on both builder extension classes without changing existing overload behavior
  - options binding runs through the default .NET options binder with no custom binder semantics
  - `HostingExtensionsTests` proves overload count and end-to-end DI options binding
- Ready-now verdict: **green**. Generic `AddVarlock<TConfig>` overloads are implemented for host and web builders and covered by `HostingExtensionsTests`.

### DX-B8 — Actionable error messages

- Owner: Data
- Reviewer: Picard
- Status: **green** — implemented and tested
- Allowed surface:
  - `packages/dotnet/Varlock.DotNet/**`
  - `packages/dotnet/Varlock.DotNet.Tests/**` for error message tests only
- Explicit non-goals:
  - no schema parse error translation (depends on CLI stderr structured output)
  - no validation failure enhancement beyond what the CLI bridge already provides
- Required artifacts:
  - `CreateExecutableNotFoundException` method listing searched path categories and install suggestion
  - `CreateMissingPayloadException` with stderr content inclusion when available
  - test coverage for searched paths, disabled lookup messages, and stderr inclusion
- Proof commands:
  - `dotnet test packages/dotnet/Varlock.DotNet.Tests/Varlock.DotNet.Tests.csproj --filter BridgeContractAlignmentTests`
- Acceptance criteria:
  - executable-not-found errors list which path categories were searched and suggest `npm install --save-dev varlock`
  - both-lookups-disabled errors explain that all lookup paths are disabled
  - missing payload errors include trimmed CLI stderr when available
  - all error messages are actionable — they tell the user what happened and what to do
- Ready-now verdict: **green**. `CreateExecutableNotFoundException` lists searched path categories (node_modules, packages, PATH) based on enabled options plus install suggestion. `CreateMissingPayloadException` includes stderr (trimmed to 500 chars). 4 new tests pass in `BridgeContractAlignmentTests.cs`.

### DX-B5 — `[VarlockSensitive]` attribute on generated properties

- Owner: Data
- Reviewer: Picard
- Status: **green** — attribute defined and emitted by typegen
- Allowed surface:
  - `packages/dotnet/Varlock.DotNet/**` for attribute definition only
  - `packages/varlock/src/env-graph/lib/type-generation.ts` for C# emission
  - `packages/varlock/src/env-graph/test/**` for test fixtures and assertions
- Explicit non-goals:
  - no runtime enforcement or ASP.NET Data Protection integration
  - no automatic JSON serialization filtering; consumers decide how to use the attribute
  - no change to `VarlockConfigMetadata.SensitiveKeys` or `PropertyBinding.IsSensitive` — the attribute is additive
- Required artifacts:
  - `VarlockSensitiveAttribute` in `Varlock.DotNet` with `[AttributeUsage(AttributeTargets.Property)]`
  - C# type generation emits `[global::Varlock.DotNet.VarlockSensitive]` on properties where `isSensitive === true`
  - attribute is not emitted in `publicOnly=true` mode (sensitive items are stripped entirely)
  - golden fixture files updated to include the attribute on sensitive properties
- Proof commands:
  - `dotnet build packages/dotnet/Varlock.DotNet/Varlock.DotNet.csproj`
  - `bun run --filter varlock test:ci -- type-generation`
- Acceptance criteria:
  - the attribute is a passive metadata marker with no runtime behavior
  - generated C# output includes `[global::Varlock.DotNet.VarlockSensitive]` on sensitive properties only
  - `publicOnly=true` mode is unaffected (sensitive items already excluded)
  - existing `SensitiveKeys` and `PropertyBinding.IsSensitive` metadata remains unchanged
  - consumers can use reflection to discover sensitive properties without referencing the metadata class
- Ready-now verdict: **green**. `VarlockSensitiveAttribute` defined in `Varlock.DotNet`. C# type generation emits `[global::Varlock.DotNet.VarlockSensitive]` on sensitive properties. Golden fixtures updated. All 36 type-generation tests pass. All 50 .NET tests pass.

## Control-set board

Artifact-backed board rows for the active control set and the adjacent teaching-surface sequencing nodes. Statuses below are limited to what the current repository state can prove.

| node | lane | status | owner | reviewer | proof artifact | definition of done | blocked by | last meaningful update | next gate |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `DX-A1` | Teaching surface | done | O'Brien | Picard | `examples/dotnet-console/**`; baseline assertions in `scripts/test-dotnet-proof.ts`; `bun run proof:dotnet` | Baseline console example stays the narrow happy path, remains runnable from its own directory, and its README/proof text are specific enough for `DX-X1` to sync without guesswork | none | Baseline example, README, schema, and proof hooks are checked in and already called green in this control set | Gate 2 — Track A teaching surface credible |
| `DX-A2a` | Teaching surface | done | O'Brien | Tuvok | `examples/dotnet-console-direct-load/**`, `examples/dotnet-console-sensitive/**`, `examples/dotnet-console-reload/**`, `examples/dotnet-console-serilog/**`, `examples/dotnet-console-typed-config/**`; sibling proof hooks in `scripts/test-dotnet-proof.ts` | First sibling batch stays example-only, each example has its own README/schema/safe values, and the checked claims stay aligned with `bun run proof:dotnet` plus `DX-X1` caveat sync | none | Five sibling example directories plus build/run proof hooks are present; ledger row `dx-a2a-sibling-batch` marks the batch complete/proven | Gate 2 — Track A teaching surface credible |
| `DX-A2b` | Teaching surface | done | O'Brien | Picard | `examples/dotnet-console-custom-schema-path/**`, `examples/dotnet-console-custom-working-dir/**`, `examples/dotnet-console-environment-name/**`, `examples/dotnet-console-optional/**`, `examples/dotnet-console-custom-runtime/**`; targeted sibling proof hooks in `scripts/test-dotnet-proof.ts`; `bun run proof:dotnet` | Second sibling batch stays example-only, each example has its own README/schema/safe values or explicit missing-entry setup, and the checked claims stay aligned with `bun run proof:dotnet` plus `DX-X1` caveat sync | none | Five second-batch sibling example directories plus build/run proof hooks are present; ledger row `dx-a2b-sibling-batch` marks the batch complete/proven | Gate 2 — Track A teaching surface credible |
| `DX-A2c` | Teaching surface | done | O'Brien | Tuvok | `examples/dotnet-console-coercion/**`, `examples/dotnet-console-validation/**`, `examples/dotnet-console-public-only/**`, `examples/dotnet-console-exec/**`, `examples/dotnet-console-composition/**`, `examples/dotnet-console-di-options/**`, `examples/dotnet-console-explicit-executable/**`, `examples/dotnet-console-leak-prevention/**`; targeted proof hooks in `scripts/test-dotnet-proof.ts`; `bun run proof:dotnet` | Third sibling batch stays example-only, each example has its own README/schema/safe values or explicit failure setup, and the checked claims stay aligned with `bun run proof:dotnet` plus DX-X1 caveat sync | none | Eight third-batch sibling example directories plus build/run proof hooks are present; ledger row `dx-a2c-sibling-batch` marks the batch complete/proven | Gate 2 — Track A teaching surface credible |
| `DX-B1` | Library surface | done | Data | Picard | `packages/dotnet/Varlock.Extensions.Hosting/**`; `HostingExtensionsTests`; ASP.NET proof path in `bun run proof:dotnet` | `WebApplicationBuilder.AddVarlock()` remains thin sugar over existing configuration behavior, compiles/runs through the ASP.NET example, and stays within the current support boundary | none | `VarlockWebApplicationBuilderExtensions.cs` and focused hosting tests are landed; DX-X1 already synced docs and ledger caveats | Gate 3 — Track B library surface credible |
| `DX-B3` | Library surface | done | Data | Picard | `packages/dotnet/Varlock.DotNet/Env.cs`; `EnvStaticApiTests`; ledger row `dx-env-static-load` | Static `Env.Load()` stays pure sugar over `VarlockCliRuntime`, preserves lookup and error semantics, and does not become the recommended default path before a proving specimen exists | direct-load specimen is still pending if the API is to be taught as a recommended path | Static API implementation and tests are landed; ledger caveat explicitly says specimen pending under `DX-A2a` | Gate 3 — Track B library surface credible |
| `DX-B6` | Library surface | done | Data | Picard | `packages/dotnet/Varlock.Extensions.Hosting/VarlockHostApplicationBuilderExtensions.cs`; `packages/dotnet/Varlock.Extensions.Hosting/VarlockWebApplicationBuilderExtensions.cs`; `packages/dotnet/Varlock.DotNet.Tests/HostingExtensionsTests.cs` | Generic `AddVarlock<TConfig>()` overloads on both host and web builders wire existing AddVarlock configuration plus standard options binding without altering runtime semantics | none | Four generic overloads are present (host/web, parameterless/configure) and `HostingExtensionsTests` proves options binding for both builder types | Gate 3 — Track B library surface credible |
| `DX-X1` | Proof and support claims | in progress | O'Brien | Picard | `docs/proposals/dotnet-support-ledger.yml`; `docs/proposals/dotnet-dx-overhaul.md`; affected READMEs; `bun run proof:dotnet` | Every shipped overhaul claim has matching ledger state, README caveat text, and automated proof reference before it is treated as accepted | blocked only by any implementation lane that lands behavior without proof/docs sync | Control-set claims for `DX-A1`, `DX-B1`, `DX-B3`, `DX-B6`, `DX-A2a`, `DX-A2b`, and `DX-A2c` are now reflected in the current ledger/control-set snapshot; later lanes remain subject to the same proof-first sync | Gate 4 — Overhaul closeout credible |

## Immediate execution verdict

- Implemented and proven: `DX-A1` (baseline console example, proof passes), `DX-B1` (WebApplicationBuilder extensions, tests pass), `DX-B3` (static Env.Load, tests pass, specimen pending), `DX-A2a` (first sibling batch, all 5 examples exist), `DX-A2b` (second sibling batch, all 5 examples exist), `DX-A2c` (third sibling batch, all 8 examples exist and are proof-backed), `DX-B2` (metapackage created and builds), `DX-B4` (DI registration, 5 tests pass), `DX-B6` (generic `AddVarlock<TConfig>()` overloads implemented and tested), `DX-B8` (actionable errors, 4 tests pass), `DX-B5` ([VarlockSensitive] attribute, defined and emitted)
- Immediately executable now: `DX-X1` (ongoing)
- Docs sync completed for all first-wave control nodes (`DX-A1`, `DX-B1`, `DX-B3`, `DX-B6`, `DX-A2a`, `DX-A2b`, `DX-A2c`, `DX-B2`, `DX-B4`, `DX-B5`, `DX-B8`) by `DX-X1`

## Fan-out gaps before broader Wiggum spawning

- `DX-B3` may execute now, but `DX-X1` must keep any package-doc wording narrow until a later direct-load specimen exists.
- `DX-B1` should not be bundled with baseline-example rewrite work; that would hide two promises in one autonomous run and violate the control-set intent.
