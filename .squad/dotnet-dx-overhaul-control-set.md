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
  - no typed binding (`AddVarlock<T>()`) — that belongs to DX-B6
  - no scoped or transient lifetime; singletons only via TryAddSingleton
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

## Immediate execution verdict

- Implemented and proven: `DX-A1` (baseline console example, proof passes), `DX-B1` (WebApplicationBuilder extensions, tests pass), `DX-B3` (static Env.Load, tests pass, specimen pending), `DX-A2a` (first sibling batch, all 5 examples exist), `DX-B2` (metapackage created and builds), `DX-B4` (DI registration, 5 tests pass), `DX-B8` (actionable errors, 4 tests pass)
- Immediately executable now: `DX-X1` (ongoing), `DX-B5` ([VarlockSensitive] attribute)
- Docs sync completed for DX-B1 and DX-B3 by DX-X1; sync pending for DX-A2a, DX-B2, DX-B4, DX-B8

## Fan-out gaps before broader Wiggum spawning

- The coordinator still needs one board row per control node carrying the exact fields already mandated in oversight: node, lane, status, owner, reviewer, proof artifact, definition of done, blocked by, last meaningful update, and next gate.
- `DX-B3` may execute now, but `DX-X1` must keep any package-doc wording narrow until a later direct-load specimen exists.
- `DX-B1` should not be bundled with baseline-example rewrite work; that would hide two promises in one autonomous run and violate the control-set intent.
