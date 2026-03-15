# Squad Decisions

## Active Decisions

### 2026-03-13: First `.NET` implementation slice boundary

- Initiative: `dotnet-support`
- Source: Picard
- Decision: The first implementation slice is limited to phase-0 closure plus the minimum phase-1 work that can start immediately without leaking into hosting, reload, native runtime, analyzer expansion, or broader platform parity.
- Start now: Tuvok owns the machine-readable bridge contract and fixtures, O'Brien owns the executable-distribution specimen, Geordi owns the C# generation specimen, and Data starts the initial `Varlock.DotNet` plus `Varlock.Extensions.Configuration` bridge skeleton once contract and acquisition outputs are concrete.
- Explicitly deferred: `Varlock.Extensions.Hosting`, reload or watch semantics, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`, Serilog helpers, Azure Functions isolated, Blazor variants, native runtime, analyzer evolution, and `.NET` parity work for `varlock run` or `varlock scan`.

### 2026-03-13: First `.NET` bridge slice shape

- Initiative: `dotnet-support`
- Source: Data
- Decision: The first code-bearing `.NET` slice is a startup-only path from the existing `varlock` CLI machine contract into standard `.NET` configuration.
- Package shape: keep CLI hardening in `packages/varlock`, add `packages/dotnet/Varlock.DotNet` for executable resolution, process invocation, DTOs, and typed bridge errors, and add `packages/dotnet/Varlock.Extensions.Configuration` for `IConfigurationSource`, `IConfigurationProvider`, and `AddVarlock(...)`.
- Smallest viable deliverable: `ConfigurationBuilder().AddJsonFile(...).AddVarlock(...)` works at startup, Varlock-backed keys flatten into configuration, Varlock overrides `appsettings` by provider order, structured bridge failures surface for missing executable, incompatible version, missing schema, invalid schema, and resolution failure, and `optional: true` can yield an empty provider when the schema entry point is absent.
- Explicitly deferred: hosting helpers, reload or watch behavior, options-monitor semantics, managed executable distribution automation, source generation or MSBuild integration, and broader platform examples.

### 2026-03-13: First CLI bridge contract slice

- Initiative: `dotnet-support`
- Source: Tuvok
- Decision: The first supported machine-readable bridge entry point is `varlock load --format json-full --bridge-contract 1`.
- Transport: in bridge mode, the CLI emits JSON envelopes on stdout for both success and failure and uses the process exit code for status.
- Success shape: `{ contractVersion, cliVersion, command, format, ok: true, graph }`.
- Failure shape: `{ contractVersion, cliVersion, command, format, ok: false, category, message, ... }`.
- Proven failure categories: `executable-version-mismatch`, `schema-missing`, `schema-invalid`, `resolution-failed`, `plugin-load-failed`, and `bridge-internal-error`.
- Caller boundary: `executable-not-found` remains a caller-side bridge error because that failure happens before the CLI starts.

### 2026-03-13: Bridge scaffold flattening and parser seam

- Initiative: `dotnet-support`
- Source: Data
- Decision: The startup-only configuration scaffold will flatten structured Varlock values into standard `IConfiguration` child keys using `:` delimiters and will not emit duplicate root JSON-string entries for object or array values.
- Bridge contract alignment: `Varlock.DotNet` invokes `varlock load --format json-full --bridge-contract 1` and treats the stdout JSON envelope as the authoritative post-launch contract for both success and failure paths.
- Parser seam: the low-level runtime parser keeps a narrow internal fallback for the legacy unversioned raw `json-full` graph only when bridge-envelope markers are absent, while reserving explicit contract-version gating for the machine-readable handshake.
- Failure mapping: once the CLI starts, bridge failures map from the envelope category and location fields; only launch-time discovery remains a caller-side `executable-not-found` bridge error.
- Why: standard `.NET` binding stays predictable when structured values look like normal configuration sections, and the bridge can execute against today's CLI output without freezing premature public version-negotiation behavior.

### 2026-03-13: First `.NET` proof obligations

- Initiative: `dotnet-support`
- Source: O'Brien
- Decision: The first `.NET` implementation slice is only acceptable if it includes the minimum proof and release scaffolding needed to keep phase-1 claims honest.
- Required in-slice proof: a defined executable-distribution specimen for local development and CI, machine-readable contract-fixture coverage for success and named failure categories, proving examples for a console app and an ASP.NET Core MVC app, and at least one CI path that exercises executable discovery plus a proving example.
- Runnable proof shape: the first follow-up proof slice lands only `examples/dotnet-console-net8/` for direct `Varlock.DotNet` runtime usage and `examples/dotnet-aspnet-mvc-net8/` for startup-only `Varlock.Extensions.Configuration` ordering over `appsettings.json`.
- CI and task wiring: `bun run proof:dotnet` is the single repository proof command, and the main `test.yaml` workflow runs it on Ubuntu with the .NET SDK installed.
- Executable acquisition boundary: this slice proves the checked-in repo-local development lookup from the example working directories to `packages/varlock/bin/cli.js` without an explicit `ExecutablePath`, but it does not yet prove package-local installs, `node_modules/.bin`, offline acquisition, package-managed assets, broader plugin-layout support, opt-in `PATH` fallback, or a platform matrix.
- Documentation constraint: support-matrix and docs claims must be limited to what those proof artifacts actually demonstrate; proven rows stop at console direct runtime, ASP.NET startup-provider usage, and `dotnet run` startup discovery.
- Explicitly deferred: broader platform matrix proof, hosted reload behavior, worker or options-monitor proof, Azure Functions isolated, Blazor variants, and wider legacy-target validation.

### 2026-03-13: P1-A1 executable lookup and handshake hardening

- Initiative: `dotnet-support`
- Node: `P1-A1`
- Source: Data
- Decision: `Varlock.DotNet` hardens executable acquisition by preferring explicit configuration, then package-local and repo-local development layouts, and only falling back to `PATH` when that behavior is explicitly enabled.
- Lookup order: explicit `ExecutablePath`, package-local `node_modules/varlock/bin/cli.js`, local `node_modules/.bin/varlock`, repo-local `packages/varlock/bin/cli.js`, then `PATH` when enabled.
- Handshake: before the normal `load`, the runtime performs a preflight compatibility probe via `load --bridge-contract 0 --compact` and requires the CLI to answer with the existing `executable-version-mismatch` envelope that names supported contract version `1`.
- Failure boundary: launch-time discovery remains caller-side `executable-not-found`; once the executable starts, absent or incompatible handshake behavior maps to `executable-version-mismatch`, and only the later bridge `load` parses post-launch success or failure envelopes for schema or resolution diagnostics.
- Why: this advances executable acquisition hardening beyond the explicit repo-relative proof path without inventing a second protocol or broadening into hosting or packaging automation.

### 2026-03-13: P1-A1 lookup proof harnesses and narrowed executable claims

- Initiative: `dotnet-support`
- Node: `P1-A1`
- Source: Data and O'Brien
- Decision: prove package-local, local `node_modules/.bin/varlock`, and opt-in `PATH` executable acquisition with proof-only harnesses inside `bun run proof:dotnet` instead of widening the checked-in examples or broadening documentation beyond what the repository actually exercises by default.
- Proof shape: temporarily create `examples/dotnet-console-net8/node_modules/varlock/bin/cli.js` and `examples/dotnet-console-net8/node_modules/.bin/varlock`, delegate each wrapper to the real repo CLI, and assert distinct markers so proof can distinguish package-local and local `.bin` acquisition from repo-local fallback; for `PATH`, prepend a temporary `varlock` entry and set `VARLOCK_DOTNET_PROOF_FORCE_PATH_LOOKUP=1` so the console example disables local lookup only for that proof run.
- Proven surface now: checked-in proof covers repo-local, package-local, local `.bin`, and opt-in `PATH` lookup.
- Still planned: offline acquisition, version-handshake specimens, and broader packaging-layout proof remain follow-on work outside this node.
- Why: this closes the executable-acquisition branch of P1-A1 while keeping default example behavior narrow and support claims pinned to exercised layouts.

### 2026-03-13: First `lang=cs` specimen shape

- Initiative: `dotnet-support`
- Source: Geordi
- Decision: The first mergeable `lang=cs` specimen stays isolated inside the existing Varlock type-generation path in `packages/varlock` and does not depend on the current `.NET` bridge packages.
- Scope: generate a flat `Varlock.Generated.VarlockConfig` POCO from schema metadata, use PascalCase property names derived from env keys, preserve original env keys and sensitive-key membership in a generated metadata sidecar, and treat the checked-in `.env.schema` plus `.g.cs` golden file as the regression artifact.
- Explicitly deferred: nested object projection rules, binder-validation proof in a compiled `.NET` example, custom C# attributes for sensitive metadata, and MSBuild or package integration.
- Why: this opens a real `lang=cs` implementation seam now, keeps deterministic output under the existing typegen flow, and avoids colliding with bridge and MSBuild work before naming and binder proof are fully locked.

### 2026-03-13: C# typegen binder-prep metadata shape

- Initiative: `dotnet-support`
- Node: `P1-B1`
- Source: Geordi
- Decision: deepen the initial `lang=cs` specimen by emitting dependency-free binding metadata in `VarlockConfigMetadata` instead of taking a direct dependency on `Microsoft.Extensions.Configuration` binder attributes in this slice.
- Output shape: generated C# includes a `PropertyBindings` list whose entries preserve the original env key, generated property name, static required-ness, and sensitive status.
- Rationale: this keeps the specimen isolated inside the existing Varlock type-generation seam, avoids forcing a compile-time Microsoft.Extensions dependency onto every generated-type consumer, and gives `P2-B1` stable metadata for later binder or MSBuild glue without re-deriving names from schema.
- Deferred: direct `ConfigurationKeyNameAttribute` emission, binder package coupling, and generated runtime helpers that assume the bridge or MSBuild packages are already present.

### 2026-03-13: P1-B1 deterministic C# namespace and type naming overrides

- Initiative: `dotnet-support`
- Node: `P1-B1`
- Source: Geordi
- Decision: allow `@generateTypes(lang=cs, ...)` to accept deterministic `namespace` and `typeName` overrides, while deriving the metadata sidecar name as `${typeName}Metadata`.
- Validation: override validation stays inside `packages/varlock` type generation, and invalid overrides fail early with explicit generator errors.
- Boundaries: no binder package dependency or MSBuild target is introduced in this slice.
- Why: this gives teams a stable application-facing naming seam now and preserves a low-friction handoff into later binder and MSBuild work.

### 2026-03-13: P1 proof-accounting guardrails

- Initiative: `dotnet-support`
- Source: O'Brien
- Decision: `P1-A1` and `P1-B1` may advance as in-progress implementation lanes without silently widening the repository's proven support claims.
- Proof accounting: `P1-A1` is now repository-backed across the full intended v1 acquisition order: repo-local development lookup, package-local install layout, local `node_modules/.bin`, and opt-in `PATH` fallback all have checked-in proof via `bun run proof:dotnet` plus targeted runtime tests.
- Support-matrix constraint: executable-acquisition claims may be marked proven only for those exercised branches; offline acquisition, version-handshake specimens, and broader packaging-layout proof remain planned. C# generation remains planned in the support matrix until an example app consumes generated output or compiled binder validation exists.
- Progression alignment: the progression board should treat `P1-A1` as done and move attention to `P1-A2` and `P1-B1`.

### 2026-03-13: P1-A2 first proven proof-matrix rows

- Initiative: `dotnet-support`
- Node: `P1-A2`
- Source: Data and O'Brien
- Decision: the first accepted `P1-A2` expansion proves two narrow developer-experience rows on top of the existing console and ASP.NET examples: explicit `dotnet build` success for both checked-in examples, and ASP.NET User Secrets coexistence in Development.
- Proof command: `bun run proof:dotnet` must first run real `dotnet build` for `examples/dotnet-console-net8/` and `examples/dotnet-aspnet-mvc-net8/`, assert the expected `bin/Debug/net8.0/*.dll` outputs exist, then run the existing runtime assertions with `--no-build`.
- User Secrets boundary: the ASP.NET proof uses the example's `UserSecretsId`, writes a User Secrets-only key and an overlapping `APP_NAME`, and proves that `AddVarlock(...)` preserves the User Secrets-only key while still overriding overlapping values by provider order.
- Still deferred inside `P1-A2`: watch-mode behavior, IDE or IntelliSense observations beyond clean MSBuild compilation, options binding proof, wider platform coverage, Azure Functions local settings coexistence, and any generated-file/MSBuild integration beyond plain build success for the checked-in examples.
- Why: both rows are exercised against checked-in examples and stay within the current startup-only bridge slice without promoting broader hosting or tooling claims.

### 2026-03-13: P1-A2 machine-readable diagnostics proof uses shared CLI fixtures

- Initiative: `dotnet-support`
- Node: `P1-A2`
- Source: Picard and Tuvok
- Decision: the current machine-readable diagnostics proof row is satisfied by consuming the existing CLI load-bridge JSON fixtures directly from the `.NET` bridge-alignment tests instead of maintaining duplicate payload copies.
- Proof artifact boundary: the shared fixture set is the canonical proof artifact for success and failure envelope alignment across the CLI and `.NET` bridge tests, including a shared location-bearing `schema-invalid` parse-error envelope.
- Current proven surface: the shared fixtures now cover category and message fidelity, handshake compatibility, and location-bearing schema-parse diagnostics across both CLI and `.NET` bridge tests.
- Why: one checked-in machine contract keeps proof accounting honest and prevents the `.NET` lane from silently drifting away from the CLI bridge semantics it claims to prove.

### 2026-03-13: Progression-board prompt requirement

- Initiative: `dotnet-support`
- Source: Matthew Corven via Copilot
- Decision: When asking Matthew about work progression, prioritization, delegation, creation, or similar next-step choices, agents must present a visual progression reference and point to the relevant progression line or stable node IDs.
- Board: `.squad/progression.md` is the stable progression reference for the current `.NET` initiative.
- Why: next-step prompts should stay grounded in a visible progression map with minimal additional process.

### 2026-03-15: P1-B1 example-backed generated-type binder proof

- Initiative: `dotnet-support`
- Node: `P1-B1`
- Source: Geordi via Copilot
- Decision: `P1-B1` is now considered done for the current slice because `bun run proof:dotnet` generates the ASP.NET example's C# specimen from `@generateTypes(lang=cs, ...)`, validates deterministic namespace/type-name overrides, compiles the example, and exercises binder-based projection through generated metadata and a normal configuration-binder flow.
- Proof path: `examples/dotnet-aspnet-mvc-net8/.env.schema` + `examples/dotnet-aspnet-mvc-net8/Generated/AppConfig.g.cs` + `examples/dotnet-aspnet-mvc-net8/AppConfigSnapshot.cs` + `scripts/test-dotnet-proof.ts`.
- Boundaries: this does not claim MSBuild-triggered generation; that remains planned under `P2-B1`.

### 2026-03-14: Context-Mode MCP Server Acquisition Strategy

- Initiative: `mcp-server-configuration`
- Date: 2026-03-14
- Source: O'Brien, requested by Matthew Corven
- Decision: Changed `context-mode` MCP server from direct PATH-based command invocation to `npx`-based global package acquisition, making it consistent with all other MCP server entries.
- Implementation: Modified `.copilot/mcp-config.json`, `.squad/templates/mcp-config.md`, and `packages/varlock-docs-mcp/README.md` to use `npx -y context-mode` instead of direct `context-mode` command.
- Rationale: Consistency with other MCP servers (`github`, `azure`, `aspire`, `trello`), automatic package installation, future version pinning capability, and cross-environment portability.
- Scope: CLI config and documentation examples only; no separate VS Code config created.

### 2026-03-15: User Directive — Remove "workitems" Language

- Date: 2026-03-15T02:55:53Z
- Source: Matthew Corven
- Directive: Remove or replace "workitems" / "work item" language in user-facing instructions and examples; users should not be introduced to that concept.
- Scope: User-facing materials only
- Rationale: User request — captured for team memory

### 2026-03-14: User Directive — Align VS Code Copilot and GitHub Copilot CLI MCP Servers

- Date: 2026-03-14T14:14:51Z
- Source: Matthew Corven
- Directive: Keep VS Code Copilot and GitHub Copilot CLI aligned on the same MCP server set and versions, preferring `npx` or similar package-runner based invocation over local direct commands.
- Scope: MCP server configuration across both environments
- Rationale: User request — captured for team memory

### 2026-03-15: P1-A2 Completion Contract and Phase 1 Exit Clarity

- Initiative: `dotnet-support`
- Node: `P1-A2`
- Source: Picard
- Date: 2026-03-15
- Decision: P1-A2 node-level scope is complete. All explicitly accepted proof rows are proven; no deferred items remain orphaned.
- Proof status: All three proof rows (explicit `dotnet build`, User Secrets coexistence, machine-readable diagnostics) proven and verified.
- Phase 1 exit gap resolution: WinForms legacy target (`net48`) belongs in P3-A1 as "wider platform proof"; `Varlock.MSBuild` belongs in P2-B1 per existing team decisions.
- Operative Phase 1 exit criteria (after deferrals): C# generation specimen with binder validation (P1-B1 ✅), executable distribution specimen for local dev and CI (P1-A1 ✅), contract fixtures backing low-level bridge tests (P1-A2 ✅), console and ASP.NET MVC examples proving initial direct and provider-based usage (P0-C3 + P1-A2 ✅).
- Ledger housekeeping: C# type generation row should be updated to `proven` citing the ASP.NET example and P1-B1 decision.
- Board action: P1-A2 should be marked `DONE` in `.squad/progression.md`.

### 2026-03-15: P1-A2 Blocker Ledger — No Blockers, Proceed to Execution

- Initiative: `dotnet-support`
- Node: `P1-A2`
- Source: Ralph
- Date: 2026-03-15T12:00:00Z
- Verdict: P1-A2 is blocked: **NO**. Proceed with autonomous execution on bounded scope.
- Critical exit criteria: All met — explicit dotnet build for both examples ✓, User Secrets coexistence ✓, machine-readable diagnostics alignment ✓, P1-B1 example-backed binder validation ✓.
- High-priority partial (non-blocking follow-up): Support-matrix ledger row synchronization requires formal review/update of `dotnet-support-ledger.yml` but does not gate execution.
- Intentional deferrals (all documented): Watch-mode behavior → P2-A1, IDE IntelliSense → P2-B1, `IOptions<T>` full semantics → P2-A1, wider platform coverage → P3-A1, MSBuild incremental integration → P2-B1. None are blockers.
- Phase-gate checklist: Prerequisites (P0-C1 through P1-A1) done ✓; critical proof rows green ✓; build validation passing ✓; User Secrets test passing ✓; shared fixtures canonical ✓; binder validation passing ✓; design deferrals documented ✓; no critical design gaps ✓.
- Autonomous scope cleared: Formal ledger row update (O'Brien follow-up) and decision record archival (if needed) are documentation-only, non-blocking tasks.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

### 2026-03-15: P2-A1 Scope Boundary for Autonomous Ralph Run

- Initiative: `dotnet-support`
- Node: `P2-A1`
- Source: Picard (Initiative Lead)
- Decision Date: 2026-03-15T04:30:00Z

**Decision:** P2-A1 is scoped to **watch-and-reload proof inside `Varlock.Extensions.Configuration`** — the existing package. The deliverable is reload semantics (ReloadOnChange, ReloadFailureBehavior, file-watching, change-token support, atomic swap, debounced coalescing, last-known-good preservation) proven through IOptionsMonitor<T>.OnChange in the existing ASP.NET example and extended `bun run proof:dotnet` harness.

**Out of Scope:**
- `Varlock.Extensions.Hosting` — convenience sugar (AddVarlock helper), deferred to P2-B1 or P2-A2
- `examples/dotnet-worker-net8/` — dedicated long-running worker example, deferred to P2-B1 or P3-A1; existing ASP.NET example sufficient to prove `IOptionsMonitor<T>` semantics
- `IOptionsSnapshot<T>` scoped-reload proof — deferred to P2-B1

**Acceptance Checklist:**
1. ReloadOnChange property on VarlockConfigurationSource (default false)
2. ReloadFailureBehavior enum (KeepLastKnownGood only for v1)
3. File watcher watching root schema path + active source files
4. Debounced coalescing (overlapping file events → single reload, low hundreds ms)
5. Atomic configuration swap (Data replaced only after successful reload)
6. Change-token integration (fires only after successful committed reload)
7. Last-known-good preservation (failed reloads keep previous Data)
8. Watch-set recomputation after each successful reload
9. IOptionsMonitor<T>.OnChange proof in ASP.NET example or harness
10. Extended scripts/test-dotnet-proof.ts with reload assertions
11. Unit/integration tests covering: successful cycle, failed preservation, change-token semantics
12. Support-matrix ledger updates (planned → proven rows)
13. Documentation (XML-doc on public reload APIs)
14. bun run proof:dotnet passes with new assertions

**Rationale:** Reload mechanics are load-bearing. Everything else is convenience, reusable, or contingent on stable infrastructure. One Ralph run. Reload correctness. Proof artifacts. Done.

### 2026-03-15: P2-A1 Contract Boundaries for Ralph's Reload Implementation

- Initiative: `dotnet-support`
- Node: `P2-A1`
- Source: Tuvok (Contracts & Security Lead)
- Decision Date: 2026-03-15

**Decision:** Ralph's reload implementation must preserve machine-readable contract boundaries for bridge envelope parsing, optional-schema semantics, and proof-harness assertions.

**Public API Stability — Preserved:**
- VarlockConfigurationSource properties: SchemaPath, Optional, EnvironmentName, WorkingDirectory, ExecutablePath, EnableLocalExecutableLookup, EnablePathLookup, Runtime
- VarlockConfigurationSource.Build(IConfigurationBuilder builder)
- VarlockConfigurationBuilderExtensions.AddVarlock(builder) and configure delegate overloads
- VarlockConfigurationProvider.Load() synchronous startup path
- VarlockConfigurationFlattener.Flatten() semantics (no JSON shadow entries)

**Public API — New (Ralph adds exactly):**
- Property ReloadOnChange { get; set; } on VarlockConfigurationSource (default false)
- Property ReloadFailureBehavior { get; set; } on VarlockConfigurationSource (enum)
- Enum VarlockReloadFailureBehavior with single value KeepLastKnownGood (v1)

**Bridge Contract — Unchanged:**
- Success payload: { contractVersion, cliVersion, command, format, ok: true, graph }
- Failure payload: { contractVersion, cliVersion, command, format, ok: false, category, message }
- 7 error categories: ExecutableNotFound, ExecutableVersionMismatch, SchemaMissing, SchemaInvalid, ResolutionFailed, PluginLoadFailed, BridgeInternalError
- VarlockResolvedGraph properties (Items, Sources, RedactLogs, PreventLeaks, BasePath, ContractVersion)
- BridgeContractAlignmentTests fixture files must pass without modification

**Last-Known-Good Preservation:**
1. Failed reload → keep previous Data unchanged
2. Failed reload → do NOT fire IConfigurationRoot.GetReloadToken()
3. Failed reload → do NOT mutate configuration values visible to consumers
4. Optional: true with missing schema → empty provider stays empty on reload failure
5. Reload failure with schema disappearing → preserve previous state

**Atomic Swap & Change-Token:**
1. Successful reload → swap Data atomically before firing change token
2. Change token fires at most once per successful reload
3. Consumers never observe partially-updated configuration state
4. Concurrent reads during reload see either last successful or next successful state, never mixed

**Watch-Set Recomputation:**
1. After successful reload → recompute watched file set from new graph
2. After failed reload → keep previous watch set unchanged
3. Watch set derived only from last-successful-state graph
4. Import graph changes and env-specific source activation reflected in next recomputation

**Optional Schema + ReloadOnChange:**
1. Optional: true, ReloadOnChange: false → empty provider, no watch for later arrival
2. Optional: true, ReloadOnChange: true → empty provider, watches for schema file, activates on successful load
3. Schema appears and loads successfully → populate Data + fire change token
4. Schema appears and load fails → stay empty, no change token

**Proof-Harness Observable Shapes:**
- Console example: appName, httpPort, featureEnabled, secretIsSensitive, redactLogs, preventLeaks, sourceLabels (unchanged)
- ASP.NET example: AppName, AppPort, FeatureEnabled, AppSettingsOnly, SecretTokenPresent, UserSecretsOnly (unchanged)
- scripts/test-dotnet-proof.ts must pass after Ralph adds reload-specific test cases

**Rationale:** Boundaries are machine-readable and test-backed. Protect existing consumer code and guarantee predictable bridge behavior under reload scenarios. Breaching these boundaries will invalidate tests and destabilize downstream production usage. Ralph's scope constrained to successful delivery within these boundaries.

### 2026-03-15: Next Node After P2-A1: P2-B1 (MSBuild Integration)

- Initiative: `dotnet-support`
- Current Node: P2-A1 (Reload/options/hosting helpers) — COMPLETE
- Next Node: P2-B1 (MSBuild integration)
- Decision Maker: Picard (Initiative Lead)
- Decision Date: 2026-03-15T04:30:00Z

**Decision:** P2-B1 is NEXT. Reload mechanics and options-monitor proof are shipped and proven. MSBuild integration is the only remaining Phase 2 work item and must complete before the convergence point P3-A1 (per progression board).

**Rationale:**
1. **No blockers remain.** P2-A1's reload infrastructure is stable; proof passes; `bun run proof:dotnet` is clean. The contract with the CLI bridge is mature.
2. **Phase gate alignment.** Progression board shows P2-B1 and P2-A1 as parallel Phase 2 deliverables, both converging at P3-A1. No Phase 2 exit possible until both complete.
3. **Proposal alignment.** `docs/proposals/dotnet-support.md` Phase 2 scope explicitly includes "MSBuild integration for generated C# output." Non-deferred item.
4. **Single critical path.** Unlike P2-A1 (which had parallel convenience-sugar deferral), P2-B1 has no deferred sub-items. Build-time integration is monolithic.

**Scope — P2-B1 will cover:**
- Integrate `varlock typegen --lang=cs` into MSBuild property groups or custom tasks
- Generated C# output written to standard .NET build output tree (e.g., `obj/generated/`)
- C# files eligible for `InternalsVisibleTo` or public consumption depending on design
- Proof via existing ASP.NET MVC example (updated .csproj to use MSBuild integration)
- Updated support-matrix ledger to mark "build-time C# generation" from `planned` to `proven`

**Scope — P2-B1 does NOT cover:**
- `Varlock.Extensions.Hosting` package (deferred to P2-A2 or beyond)
- `varlock watch` behavior during MSBuild incremental builds (deferred; bridge can rerun on demand)
- Analyzer or native runtime (deferred to P4-A1)

**What Remains Deferred:**
- `Varlock.Extensions.Hosting` → P2-A2 or P2-B2 (convenience sugar, not load-bearing)
- Analyzer / native .NET parser → P4-A1 (requires bridge maturity + broader proof first)
- Broader framework examples (Functions, Blazor, WinForms) → P3-A1 (part of wider platform proof)

**Next Coordination Step:** Once P2-B1 completes and proof artifacts pass, both P2-A1 and P2-B1 converge at P3-A1, which will expand proof coverage across broader .NET platforms and runtimes.


### 2026-03-15: Geordi — P2-B1 First MSBuild Typegen Cut

- Initiative: `dotnet-support`
- Node: `P2-B1`
- Source: Geordi via Copilot
- Decision Date: 2026-03-15T15:53:27.444Z

**Decision:** The first mergeable `P2-B1` slice wires the existing `varlock typegen` flow into MSBuild with opt-in `.props` / `.targets` rather than opening with a dedicated analyzer or new CLI surface.

**Implementation Details:**
- Generated C# moves into `obj/Varlock/` and is added to `@(Compile)` by the target
- Example stops depending on checked-in `.g.cs` artifact
- Incremental proof: "same inputs, no rewrite" on a second `dotnet build`

**Files Created/Modified:**
- `packages/dotnet/Varlock.MSBuild/build/Varlock.MSBuild.props`
- `packages/dotnet/Varlock.MSBuild/build/Varlock.MSBuild.targets`
- `examples/dotnet-aspnet-mvc-net8/.env.schema` — points C# output at `obj/Varlock/AppConfig.g.cs`
- `examples/dotnet-aspnet-mvc-net8/dotnet-aspnet-mvc-net8.csproj` — imports build files, excludes legacy generated files
- `scripts/test-dotnet-proof.ts` — proves build-driven generation plus no rewrite on unchanged second build

**Rationale:** Keeps the build step deterministic and reviewable while reusing the already-proven `lang=cs` generator, naming overrides, and metadata sidecar output. Avoids smuggling environment-dependent validation or analyzer work into the first MSBuild bridge cut.

**Remaining work after this cut:**
- Add a packageable `Varlock.MSBuild.csproj` with proper `build` / `buildTransitive` assets instead of repo-local manual imports
- Add explicit MSBuild validation target only after non-env-sensitive validation command or deterministic bridge path exists
- Expand proof to IDE/design-time and `dotnet watch` behavior
- Track imported-schema inputs more precisely than root schema path alone

### 2026-03-15: O'Brien — P2-B1 Proof Lane Brief

- Initiative: `dotnet-support`
- Node: `P2-B1`
- Source: O'Brien via Copilot
- Decision Date: 2026-03-15T15:53:27.444Z

**Decision:** The Distribution & Proof lane for P2-B1 ("MSBuild integration for generated C# output") executes in parallel with Geordi's core MSBuild package work. O'Brien owns proof artifacts, CI wiring, and documentation surface.

**P2-B1 Contract (From Proposal):**
- Generate C# during build via `Varlock.MSBuild` package
- Optional build-time validation during build
- Incremental inputs and outputs (MSBuild caching behavior)
- Write generated files into `obj/Varlock/` for hygiene
- Surface failures as normal MSBuild diagnostics

**Proof Lane Scope — What Gets Added to `bun run proof:dotnet`:**

1. **Build-time generation check:**
   - Both example projects import `Varlock.MSBuild` in `.csproj`
   - Run `dotnet build` against both examples
   - Assert `obj/Varlock/VarlockConfig.g.cs` exists and is not empty
   - Verify generated file contains expected class and property names
   - Verify no pathological rebuild loops under repeated builds

2. **Generated-file freshness validation:**
   - Run build, modify `.env.schema`, run build again
   - Assert generated file updates with new schema state
   - Assert old generated files clean from `obj/` when no longer needed

3. **Incremental build caching:**
   - First build generates the file
   - Second build with no schema changes should skip generation (verify via log/timestamp)
   - Modify only non-schema input, verify generation is skipped

4. **Optional validation during build:**
   - If `VarlockValidateOnBuild=true`, assert validation runs
   - Prove validation failure surfaces as normal MSBuild error/warning

**Documentation Touch Points:**
- Update proposal/support matrix caveat for generated-file behavior
- Package README: explain `VarlockEnabled`, `VarlockSchemaPath`, `VarlockGenerateTypes`, `VarlockValidateOnBuild`, `VarlockGeneratedFile` properties
- Example projects: demonstrate property overrides and schema → generated-file flow

**Explicitly Deferred from P2-B1:**
- `dotnet watch` behavior and IDE incremental builds
- IntelliSense/design-time build diagnostics
- Complex binder validation beyond basic proof
- Serilog or logging integration

**Handoff:**
- Geordi owns core `Varlock.MSBuild` implementation
- O'Brien owns proof command updates, CI integration, documentation, and repository hygiene

**Rationale:** This proof lane does not depend on Geordi's implementation details beyond the contract promise. O'Brien can prepare proof command skeleton and documentation structure in advance without blocking Geordi's work.

### 2026-03-15: Picard — P2-B1 First Cut Review & Acceptance

- Initiative: `dotnet-support`
- Node: `P2-B1`
- Source: Picard (Initiative Lead)
- Decision Date: 2026-03-15T15:53:27.444Z

**Decision:** P2-B1 first cut is ACCEPTED. Marking P2-B1 as **`in progress`**.

**Executive Summary:**

Geordi's first cut of P2-B1 is contract-respecting, deterministic, and ready for active development. The slice wires build-time C# generation into MSBuild pipelines via `.props`/`.targets` files, writes generated C# to `obj/Varlock/` (configurable), and proves incremental build behavior (second build with unchanged inputs does NOT rewrite the generated file). Proof test passes end-to-end.

**Validation Against Proposal Contract:**

| Promise | Status | Evidence |
|---------|--------|----------|
| Generate C# during build | ✅ Proven | VarlockGenerateTypes target invokes `varlock typegen` |
| Optionally validate during build | ✅ Ready | VarlockValidateOnBuild property defined, defaults false (deferred implementation) |
| Incremental inputs and outputs | ✅ Proven | Target.Inputs/Outputs properly specified; second build skips generation |
| Write generated files into `obj/Varlock/` | ✅ Proven | Default path is `obj/Varlock/VarlockConfig.g.cs`; example uses `obj/Varlock/AppConfig.g.cs` |
| Surface failures as normal MSBuild diagnostics | ✅ Ready | Error tasks in place for missing schema, missing executable, missing output |

**Properties Included (as specified):**
- `VarlockEnabled` (false by default)
- `VarlockSchemaPath` (.env.schema default)
- `VarlockGenerateTypes` (true by default)
- `VarlockValidateOnBuild` (false by default, deferred)
- `VarlockGeneratedFile` (configurable output path)

**Intelligently Added (justified by earlier phases):**
- `VarlockEnableLocalExecutableLookup` — aligns with P1-A1 executable acquisition
- `VarlockEnablePathLookup` — aligns with P1-A1 executable acquisition
- `VarlockWorkingDirectory` — allows schema resolution flexibility
- `VarlockExecutablePath` — explicit override for advanced scenarios

**Incremental Build Behavior (Proven Deterministic):**
- First build generates file and adds to compile
- Second build with no schema/csproj changes:
  - Skips VarlockGenerateTypes execution (incremental cache hit)
  - File exists; VarlockPrepareGeneratedCompileItems re-adds to compile
  - File timestamp unchanged (not rewritten)
- Proof test validates both content equality and mtime preservation

This prevents rebuild loops and keeps IDE experiences clean.

**Integration Consistency:**

✅ **With P1-A1 (Executable Acquisition):** MSBuild target reuses the same fallback chain proven in P1-A1 (explicit override → package install → local bin → repo development → PATH).

✅ **With P1-B1 (C# Type Generation):** Target invokes existing `varlock typegen --path` command, reusing P1-B1's proven lang=cs generator, naming overrides, and metadata sidecar output.

✅ **With P2-A1 (Reload/Options/Hosting):** P2-A1 proves reload behavior at runtime; P2-B1 proves generation at build time. No interaction; reload works with or without MSBuild-generated types.

**Scope Containment (Correctly Deferred):**

1. **Optional validation during build** — VarlockValidateOnBuild property defined; implementation deferred
2. **Design-time / IDE generation** — Target includes `DesignTimeBuild` guard to prevent expensive generation during IDE operations
3. **dotnet watch integration** — Watch-mode regeneration semantics deferred; slice proves cold builds only
4. **Packageable NuGet assets** — Current slice uses repo-local `build/` folder imports; packaging as `Varlock.MSBuild` NuGet deferred
5. **Binder attributes or validation error elaboration** — Validation deterministic but diagnostics remain basic MSBuild errors

**Risk Assessment: MINIMAL**

- ✅ No new CLI surfaces (uses existing `varlock typegen`)
- ✅ No new validation semantics (generation only, validation deferred)
- ✅ No new example projects (reuses dotnet-console-net8 and dotnet-aspnet-mvc-net8)
- ✅ No new package dependencies (MSBuild targets are XML; no runtime dependencies)
- ✅ No breaking changes to existing packages
- ✅ Proof test passes; no known blockers

**Outstanding Work Before P2-B1 Closure:**

**Geordi's Remaining Work:**
1. NuGet packageability — Create true `Varlock.MSBuild.csproj` with `build`/`buildTransitive` assets
2. Design-time expansion — Consider IDE/IntelliSense generation behavior
3. dotnet watch support — Prove watch-mode regeneration without breaking watch behavior
4. Precise input tracking — Track schema imports/includes more precisely than root schema path alone

**O'Brien's Proof Lane:**
1. Proof checklist completion — Validate content parsing, optional validation during build test
2. CI integration — Ensure `test.yaml` `proof:dotnet` step passes without manual changes
3. Documentation touch points — Update proposal matrix, package README, example project README
4. Repository hygiene — Verify no generated files committed; `obj/Varlock/` properly ignored

**P2-B1 Closure Criteria:**

P2-B1 will be considered **complete and squad-closed** when:

1. Proof checklist is fully checked (O'Brien confirms all assertions pass)
2. Packageable `Varlock.MSBuild` NuGet exists (Geordi delivers)
3. CI passes without manual intervention (existing `test.yaml` runs proof automatically)
4. No blockers remain for P3-A1 (platform proof expansion)

At that point, Picard will re-review and approve P2-B1 closure, unblocking P3-A1 entry.

**Status Change:** Marking P2-B1 as **`in progress`** in `.squad/progression.md`.

**Rationale:** Geordi's core work is solid; O'Brien's proof lane can now proceed in parallel. Both can execute without waiting on new decisions from Picard.

**No Blockers for Continuation:** Geordi may proceed with packageability and IDE expansion work. O'Brien may proceed with proof checklist completion and documentation updates. Matthew may begin delegation on either or both tracks.

---

## O'Brien: C# Type Generation Path Alignment

- **Decision Date:** 2026-03-15
- **Initiative:** `dotnet-support`
- **Node:** `P2-B1`
- **Status:** RESOLVED
- **Impact:** Documentation alignment only

### Issue

The P2-B1 support-matrix ledger entry for "C# type generation" claimed the generated file location as `Generated/AppConfig.g.cs`, but the actual MSBuild implementation following the deterministic-external-typegen pattern now writes to `obj/Varlock/AppConfig.g.cs`.

This was caught during proof validation before commit.

### Resolution

Updated the ledger entry to reflect the actual generated-file path and clarified that generation happens during `dotnet build` via MSBuild integration, not as a separate step. The entry now correctly states:
- Generated file lives at `obj/Varlock/AppConfig.g.cs` (intermediate output, not tracked)
- Generation is deterministic and incremental
- Proof validates both structure and incrementality

### Proof Status

✓ `bun run proof:dotnet` passes with updated docs  
✓ Generation path assertion in `scripts/test-dotnet-proof.ts` confirms `obj/Varlock/AppConfig.g.cs`  
✓ Incremental build detection confirmed (mtime unchanged on second identical build)

### Files Modified

- `docs/proposals/dotnet-support.md` — ledger entry clarification only (no claim changes, only accuracy)
