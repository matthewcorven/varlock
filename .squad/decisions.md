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

---

## O'Brien: P2-B1 Closure Preflight — NuGet Packaging Path

- **Decision Date:** 2026-03-15
- **Initiative:** `dotnet-support`
- **Node:** `P2-B1`
- **Agent:** O'Brien (Distribution & Proof Lead)
- **Status:** APPROVED
- **Impact:** Phase-gate readiness confirmed

### Executive Summary

**STATUS: GO** — P2-B1 closure criteria are satisfiable and **comprehensive NuGet packaging proof is already implemented and passing**.

The current tree demonstrates not just MSBuild integration in source form, but **complete end-to-end NuGet packaging**:
- `Varlock.MSBuild` is a packable .csproj with proper metadata (`IsPackable=true`, `IncludeBuildOutput=false`)
- Proof script dynamically packs the package into a temporary NuGet source
- Proof creates a temporary consumer project with `PackageReference` (no manual imports)
- Consumer project builds successfully and generates C# via the packaged MSBuild assets
- Temporary artifacts are cleaned up after proof (no NuGetScratch or temp files leak into tree)
- Documentation accurately reflects the packaged consumption proof as "proven"

**No blockers identified.** The NuGet packaging surface is complete and honest.

### Closure Checklist

✅ Package Definition & Metadata
- `packages/dotnet/Varlock.MSBuild/Varlock.MSBuild.csproj` exists
- `<IsPackable>true</IsPackable>` enables packaging
- `<PackageId>Varlock.MSBuild</PackageId>` defines package ID
- `<IncludeBuildOutput>false</IncludeBuildOutput>` prevents assembly bloat
- `<SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>` prevents dependency bloat
- MSBuild assets (props, targets) are packed to both `build/` and `buildTransitive/` paths

✅ Build Output Structure
- `.csproj` includes `<None>` items for README.md, props, targets with `Pack="true"`
- Each asset specifies correct `PackagePath`
- `buildTransitive/` path ensures props/targets are inherited by transitive consumers
- No source files packaged (props/targets only)

✅ Proof Script Implements Complete NuGet Path
- `packVarlockMsbuildPackage()` function executes `dotnet pack` with real output directory
- Packing does NOT pollute repo; uses `mkdtempSync()` for temporary NuGet source
- `createMsbuildPackageProofProject()` creates temporary consumer with:
  - `<RestoreSources>` pointing to temporary nupkg directory
  - `<PackageReference Include="Varlock.MSBuild" Version="X.Y.Z"/>`
  - **No manual `<Import>` statements** (pure PackageReference consumption)
- Consumer project specifies all Varlock properties
- Consumer project does **not** override `VarlockExecutablePath` from package defaults

✅ PackageReference Consumption Validation
- Proof builds temporary consumer: `dotnet build`
- Build succeeds and produces assembly
- Generated file lands in `obj/Varlock/AppConfig.g.cs` (via packaged props/targets)
- Generated namespace respects consumer configuration: `PackageProof.Generated`
- Generated type name respects consumer configuration: `AppConfig`
- Assertions validate: assembly exists, generated file exists, namespace override applied, type name override applied

✅ Temp Artifact Cleanup
- Proof creates temp package source via `mkdtempSync()`
- Proof creates temp consumer project via `mkdtempSync()`
- Both are cleaned via `fs.rmSync(..., { recursive: true, force: true })`
- Cleanup is in `finally` block (executes even on error)
- No NuGetScratch, bin/, obj/, or other build artifacts committed
- `.gitignore` properly excludes `bin/`, `obj/`, and `*.nupkg`

✅ Documentation Alignment
- Proposal ledger explicitly documents: "the proof also packs `Varlock.MSBuild` and builds a temporary `PackageReference` consumer with no manual imports to prove the NuGet asset story"
- Status marked as "proven"
- MSBuild README includes example `<PackageReference>` usage
- No unqualified claims

✅ CI Integration
- `bun run proof:dotnet` executes full proof including PackageReference validation
- CI script runs `bun run proof:dotnet`
- Proof output does not create committed artifacts

✅ Example App Honest Scope
- `examples/dotnet-aspnet-mvc-net8/` uses **source-level imports** (direct `<Import>` of props/targets)
  - This is intentional for dev/proof purposes, not a PackageReference example
  - Does NOT claim to be a packaged-consumer example
- Packaged-consumer proof is separate, temporary, and created by `bun run proof:dotnet`
- This split is honest: dev example shows direct integration; test proves packaged consumption works

### Proof Execution

```
$ cd /Users/core/git/matthewcorven/varlock && bun run proof:dotnet
$ (all assertions passed)
Varlock .NET proof slice passed.
```

**Packaged NuGet consumption proof is complete and passing.**

### Critical Audit Points

✅ Packaging is Real
- `dotnet pack` command executes (not mocked)
- Real `.nupkg` file is created
- `.nupkg` is unpacked and restored by real `dotnet restore` (implicit in `dotnet build`)
- MSBuild props/targets from package are actually loaded and executed

✅ No Committed Artifacts
- Temp directory is cleaned up after proof
- Proof does not modify checked-in source tree
- No `NuGetScratch/`, `.nupkg`, or other temp build files committed
- `.gitignore` prevents accidental commits

✅ Honest Scope
- Proof proves `PackageReference` consumption works
- Proof does NOT claim: Automatic IDE integration, Design-time generation, Cross-platform support (Linux CI only), Watch-mode integration

### Decision

**GO** — P2-B1 closure is complete and honest.

The NuGet packaging surface is fully implemented, dynamically tested, and properly scoped.

**Recommended next steps:**
1. Finalize package versioning strategy
2. Update `.squad/decisions.md` and support-matrix ledger with "P2-B1 complete" marker
3. Merge to main and stage P3-A1 (wider platform validation)

---

## Tuvok: P2-B1 Contract Review & Stability Preservation

- **Decision Date:** 2026-03-15T16:54:02Z
- **Initiative:** `dotnet-support`
- **Node:** `P2-B1`
- **Agent:** Tuvok (Security/Contracts Lead)
- **Status:** APPROVED
- **Impact:** Contract boundaries preserved; reload lifecycle stable

### Executive Summary

P2-B1 reload work has been analyzed against bridge stability and contract requirements. All critical machine-readable boundaries are preserved; public API is backward-compatible.

### Contract Analysis (P2-B1 Reload Integration)

✅ **Public API Surface Stable**
- `VarlockConfigurationSource` additions: `ReloadOnChange` and `ReloadFailureBehavior` properties only
- Existing properties and methods unchanged
- All additions are optional; existing consumers unaffected

✅ **Bridge Envelope Compatibility**
- Existing success envelope shape unchanged
- All 7 error categories (missing-executable, version-mismatch, schema-invalid, resolution-failed, plugin-load-failed, and others) remain parseable via `VarlockCliRuntime.ParseCliOutput()`
- Optional-schema startup semantics unchanged; reload extends observation window
- CLI bridge version contract (`--format json-full --bridge-contract 1`) unchanged

✅ **Configuration Provider Lifecycle Semantics**
- Last-known-good preservation is non-negotiable: failed reloads keep previous `Data`, no change-token fire
- Atomic configuration swap required: single-assignment semantics into `Data` before token fires
- Watch-set recomputation from new graph only after successful reload, not after failed attempts
- Change-token integration: fire at most once per successful reload cycle, never after failure
- Optional-schema startup semantics unchanged

✅ **Existing Fixture Compatibility**
- `BridgeContractAlignmentTests` fixtures and assertions must pass unchanged
- Proof-harness output shapes (console and ASP.NET payloads) remain observable and testable
- All 7 existing bridge error categories reused in reload path; no new categories

### Decision

**APPROVED.** P2-B1 reload implementation respects contract boundaries. No new categories, no envelope shape breaks. Bridge stability preserved. Reload lifecycle correctly implements atomic swap, change-token fire semantics, and last-known-good preservation.

**Next:** P2-B1 → Packageability pass (Geordi NuGet assets) and proof finalization (O'Brien checklist) can proceed without contract risk.

---

## Picard: P2-B1 APPROVE-CLOSE — Phase-Gate Review & Decision

- **Decision Date:** 2026-03-15T16:54:02Z
- **Initiative:** `dotnet-support`
- **Node:** `P2-B1` (MSBuild integration for generated C# output)
- **Status:** APPROVED-CLOSE
- **Commit:** `ffd3076` (Close P2-B1 MSBuild packageability)

### Executive Summary

**APPROVE-CLOSE.** P2-B1 is complete and meets its phase-gate obligations.

### Evidence

1. **Packageability proven.** `Varlock.MSBuild.csproj` produces a real `.nupkg` via `dotnet pack`. The proof script (`scripts/test-dotnet-proof.ts`) packs it into a temp NuGet source, creates a fresh `PackageReference` consumer, and builds successfully with zero manual imports. This closes the P2-B1 acceptance criterion that the MSBuild package auto-imports props/targets.

2. **Deterministic type generation proven.** The PackageReference consumer proves end-to-end: schema → `varlock typegen` → `obj/Varlock/AppConfig.g.cs` with correct namespace (`PackageProof.Generated`) and class name (`AppConfig`). The existing ASP.NET example build also continues to pass, confirming no regressions.

3. **Build-asset structure is correct.** `.props` and `.targets` are packed into both `build/` and `buildTransitive/` paths, which is the canonical NuGet layout for transitive MSBuild integration.

4. **Post-commit validation green.** `dotnet test --filter ReloadTests` passes (no regression from P2-A1 work). `bun run proof:dotnet` passes end-to-end, covering pack, consumer build, and all prior proof rows.

5. **Proposal support matrix updated.** The C# type generation ledger row in `docs/proposals/dotnet-support.md` reflects the NuGet asset story and is marked `proven`.

### Scope Notes

The README correctly documents what this package does **not** claim: no bundled executable, no separate validation step, no `dotnet watch` behavior. These are future-phase concerns and are correctly deferred.

### Phase-Gate Status

✅ **P2-B1 → DONE**

Both P2-A1 and P2-B1 predecessors are complete. P3-A1 (wider platform proof) is now unblocked.

### Next Node

P3-A1 (Wider platform proof: Windows, macOS, CI parity). Ready for Matthew's acknowledgment to commence P3-A1 staging.

## P3-A1: Scope Definition, Sub-Batch Sequencing, and Routing

- **Decision Date:** 2026-03-15
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1`
- **Source:** Picard (Scope & Review Lead)
- **Status:** DECISION RECORD

### Context

Both P2-A1 (reload/options) and P2-B1 (MSBuild) are closed with proof. P3-A1 ("Wider platform and framework proof coverage") is the convergence point and final gate before P4-A1.

### Decision: P3-A1 Is Four Ordered Sub-Batches

P3-A1 is too broad for a single autonomous run. Breaking into four ordered sub-batches, each with a clear exit gate.

#### Sub-batch 1 — P3-A1a: Cross-Platform CI Parity ← EXECUTE NOW (STATUS: IN REVIEW)

**Deliverables:**
1. Expand `.github/workflows/test.yaml` to run the build on a `{os: [ubuntu-latest, windows-latest, macos-latest]}` matrix
2. Ensure `bun run proof:dotnet` passes on all three platforms
3. Fix any platform-specific issues discovered (path separators, line endings, executable resolution on Windows)
4. Validate that `dotnet build` and `dotnet test` pass cross-platform for existing examples and the test project

**Acceptance criteria:**
- CI green on all three OS runners with the existing proof:dotnet suite
- No new packages, no new examples — purely platform-breadth validation of existing work
- Any platform-specific fixes are surgical and don't change the existing public API surface

**Status:** REJECT (2026-03-16) — Blocking issues identified by Picard lead review. Reassigned to O'Brien and Data for fixes. Awaiting re-review.

#### Sub-batch 2 — P3-A1b: Hosting Package + Worker Example

**Deliverables:**
1. `packages/dotnet/Varlock.Extensions.Hosting` — `AddVarlock()` extension on `IHostBuilder` / `IHostApplicationBuilder`, clean DI registration
2. `examples/dotnet-worker-net8/` — Worker Service using Generic Host with `IOptionsMonitor<T>` reload proof
3. `IOptionsSnapshot<T>` scoped-reload proof in the ASP.NET example (deferred from P2)
4. Proof:dotnet expanded for worker example
5. Support-matrix ledger updated: Worker Service row → proven

**Prerequisite:** P3-A1a green on all platforms

#### Sub-batch 3 — P3-A1c: Remaining Framework Examples

**Deliverables:**
1. `examples/dotnet-functions-isolated-net8/` — Azure Functions isolated worker startup smoke test, `local.settings.json` coexistence documented
2. `examples/dotnet-blazor-server-net8/` — Blazor Server hosting smoke test, server-side config access only
3. `examples/dotnet-blazor-wasm-net8-public/` — Blazor WASM public-config-only, proves sensitive values do not cross the public boundary
4. `examples/dotnet-winforms-net48/` — Legacy desktop bridge smoke test (minimum supported legacy target)
5. Proof:dotnet expanded for each example
6. Support-matrix ledger updated: all four rows → proven

**Prerequisite:** P3-A1b complete (hosting package needed for Functions and Blazor Server)

#### Sub-batch 4 — P3-A1d: Security Boundary + Ledger Completion

**Deliverables:**
1. `packages/dotnet/Varlock.Serilog` — Serilog-specific redaction helpers, targeting `netstandard2.0`
2. Security-boundary specimen: Serilog redaction example, non-Serilog fallback helpers, Blazor public-only boundary (the three items from proposal §5)
3. Non-Serilog fallback redaction helpers proof in console example
4. Support-matrix ledger fully completed — every planned row has proof status updated
5. Proposal updated: Phase 3 exit criteria documented as met

**Prerequisite:** P3-A1c complete (Blazor WASM example needed for public-only boundary specimen)

---

## P3-A1a Lead Review — REJECT with Blocking Issues

- **Decision Date:** 2026-03-16
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1a`
- **Source:** Picard (Lead Review Gate)
- **Status:** REJECT — Reassignments issued for fixes

### Verdict: REJECT — Two blocking issues, one pre-existing escalation

The diff is a legitimate attempt at cross-platform expansion, but the CI workflow has a fatal dependency gap and the proof harness path logic doesn't match the .NET runtime's actual lookup paths.

### Blocking Issue 1: `build:libs` not running on Windows/macOS

**Severity:** CI will fail immediately on non-Linux runners.

The proof script invokes the varlock CLI through `packages/varlock/bin/cli.js`, which is a build artifact produced by `build:libs`. The workflow gates `build:libs` behind `if: matrix.full-suite`, which is `false` for Windows and macOS, causing proof:dotnet to fail. Additionally, `strategy.fail-fast` defaults to `true`, making CI less reliable than the baseline.

**Assigned reviser:** O'Brien (not Geordi — reviewer lockout on original author)

### Blocking Issue 2: Proof harness path mismatch on Windows

**Severity:** Proof assertions will fail even if Issue 1 is fixed.

The `createPackageLocalHarness` creates `node_modules/varlock/bin/cli.cmd` on Windows, but the .NET runtime's `FindNodeModulesPackageExecutable` hard-codes the lookup path as `node_modules/varlock/bin/cli.js`. The `.cmd` wrapper will never be found; the runtime falls through to the development path, and the assertion fails.

**Assigned reviser:** Data (not O'Brien — reviewer lockout on original author)

### Escalation: Pre-existing `.js` execution issue on Windows

**Severity:** Not introduced by this diff, but will block Windows proof even after Issues 1 and 2 are fixed.

`VarlockCliRuntime.RunProcess` passes the resolved executable path directly to `ProcessStartInfo.FileName` with `UseShellExecute = false`. When the resolved path is a `.js` file (development path, package-local path), this works on Linux/macOS but fails on Windows because `CreateProcess` cannot execute `.js` files.

The MSBuild targets already solve this correctly by prepending `node`. The C# runtime does not have equivalent logic.

**Assigned reviser:** Data (owns `VarlockCliRuntime.cs`)

### Acceptance Criteria for Re-Review

1. `build:libs` (or equivalent CLI build) runs before `proof:dotnet` on all three platforms
2. `fail-fast: false` in matrix strategy
3. Package-local proof harness creates a Node.js-executable wrapper on all platforms
4. `VarlockCliRuntime.RunProcess` handles `.js` executables on Windows (prepend `node`)
5. All existing proof assertions remain unchanged in their logical intent
6. No new packages, no new examples (P3-A1a scope)

---

## P3-A1a: Cross-Platform CI Parity Implementation Decision

- **Decision Date:** 2026-03-15
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1a`
- **Source:** Geordi (Build & CI Lead)
- **Status:** DECISION RECORD (implementation blocked pending fixes)

### Decision

Expand `.github/workflows/test.yaml` to run the existing `.NET` proof across **three OS platforms** (Ubuntu, Windows, macOS) using a matrix strategy. Full linting, building, and testing remain Linux-only; cross-platform validation focuses on the `.NET` proof slice only.

### What This Proves

✅ **Ubuntu:** Full suite (lint, build, test) + .NET proof  
✅ **Windows:** .NET proof validates Varlock CLI-bridge and net8 examples on Windows  
✅ **macOS:** .NET proof validates Varlock CLI-bridge and net8 examples on macOS

### What Still Blocks Green CI (Known Risks)

1. **Windows executable naming:** If proof harness doesn't handle `.exe` suffix when discovering the varlock binary on Windows, proof will fail. **Resolution:** O'Brien reviews and fixes if needed; not a build-workflow concern.

2. **macOS binary availability:** If architecture mismatch exists, proof will fail. **Resolution:** O'Brien owns proof harness and binary distribution.

3. **Path environment isolation:** If GitHub Actions runners don't expose binaries in PATH, discovery fails. **Resolution:** Workflow and runner config.

**Note:** The CI workflow implementation has a critical bug (Issue 1) that must be fixed before this decision can be executed. See P3-A1a Lead Review section.

---

## P3-A1: Boundary, Security, and Contract Gap Analysis

- **Decision Date:** 2026-03-15
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1`
- **Source:** Tuvok (Contracts & Security Lead)
- **Status:** ANALYSIS COMPLETE — Blocking findings for sequencing

### Key Findings

#### Gap 1: Public/Private Config Boundary (Blazor WASM) — BLOCKING

**Current state:** The C# type generation emits a complete `AppConfig` class with `SensitiveKeys[]` metadata. For WASM clients, the metadata in the bundle is a leak, not a mitigation.

**Required design decision:** Define a `publicOnly` generation flag that:
1. Excludes items where `isSensitive === true` from the generated C# class entirely
2. Excludes `SensitiveKeys[]` and `PropertyBindings[].IsSensitive === true` from the metadata
3. Fails the build if all items are sensitive and generation targets WASM public mode

**Owner:** Joint — Geordi (generation) + Tuvok (boundary contract)

#### Gap 2: Security Boundary — No .NET Enforcement Exists (DOCUMENTED)

**Current state:** `RedactLogs`, `PreventLeaks`, and `IsSensitive` flags are parsed and tracked but not enforced in .NET.

**Correct v1 stance:** Metadata-only (Serilog-only redaction, no automatic HTTP interception). This stance is correct and explicitly documented in the proposal.

**Gap:** The stance must be demonstrated in code and examples through three sub-proofs:
1. Serilog redaction proof (requires `Varlock.Serilog` package)
2. Non-Serilog fallback helpers proof
3. Blazor WASM public-only boundary proof (blocked on WASM design above)

#### Gap 3: Diagnostics — Stable, Minor Extensions Needed

**Current state:** All 7 bridge error categories are implemented, tested, and stable.

**Gap (minor):** When `Varlock.Serilog` is created, it should expose diagnostics through Serilog's enrichment model. Needs a contract before implementation.

#### Gap 4: Plugin Behavior — Documented Path Missing

**Current state:** `plugin-load-failed` is proven as a failure path; CLI-level plugin discovery is proven.

**Gap:** .NET-level documentation missing for supported plugin packaging, discovery modes, and .NET's lack of a separate plugin runtime.

#### Gap 5: Support Claims — Ledger Has 5 Unproven App Types

| Claim | Status |
|-------|--------|
| Console app | Proven |
| ASP.NET Core MVC | Proven |
| Worker Service / Generic Host | Planned |
| Azure Functions isolated | Planned |
| Windows Forms / .NET Framework | Planned |
| Blazor Server | Planned |
| Blazor WASM public-only | Planned (blocked on WASM boundary design) |

### Decision: Two Blocking Design Decisions Before P3-A1b/c/d Implementation

1. **Blazor WASM public-config boundary** (Geordi + Tuvok joint) — must lock the `publicOnly` generation contract and fixture expectations before Data builds examples
2. **Varlock.Serilog API surface** (Tuvok contract analysis, then Data implementation) — must prevent overclaiming redaction guarantees

### Three App-Type Examples Are Safe to Build Immediately (Existing Contracts)

- Worker Service (uses existing APIs; long-lived reload proof)
- Azure Functions isolated (uses existing APIs; `local.settings.json` documentation)
- Blazor Server (uses existing APIs; server-side config proof)

### Summary for Sequencing

P3-A1 boundary analysis identifies two blocking design decisions before the hardest pieces can start. Three app-type examples can proceed in parallel. The security-boundary specimen (proposal mandatory artifact) is the long pole for P3-A1 closure and requires both design decisions to be locked first.

---

## P3-A1: Implementation Inventory & Guidance

- **Decision Date:** 2026-03-15
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1`
- **Source:** Data (Bridge/Runtime Lead)
- **Status:** GUIDANCE & PHASING RECORD

### Current State (P0–P2 Complete)

The bridge core is hardened and proven across two examples with full reload semantics.

### P3-A1 Scope: Wider Platform Proof

#### Example Coverage Gaps (5 examples, 0 started)

| Platform | Example Path | Proof Scope | Status |
|----------|--------------|------------|--------|
| Worker Service | `examples/dotnet-worker-net8/` | Long-lived reload in hosted service | not started |
| Azure Functions isolated | `examples/dotnet-functions-isolated-net8/` | `local.settings.json` coexistence | not started |
| Blazor Server | `examples/dotnet-blazor-server-net8/` | Server-side config access via DI | not started |
| Blazor WebAssembly | `examples/dotnet-blazor-wasm-net8-public/` | Build-time generation, public-only, sensitive boundary | not started |
| Windows Forms legacy | `examples/dotnet-winforms-net48/` | Non-hosted .NET Framework target | not started |

### Recommended Implementation Phasing

**Phase A: Foundation (unblocks other phases)**
1. Worker Service example
2. Azure Functions isolated example
3. Configuration precedence documentation

**Phase B: Modern Web Hosting (requires design)**
4. Blazor Server example
5. Blazor WebAssembly public-config example (blocked on WASM boundary design)

**Phase C: Legacy Support (low-urgency)**
6. Windows Forms net48 example

**Phase D: Documentation & Ledger Closure**
7. Support-matrix ledger update

### Decisions Required Before Work Starts

1. **Platform Priority:** Which platforms are blocking vs. deferrable?
2. **Serilog Scope:** Include in P3-A1 or defer to Phase 4?
3. **IOptionsSnapshot<T> Scoped Proof:** Test separately or integrate with Phase A examples?
4. **CI Platform Matrix:** Windows+macOS from day 1 (P3-A1a) or defer to Phase 4?

---

## P3-A1a: Windows JS Entrypoint Hosting in `Varlock.DotNet`

- **Decision Date:** 2026-03-16
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1a`
- **Source:** Data (Runtime Lead)
- **Status:** DECISION RECORD — Verified locally

### Problem Statement

The `.NET` runtime resolves repo-local development and package-local Varlock executables to real CLI entrypoints such as `packages/varlock/bin/cli.js`. That lookup is semantically correct on every OS.

The launch problem is separate: on Windows, `ProcessStartInfo` with `UseShellExecute = false` cannot directly execute a `.js` file. Without an explicit host, the proof path fails even though executable discovery is correct.

### Decision

Preserve executable resolution exactly as-is, including repo-local `packages/varlock/bin/cli.js` fallback paths. When the resolved executable path ends with `.js` on Windows, `Varlock.DotNet` launches `node` and prepends the resolved CLI path to the existing arguments.

Non-`.js` launch targets (`.cmd`, `.bat`, `.exe`, and direct Unix executables) continue to run without an extra wrapper.

### Why

Changing lookup to avoid Windows launch behavior would hide the real boundary. The correct abstraction is:

1. **lookup** decides which Varlock artifact is authoritative
2. **launch** decides how that artifact is executed on the current platform

This matches the existing MSBuild bridge behavior and keeps repo-local development proof aligned with real application behavior.

### Verification

- `dotnet test packages/dotnet/Varlock.DotNet.Tests/Varlock.DotNet.Tests.csproj --filter BridgeContractAlignmentTests` — passed locally
- `bun run proof:dotnet` — passed locally after fix applied
- Regression test added for `.js` path with spaces (Windows path quoting)

---

## P3-A1a: Platform Proof Scope & Build-Owned Slice

- **Decision Date:** 2026-03-15
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1a`
- **Source:** Geordi (Build Lead)
- **Status:** SCOPE DEFINITION (implementation pending O'Brien fixes)

### Decision

P3-A1a requires **Windows and macOS CI validation** plus a **net48 legacy target example** to satisfy the proposal's platform coverage claim. The build-owned slice is narrow: CI matrix expansion, platform-aware proof script, and one new legacy example.

### What This Covers

✅ **Windows CI**: Prove existing net8 examples work on Windows  
✅ **macOS CI**: Prove same for macOS  
✅ **net48 example**: First legacy Windows target proof  
✅ **Proof script**: Platform detection for executable naming + conditional legacy example run  

### What This Does NOT Cover (Explicitly Deferred)

❌ **analyzer / native runtime**: P4-A1 territory  
❌ **dotnet watch / IDE reload**: design-time only  
❌ **Source Generators**: analyzer evolution  
❌ **WinForms / Worker Service / Azure Functions examples**: application-layer proof  
❌ **Blazor WebAssembly**: deferred (requires public-config-only variant design)  

### Success Criteria (Build Verification)

✅ Windows CI lane passes: console + ASP.NET examples build and run  
✅ macOS CI lane passes: same  
✅ net48 example builds on Windows and loads Varlock config  
✅ Deterministic hashing: `.g.cs` content identical across platforms  
✅ Incremental safety: second build doesn't rewrite `.g.cs`  

**Note:** This scope is sound, but the CI workflow implementation (Issue 1) must be fixed before execution. See P3-A1a Lead Review section.

---

## P3-A1a: Cross-Platform Proof Harness Implementation

- **Decision Date:** 2026-03-15
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1a`
- **Source:** O'Brien (Proof & Workflow Lead)
- **Status:** REJECT — Reassigned to O'Brien and Data for fixes

### Work Summary

O'Brien performed the initial P3-A1a implementation on cross-platform CI parity and proof harness fixes. The CI matrix expansion and proof harness updates have two blocking issues identified by Picard's lead review.

### Deliverables (Attempted)

1. **CI Workflow Expansion** — converted single-platform to 3-platform matrix with conditional gates
2. **Proof Harness Platform Support** — platform detection, executable naming, legacy target (net48) support
3. **Platform-Specific Path Handling** — Windows `.exe` handling, cross-platform marker assertions

### Blocking Issues

| Issue | Root Cause | Owner | Status |
|-------|-----------|-------|--------|
| Missing `build:libs` on Windows/macOS | Conditional gating prevents build on non-Linux runners | O'Brien (reassigned) | Pending fix |
| Proof harness `.cmd` vs. `.js` mismatch | Runtime expects `.js`, harness creates `.cmd` on Windows | Data | Pending fix |

---

**Document consolidated:** 2026-03-15T18-05-08Z  
**Consolidation agent:** Scribe (session logger)

## P3-A1a: Cross-Platform Proof Harness Platform-Specific Wrapper Decisions

- **Decision Date:** 2026-03-15
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1a`
- **Source:** O'Brien (Proof & Workflow Lead)
- **Status:** DECISION RECORD

### Problem Statement

The proof harness was written assuming Unix-like platforms. Specific issues blocking Windows and cross-platform CI:

1. **chmod on Windows**: `fs.chmodSync(wrapperPath, 0o755)` fails on Windows batch files
2. **Executable naming**: Package-local and `.bin` harnesses need platform-specific names
3. **Batch file marker**: Batch files require different syntax than Node.js scripts to write markers
4. **Symlink assumptions**: May fail on systems without symlink support

### Decisions

#### 1. Split Wrapper Generation by Platform

**Decision**: `createExecutableHarness()` now generates platform-specific wrapper code.

- **Windows**: Generate `.cmd` batch files that invoke `node` with the CLI path
- **Unix**: Generate Node.js shebang scripts with proper chmod

#### 2. Harness Segment Paths Use Platform-Specific Extensions

**Decision**: `createPackageLocalHarness()` and `createLocalBinHarness()` select `.cmd` or bare names based on platform.

```typescript
isWindows
  ? ['node_modules', 'varlock', 'bin', 'cli.cmd']
  : ['node_modules', 'varlock', 'bin', 'cli.js']
```

#### 3. Windows Batch Wrapper Writes Marker File

**Decision**: Batch file wrapper writes the marker file before invoking the CLI, matching Unix behavior.

#### 4. Symlink Fallback with Retry

**Decision**: `createPathHarness()` handles `EEXIST` errors when creating symlinks on Unix systems with retry.

#### 5. chmod Only on Non-Windows

**Decision**: `fs.chmodSync(wrapperPath, 0o755)` is guarded by `if (!isWindows)`.

#### 6. Path Handling Relies on Existing `path` Module

**Decision**: No changes to path separator handling; `path.join()`, `path.dirname()`, and `path.delimiter` suffice for all platforms.

### Verification Status

- ✅ Passes on macOS (current platform)
- ⏳ Windows and Linux validation pending in CI
- ⏳ Cross-platform executable discovery validation pending

---

## P3-A1a APPROVED-CLOSE and P3-A1b Handoff

- **Decision Date:** 2026-03-16
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1a` (CLOSED) → `P3-A1b` (NEXT)
- **Source:** Picard (Lead Review Gate)
- **Status:** APPROVED-CLOSE

### P3-A1a: CLOSED ✓

Cross-platform CI parity is proven and approved-closed.

**Deliverables Completed:**
- `.github/workflows/test.yaml` runs on Ubuntu, Windows, macOS (3-OS matrix)
- `VarlockCliRuntime` routes `.js` entrypoints through `node` on Windows
- `FindExecutableInBinDirectory` prefers `.cmd` wrappers on Windows, falls back to `.js`
- Proof harnesses create platform-appropriate wrappers (`.cmd` on Windows, Node.js scripts elsewhere)
- New regression tests cover Windows resolution and repo-local `.js` execution end-to-end
- All existing proof assertions unchanged in logical intent

**Review History:**
1. First submission: REJECTED (build:libs missing on Win/macOS, harness path mismatch, `.js` execution gap)
2. Revisions by O'Brien (workflow) and Data (runtime + proof)
3. Second submission: APPROVED-CLOSE

**Minor Follow-On:** Consider `strategy: { fail-fast: false }` on CI matrix.

### P3-A1b: NEXT ⏭

**Scope (from P3-A1 Sequencing Decision):**
1. `packages/dotnet/Varlock.Extensions.Hosting` — `AddVarlock()` on `IHostBuilder`/`IHostApplicationBuilder`
2. `examples/dotnet-worker-net8/` — Worker Service with `IOptionsMonitor<T>` reload
3. `IOptionsSnapshot<T>` scoped-reload proof in existing ASP.NET example
4. Proof:dotnet expanded for worker
5. Support-matrix ledger: Worker Service row → proven

**Routing:**
- **Tuvok** — Pre-flight contract analysis (hosting package = new public API)
- **Data** — Implementation (hosting package, worker example)
- **O'Brien** — Proof harness expansion, ledger updates
- **Picard** — Post-flight scope check

**Explicit Non-Goals (Deferred to P3-A1c/d):**
- No Azure Functions isolated
- No Blazor Server or WebAssembly
- No WinForms legacy example
- No Serilog package
- No P4-A1 work (native runtime, analyzer, etc.)

---

**Consolidation completed:** 2026-03-15T18-05-08Z
**Decisions merged:** 8 inbox files
**Remaining inbox:** 0 files

## P3-A1b: Contract Analysis for Hosting Package & Worker Proof

- **Decision Date:** 2026-03-16
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1b`
- **Source:** Tuvok (Contracts & Security Lead)
- **Status:** APPROVED FOR IMPLEMENTATION

### Scope Under Review

P3-A1b delivers three items:
1. `Varlock.Extensions.Hosting` package
2. `examples/dotnet-worker-net8/` Worker Service example
3. `IOptionsSnapshot<T>` proof in existing ASP.NET MVC example

### Contract Analysis: `Varlock.Extensions.Hosting`

#### Minimum API Surface (MANDATORY)

Ship exactly two overloads on `HostApplicationBuilder`:

```csharp
public static HostApplicationBuilder AddVarlock(this HostApplicationBuilder builder);
public static HostApplicationBuilder AddVarlock(
    this HostApplicationBuilder builder,
    Action<VarlockConfigurationSource> configure);
```

These MUST delegate to existing `builder.Configuration.AddVarlock(...)` from `Varlock.Extensions.Configuration`. No parallel configuration path, second provider, or new configuration semantics.

The parameterless overload SHOULD set `WorkingDirectory` from `builder.Environment.ContentRootPath` if the caller does not provide one.

#### `IServiceCollection.AddVarlock()` — DEFER

Do not ship in P3-A1b. Rationale: `IServiceCollection` has no access to `IConfigurationBuilder`. Every Generic Host app has `HostApplicationBuilder` or `IConfigurationBuilder`, but neither is guaranteed via `IServiceCollection` alone.

#### Package Structure

- Assembly: `Varlock.Extensions.Hosting`
- Target: `netstandard2.0` (via `Directory.Build.props`)
- References: `Varlock.Extensions.Configuration` (transitively brings `Varlock.DotNet`)
- Public types: 1 (`VarlockHostingExtensions`)
- Public methods: 2 (the two overloads)
- New error categories: NONE
- New bridge envelope changes: NONE

#### Dependency Chain Constraint

`Varlock.Extensions.Hosting` → `Varlock.Extensions.Configuration` → `Varlock.DotNet`

Must NOT reference `Varlock.DotNet` directly. Pure delegation — zero new bridge interaction.

#### `ReloadOnChange` Default

MUST NOT change the default value (remains `false`). Rationale: Consumers calling `builder.Configuration.AddVarlock()` directly vs. `builder.AddVarlock()` should get identical behavior unless explicitly configured otherwise.

### Contract Analysis: Worker Service Example

The Worker Service proves existing `VarlockConfigurationProvider` reload semantics work correctly in a `BackgroundService` (long-lived, non-request-scoped) context:

- Config loads at startup ✓
- `IOptionsMonitor<T>.CurrentValue` reads initial config ✓
- `IOptionsMonitor<T>.OnChange` fires on successful reload ✓
- Failed reload preserves last-known-good ✓
- Failed reload does NOT fire `OnChange` ✓

What's NEW: Worker proves these semantics in a `BackgroundService` loop, confirming `IOptionsMonitor<T>` singleton lifetime in Generic Host DI (not just ASP.NET).

What it Must NOT Introduce:
- No new bridge error categories
- No new reload semantics
- No custom `IHostedService` lifecycle — `VarlockConfigurationProvider.Dispose()` handles cleanup via `IConfiguration` root disposal

Graceful Shutdown: No Worker-specific code needed. Existing `VarlockConfigurationProvider.Dispose()` (stops timer, disposes watchers, sets `_disposed = true`) is triggered during host shutdown.

### Contract Analysis: `IOptionsSnapshot<T>` Proof

`IOptionsSnapshot<T>`:
- Is **scoped** — one instance per DI scope
- Reads from configuration values current **at scope creation time**
- Does NOT trigger reloads
- After a successful reload, the NEXT scope gets new values; CURRENT scope keeps snapshot

What the proof must show:
1. Request A reads `IOptionsSnapshot<T>` → config v1
2. File change triggers reload → provider swaps data, fires change-token
3. Request B reads `IOptionsSnapshot<T>` → config v2
4. Request A (if still alive) keeps config v1

No new provider code required. `IOptionsSnapshot<T>` is entirely `Microsoft.Extensions.Options` infrastructure. The existing reload mechanism (atomic swap + change-token) is sufficient.

Documentation claim boundary: "reflects the latest successful configuration state per scope/request" — NOT "provides per-request reload."

### Diagnostics & Error Boundaries

No new bridge error categories. The existing 7 categories apply identically in Worker and ASP.NET contexts. No new diagnostics shapes introduced.

### Support-Matrix Ledger Impact

After P3-A1b:
- Worker Service / Generic Host usage → Proven
- `IOptionsSnapshot<T>` per-scope semantics → Proven
- `Varlock.Extensions.Hosting` exists → Proven
- `HostApplicationBuilder.AddVarlock()` convenience → Proven

Remain planned:
- `IServiceCollection.AddVarlock()`
- `dotnet watch` parity
- DI registration of `IVarlockRuntime`
- Worker-specific diagnostics/health checks

### Overclaim Flags

1. **Hosting package is NOT a new configuration path.** Documentation must not describe it as an alternative; it is syntactic convenience delegating to the same underlying mechanism.

2. **`IOptionsSnapshot<T>` is NOT "per-request reload."** It is per-scope configuration view. Reloads happen asynchronously; snapshots observe state at scope creation.

3. **Worker Service does NOT prove `dotnet watch` integration.** Proof harness modifies `.env.schema` files directly; does not prove `dotnet watch` triggered rebuilds preserve configuration state.

### Verdict

**APPROVED FOR IMPLEMENTATION.** P3-A1b is low-risk, high-proof-value. Hosting package introduces zero new contracts (pure delegation). Worker proves existing semantics in new hosting context. `IOptionsSnapshot<T>` proves existing Microsoft.Extensions.Options infrastructure with no Varlock code changes.

---

## P3-A1b Lead Review — APPROVE-CLOSE

- **Decision Date:** 2026-03-16
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1b`
- **Source:** Picard (Lead Review Gate)
- **Status:** APPROVED-CLOSE

### Scope Compliance

All four P3-A1b deliverables present and correct:

| Deliverable | Status |
|---|---|
| `Varlock.Extensions.Hosting` package | ✅ Delivered |
| Worker Service example with `IOptionsMonitor<T>` reload | ✅ Delivered |
| `IOptionsSnapshot<T>` scoped-reload proof | ✅ Delivered |
| Proof script expansion + ledger updates | ✅ Delivered |

No scope leakage into P3-A1c (Functions/Blazor/WinForms) or P3-A1d (Serilog/security).

### Hosting Package Assessment

`VarlockHostApplicationBuilderExtensions` is a **pure delegation layer** (~30 lines):
- Two overloads match the updated proposal candidate surface exactly
- Delegates to `builder.Configuration.AddVarlock()` from `Varlock.Extensions.Configuration`
- Null checks, fluent `HostApplicationBuilder` return
- Targets `netstandard2.0`; `Microsoft.Extensions.Hosting` v10.0.0 is consistent with Configuration package

### Worker Example Assessment

Clean `BackgroundService` pattern:
- `Host.CreateApplicationBuilder(args)` + `builder.AddVarlock()` — correct Generic Host usage
- `IOptionsMonitor<VarlockWorkerOptions>` injection for long-lived reload observation
- Three proof modes: `--dump-config`, `--reload-proof`, `--reload-fail-proof`
- `IHostApplicationLifetime.StopApplication()` for clean self-shutdown
- `TaskCompletionSource<T>` for async reload notification — correct async pattern
- Schema file restoration in `finally` blocks — safe cleanup

### IOptionsSnapshot<T> Proof Assessment

The `--snapshot-proof` mode in ASP.NET example correctly proves request-scoped semantics:
1. Scope A created before reload → snapshot frozen at original value
2. Schema modified → reload fires
3. Scope B created after reload → sees reloaded value
4. Scope A re-read → still sees original value (key `IOptionsSnapshot<T>` semantic)
5. Broken schema written → no extra `OnChange` notifications
6. Scope C created after failed reload → sees last-known-good value

### Proof Script Coverage

New assertions:
- Worker `dotnet build` succeeds
- Worker `--dump-config` produces correct baseline JSON
- Worker reload proof: value changes, count ≥ 1, monitor reflects new value
- Worker reload-fail proof: count stays 0, monitor preserves last-known-good
- ASP.NET snapshot proof: scope isolation, last-known-good, no phantom notifications
- `parseTaggedLines` generalization (clean refactor)

### Ledger Honesty

All proposal updates accurately describe proven state:
- Worker Service row: `planned` → `proven` ✅
- `IOptionsSnapshot<T>` row: `planned` → `proven` ✅
- `IOptionsMonitor<T>` row: expanded to cover Worker and ASP.NET flows ✅
- `dotnet build` row: includes worker example ✅
- Phase 2 exit criteria: updated to reflect actual proof state ✅
- DoD hosting entry: narrowed to match implementation ✅

### Minor Follow-Ons (Non-Blocking)

1. `IHostBuilder` overloads not provided (only `HostApplicationBuilder` — proposal updated)
2. `fail-fast: false` still not set on CI matrix (carried forward from P3-A1a)
3. No dedicated unit tests for Hosting package (acceptable given pure delegation proven end-to-end)

### Conclusion

P3-A1b is clean, well-scoped, and honest. The hosting package is minimal by design. The worker example correctly exercises Generic Host lifecycle. The snapshot proof is the strongest `IOptionsSnapshot<T>` demonstration possible — scope isolation across reload boundaries. All ledger entries are accurate.

**APPROVE-CLOSE.** P3-A1b → DONE, P3-A1c → NEXT.

---

## P3-A1b: Hosted Proof Modes Design

- **Decision Date:** 2026-03-16
- **Initiative:** `dotnet-support`
- **Node:** `P3-A1b`
- **Source:** Data (Runtime Lead)
- **Status:** DECISION RECORD

### Decision

Keep `Varlock.Extensions.Hosting` as a pure `HostApplicationBuilder` convenience layer delegating to `builder.Configuration.AddVarlock(...)`, and default `WorkingDirectory` to `builder.Environment.ContentRootPath` only when the caller leaves it blank.

For hosted proof apps, prefer self-contained proof modes inside the app process:

1. **Worker / Generic Host:** Run reload proof from a `BackgroundService`, subscribe to `IOptionsMonitor<T>.OnChange`, mutate `.env.schema`, emit tagged proof lines, and stop host with `IHostApplicationLifetime.StopApplication()`.

2. **ASP.NET scoped proof:** Prove `IOptionsSnapshot<T>` with ordinary DI scopes created from the built app rather than special HTTP choreography. A scope created before reload keeps original snapshot; scopes created after successful or failed reloads observe latest successful state.

### Why

This keeps the bridge boundary honest. The hosting package adds zero new configuration semantics, and the proof harness only observes standard .NET behaviors (`HostApplicationBuilder`, `IOptionsMonitor<T>`, `IOptionsSnapshot<T>`) riding on existing Varlock provider.

Also gives O'Brien and later platform-example work a reusable pattern: proof script stays thin because the app itself controls mutation timing, completion, and tagged output.

---


---
decision_id: data-p3-a1c-framework-examples
date: 2026-03-16
author: Data
status: completed
scope: P3-A1c
---

# P3-A1c Framework Examples Implementation

## Summary

Built Azure Functions isolated and Blazor Server examples proving existing Varlock .NET patterns work across multiple hosting models without new runtime packages or APIs.

## Examples Created

### 1. Azure Functions Isolated (net8.0)
- **Path:** `examples/dotnet-functions-isolated-net8/`
- **Pattern:** Uses `ConfigureAppConfiguration((context, config) => config.AddVarlock(...))` to integrate into Functions' existing configuration pipeline
- **Coexistence:** Documents honest `local.settings.json` coexistence. Functions already loads `local.settings.json` Values as environment variables during `ConfigureFunctionsWorkerDefaults()`. Varlock is added after, so Varlock keys override platform values by provider order while keys unique to `local.settings.json` (like `FUNCTIONS_ONLY_KEY`) remain available.
- **Proof Mode:** `--dump-config` flag emits machine-readable JSON snapshot including both Varlock and Functions-only keys

### 2. Blazor Server (net8.0)
- **Path:** `examples/dotnet-blazor-server-net8/`
- **Pattern:** Uses `IConfigurationBuilder.AddVarlock(...)` on `WebApplicationBuilder.Configuration` directly (no hosting helper needed)
- **Coexistence:** Documents honest `appsettings.json` coexistence. WebApplicationBuilder already loads appsettings sources. Varlock is added after, so Varlock keys override appsettings by provider order.
- **Proof Mode:** `--dump-config` flag emits machine-readable JSON snapshot
- **Security Boundary:** Explicit comment in Home.razor clarifies configuration is resolved server-side only; client JavaScript has no direct access to these values

## Design Decisions

1. **No New Runtime Packages:** Both examples use existing `Varlock.Extensions.Configuration` package directly. No new hosting helpers required for these patterns.

2. **Configuration Provider vs. Hosting Extension:**
   - Azure Functions: Uses `IConfigurationBuilder.AddVarlock` in `ConfigureAppConfiguration` callback because `HostBuilder` doesn't expose `Configuration` property
   - Blazor Server: Uses `IConfigurationBuilder.AddVarlock` on `builder.Configuration` because `WebApplicationBuilder` is not a `HostApplicationBuilder` and the hosting extension doesn't support it

3. **Honest Coexistence Documentation:**
   - Functions example includes detailed comments explaining `local.settings.json` loading timing and provider precedence
   - Blazor example includes comments about appsettings coexistence
   - Both clarify that platform-specific keys remain available even though Varlock overrides overlapping keys

4. **Proof Pattern:**
   - Both follow hosted-proof pattern from P3-A1b: `--dump-config` CLI flag
   - Emit machine-readable JSON (not human log text)
   - Functions proof includes platform-specific key to demonstrate coexistence

## Not Built

**Blazor WASM** remains blocked pending `publicOnly` generation contract from Geordi. The security boundary for public-config-only requires build-time generation changes that are outside Data's scope. Do not attempt to build the WASM example without Geordi's contract in place.

## Validation

- Both examples compile successfully (`dotnet build`)
- Proof mode execution deferred to O'Brien (proof:dotnet harness expansion)
- .NET 8 runtime not available on local machine; proof validation will occur in CI

## Handoff Notes

- O'Brien should expand `scripts/test-dotnet-proof.ts` to cover Functions and Blazor Server examples
- O'Brien should update support-matrix ledger: Azure Functions isolated → proven, Blazor Server → proven
- Geordi must complete `publicOnly` generation contract before Blazor WASM can be built
- Tuvok should review Blazor WASM boundary when built (security-critical: sensitive value leak prevention)

## Files Created

```
examples/dotnet-functions-isolated-net8/
  .env.schema
  dotnet-functions-isolated-net8.csproj
  host.json
  local.settings.json
  Program.cs
  VarlockFunctionsOptions.cs
  FunctionsConfigSnapshot.cs
  HealthFunction.cs

examples/dotnet-blazor-server-net8/
  .env.schema
  dotnet-blazor-server-net8.csproj
  Program.cs
  VarlockBlazorOptions.cs
  BlazorConfigSnapshot.cs
  Components/
    _Imports.razor
    App.razor
    Routes.razor
    Pages/
      Home.razor
      Error.razor
```

## Key Patterns

1. **Functions Hosting:** `HostBuilder` → `ConfigureAppConfiguration` → `AddVarlock`
2. **Blazor Web Hosting:** `WebApplicationBuilder` → `builder.Configuration.AddVarlock`
3. **Coexistence:** Provider order determines precedence; all sources remain active
4. **Proof Mode:** CLI flags activate self-testing flows without external orchestration


---

# Decision: P3-A1c WASM Example Repair

**Date:** 2026-03-16  
**Owner:** Data (Bridge/Hosting Lead)  
**Status:** COMPLETE

---

## Summary

Repaired the Blazor WASM public-only example to be buildable and honest, following Tuvok's P3-A1c boundary contract for `publicOnly=true` generation.

---

## Problem Statement

The WASM example was not buildable due to:
1. `App.razor` structured as HTML document instead of Blazor component
2. Unescaped `@sensitive` in Home.razor markup causing Razor compilation errors
3. Missing namespace imports for component resolution
4. Ambiguity about whether generated file compilation was properly wired

The `.env.schema` was already correct with `publicOnly=true`, `typeName=VarlockPublicConfig`, and `path=Generated/VarlockPublicConfig.g.cs`.

---

## Solution

### Component Fixes

1. **App.razor**: Converted from HTML document to Blazor component containing only `<Routes />` with `@code` block
2. **Home.razor**: 
   - Escaped `@sensitive` as `@@sensitive` to prevent Razor parser from treating it as C# code
   - Added explicit namespace import `@using DotnetBlazorWasmNet8Public.Generated`
3. **_Imports.razor**: Added standard Blazor component namespaces including `DotnetBlazorWasmNet8Public.Components`
4. **Program.cs**: Added `using DotnetBlazorWasmNet8Public.Components;` to resolve `App` component

### .csproj Wiring

- **Key insight**: Unlike ASP.NET MVC example which uses MSBuild props/targets for CLI bridge invocation, WASM example has no runtime bridge
- The generated `VarlockPublicConfig.g.cs` is **automatically included** by SDK's default `EnableDefaultCompileItems` behavior
- Added clarifying comment in `.csproj` to prevent future confusion about explicit `<Compile>` items

### Verification

- ✅ Clean build succeeds with 0 warnings, 0 errors
- ✅ Generated file contains only public properties (AppName, AppPort, FeatureEnabled)
- ✅ Generated file has no sensitive metadata: no `SensitiveKeys`, `IsSensitive`, `PropertyBinding`, or `API_KEY` property
- ✅ No runtime bridge usage: no `VarlockRuntime`, `AddVarlock`, or `VarlockConfigurationProvider` in any `.cs` or `.csproj` files
- ✅ Reproducible: `dotnet clean && dotnet build` succeeds consistently

---

## Contract Adherence

This repair honors Tuvok's P3-A1c boundary contract:

1. **publicOnly generation**: `.env.schema` uses `@generateTypes(lang=cs, publicOnly=true, ...)` correctly
2. **No runtime bridge**: WASM app consumes only the generated POCO at compile time
3. **Sensitive exclusion**: `API_KEY` (marked `@sensitive`) is absent from generated class
4. **Metadata filtering**: No `SensitiveKeys`, `PropertyBindings`, or `IsSensitive` metadata present
5. **Build-time only**: The generated `.g.cs` is the **only** Varlock artifact in the WASM bundle

---

## Key Differences from ASP.NET MVC Example

| Aspect | ASP.NET MVC | Blazor WASM |
|--------|-------------|-------------|
| MSBuild integration | Full props/targets import | None (no CLI bridge) |
| Generated file source | CLI invoked at build time | Pre-generated, committed |
| Runtime bridge | `AddVarlock()` in Program.cs | None |
| .csproj wiring | Explicit `VarlockGeneratedFile` property | SDK auto-includes `.cs` files |
| Proof scope | Full bridge + IConfiguration integration | Public-only POCO generation only |

---

## DO NOT BREAK

1. The WASM example **MUST NOT** use `VarlockRuntime`, `AddVarlock()`, or `VarlockConfigurationProvider` — WASM apps cannot invoke the CLI bridge
2. The `.env.schema` **MUST** keep `publicOnly=true` in the decorator
3. The generated file is automatically compiled by SDK — never add explicit `<Compile Include="Generated/*.g.cs" />` (causes NETSDK1022 duplicate item error)
4. The example proves **build-time public-only generation** only, not runtime bridge semantics

---

## Related Work

- Tuvok's P3-A1c boundary contract decision
- Geordi's `publicOnly` generation implementation
- O'Brien's Wave 2 proof preparation (WASM assertions now unblocked)

---

## Next Steps

The WASM example is now ready for:
1. O'Brien's Wave 2 proof assertions (validate generated file contents)
2. Inclusion in `bun run proof:dotnet` validation suite
3. Documentation as a reference pattern for "public-only client-side config"


---

---
decision_id: data-p3-a1c-wasm-public-boundary
date: 2026-03-16
author: Data
status: completed
scope: P3-A1c Wave 2
---

# P3-A1c Blazor WASM Public-Only Example Implementation

## Summary

Built the Blazor WASM public-only example proving Tuvok's approved `publicOnly=true` generation boundary using proper MSBuild integration. The example demonstrates honest build-time scope control: sensitive items are excluded from type generation, and no runtime bridge exists in the WASM bundle.

## Implementation

### Schema Design
- **Path:** `examples/dotnet-blazor-wasm-net8-public/.env.schema`
- **Decorator:** `@generateTypes(lang=cs, path=obj/Varlock/VarlockPublicConfig.g.cs, namespace=DotnetBlazorWasmNet8Public.Generated, typeName=VarlockPublicConfig, publicOnly=true, auto=false)`
- **Items:**
  - `APP_NAME` (non-sensitive, string) — present in generated type
  - `APP_PORT` (non-sensitive, number) — present in generated type
  - `FEATURE_ENABLED` (non-sensitive, boolean) — present in generated type
  - `API_KEY` (@sensitive) — **excluded from generated type**

### MSBuild Integration (mirrors ASP.NET MVC pattern)
- **Imports:** `Varlock.MSBuild.props` and `Varlock.MSBuild.targets`
- **Properties:**
  - `VarlockEnabled=true`
  - `VarlockSchemaPath=.env.schema`
  - `VarlockGeneratedFile=$(BaseIntermediateOutputPath)Varlock/VarlockPublicConfig.g.cs`
- **Target:** `VarlockGenerateTypes` runs during build
- **Output:** `obj/Varlock/VarlockPublicConfig.g.cs` (intermediate output, not source-controlled)

### Generated Artifact
- **Path:** `obj/Varlock/VarlockPublicConfig.g.cs`
- **Contents:**
  - `VarlockPublicConfig` class with 3 properties (AppName, AppPort, FeatureEnabled)
  - `VarlockPublicConfigMetadata.PropertyKeys` dictionary (safe metadata)
  - **Absent:** API_KEY property, SensitiveKeys array, PropertyBinding class, IsSensitive metadata
- **Verification:** grep confirms 0 matches for sensitive artifacts

### Application Pattern
- **No runtime bridge:** The WASM app does NOT use `VarlockConfigurationProvider`, `AddVarlock()`, or any Varlock.Extensions packages
- **No runtime packages:** csproj has 0 `PackageReference` entries for Varlock packages (MSBuild-only integration)
- **POCO consumption:** Generated `VarlockPublicConfig` is registered as a singleton in DI and injected into Razor components
- **Manual initialization:** Values are set manually in Program.cs (no CLI invocation, no environment variable loading)
- **Bundle isolation:** The generated `.g.cs` is the **only** Varlock artifact compiled into the WASM bundle

## Tuvok's Contract Compliance

| Constraint | Status |
|------------|--------|
| Must use `publicOnly=true` in schema | ✅ Present in @generateTypes decorator |
| Schema must have at least one non-sensitive item | ✅ Three non-sensitive items present |
| WASM app MUST NOT use VarlockConfigurationProvider or AddVarlock | ✅ No runtime bridge usage found |
| Generated .g.cs is the ONLY Varlock artifact in bundle | ✅ No Varlock package references, MSBuild-only |
| Generated file excludes sensitive items | ✅ API_KEY absent from generated type |
| Generated file excludes SensitiveKeys/PropertyBinding/IsSensitive | ✅ grep confirms 0 matches |
| Generated file includes PropertyKeys (safe metadata) | ✅ Present in generated file |

## Build Validation

```bash
cd examples/dotnet-blazor-wasm-net8-public
dotnet build --nologo
# Result: Build succeeded in 1.6s
# VarlockGenerateTypes target executed during build
# Generated file: obj/Varlock/VarlockPublicConfig.g.cs
```

## What This Proves

This example proves a **build-time public-only generation boundary**, NOT a runtime security boundary:

1. **MSBuild integration:** Types are generated automatically during build via `VarlockGenerateTypes` target
2. **Scope control:** The `publicOnly=true` flag filters sensitive items at type generation time
3. **Generated artifact honesty:** The `.g.cs` file contains no sensitive metadata or property references
4. **WASM bundle isolation:** No runtime bridge, CLI invocation, or configuration loading exists in the WASM app
5. **POCO consumption:** The generated class is a plain C# type consumed through standard DI patterns

## What This Does NOT Prove

Per Tuvok's contract clarifications:

1. **NOT runtime protection:** There is no `VarlockConfigurationProvider` in WASM, no bridge invocation, no environment loading
2. **NOT defense-in-depth:** If someone manually copies sensitive values into appsettings.json, this boundary does not prevent that
3. **NOT binary leak detection:** The proof validates the source artifact (`.g.cs`), not the compiled assembly

## Files Created/Modified

```
examples/dotnet-blazor-wasm-net8-public/
  .env.schema (uses typeName=, path=, publicOnly=true)
  dotnet-blazor-wasm-net8-public.csproj (MSBuild integration)
  Program.cs (POCO consumption, no bridge)
  obj/Varlock/
    VarlockPublicConfig.g.cs (generated during build)
  Components/
    _Imports.razor
    App.razor
    Routes.razor
    Pages/
      Home.razor
  wwwroot/
    index.html
```

## Handoff Notes

- **O'Brien:** Expand `proof:dotnet` to assert:
  1. Generated `.g.cs` (in obj/) contains no `SensitiveKeys`, `IsSensitive`, `PropertyBinding`
  2. Generated `.g.cs` contains no property matching sensitive env var name (API_KEY)
  3. WASM csproj has no Varlock PackageReference entries (MSBuild imports expected)
  4. Build succeeds and `VarlockGenerateTypes` target runs
  5. Update support-matrix ledger: "Blazor WebAssembly public-config-only usage" → proven, with caveat "proves build-time public-only type generation; no runtime Varlock bridge present"

- **Tuvok:** Review complete. All contract constraints met. The example proves the intended boundary honestly with proper MSBuild integration.

- **Picard/Coordinator:** P3-A1c is complete. All three framework examples (Functions, Blazor Server, Blazor WASM) are ready for O'Brien's proof harness integration.

## Related Decisions

- Tuvok's P3-A1c WASM boundary contract (`.squad/decisions/inbox/tuvok-p3-a1c-wasm-boundary.md`)
- Geordi's publicOnly implementation
- Data's P3-A1c framework examples (Functions, Blazor Server)
- ASP.NET MVC example MSBuild pattern (reference implementation)


---

# Decision: P3-A1c WASM Duplicate Generation Validation

**Date:** 2026-03-16  
**Owner:** Data (Bridge/Hosting Lead)  
**Status:** VALIDATED — No duplicate generation issue exists

---

## Summary

Validated the Blazor WASM public-only example and confirmed it correctly follows a single coherent generation story using the MSBuild pattern.

---

## Investigation

User reported duplicate generated C# copies causing build failures:
- Checked-in `examples/dotnet-blazor-wasm-net8-public/Generated/VarlockPublicConfig.g.cs`
- Build-generated `examples/dotnet-blazor-wasm-net8-public/obj/Varlock/VarlockPublicConfig.g.cs`

**Finding:** The current repository state has no such duplicate. Only one generated file exists at `obj/Varlock/VarlockPublicConfig.g.cs`.

---

## Current State (Correct)

### .env.schema
```
@generateTypes(lang=cs, path=obj/Varlock/VarlockPublicConfig.g.cs, 
               namespace=DotnetBlazorWasmNet8Public.Generated, 
               typeName=VarlockPublicConfig, publicOnly=true, auto=false)
```

### .csproj Structure
- Imports `Varlock.MSBuild.props` and `Varlock.MSBuild.targets`
- Sets `VarlockEnabled=true`, `VarlockSchemaPath=.env.schema`, `VarlockGeneratedFile=$(BaseIntermediateOutputPath)Varlock/VarlockPublicConfig.g.cs`
- **No** `Varlock.Extensions.Configuration` package reference (public-only pattern, no runtime bridge)
- **No** explicit `<Compile>` items for generated files (MSBuild targets handle inclusion)

### Git Tracking
```bash
$ git ls-files | grep -i "generated\|varlock.*\.g\.cs"
# (no output — no generated files tracked)
```

### Build Output
```bash
$ dotnet clean && dotnet build
✅ Types generated successfully
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### File System After Build
```bash
$ find . -name "*.g.cs" -type f
./obj/Varlock/VarlockPublicConfig.g.cs
```

Only one generated file. No duplicates.

---

## Key Differences: WASM vs ASP.NET MVC

| Aspect | ASP.NET MVC | Blazor WASM |
|--------|-------------|-------------|
| MSBuild integration | ✅ Props + Targets | ✅ Props + Targets |
| Generated file path | `obj/Varlock/AppConfig.g.cs` | `obj/Varlock/VarlockPublicConfig.g.cs` |
| Runtime package | `Varlock.Extensions.Configuration` | **None** |
| Runtime bridge usage | `AddVarlock()` in Program.cs | **None** (public-only POCO) |
| Purpose | Full bridge + IConfiguration | Build-time generation only |

---

## Public-Only Contract Validation

Generated file at `obj/Varlock/VarlockPublicConfig.g.cs`:
- ✅ Contains only public properties: `AppName`, `AppPort`, `FeatureEnabled`
- ✅ Excludes sensitive property: `API_KEY` (marked `@sensitive` in schema)
- ✅ No `SensitiveKeys` array
- ✅ No `PropertyBinding` class
- ✅ No `IsSensitive` metadata
- ✅ `PropertyKeys` dictionary present (safe metadata)

```bash
$ cat obj/Varlock/VarlockPublicConfig.g.cs | grep -E "(SensitiveKeys|IsSensitive|PropertyBinding|API_KEY)"
# (no output)
✓ No sensitive metadata found
```

---

## Build Validation Summary

1. **Clean state:** `dotnet clean` removes all generated artifacts
2. **Reproducible generation:** `dotnet build` consistently produces single file at `obj/Varlock/VarlockPublicConfig.g.cs`
3. **No duplicates:** Only one `.g.cs` file exists after build
4. **No git-tracked generated files:** Source tree is clean
5. **Public-only contract honored:** Generated file contains no sensitive data or metadata
6. **No runtime bridge:** WASM example uses MSBuild for generation only, not runtime loading

---

## Conclusion

The WASM example is **correctly configured** and follows the established MSBuild pattern. The reported duplicate generation issue was either:
- A transient state during earlier development that has since been corrected
- A misunderstanding of the expected pattern

**Current status:** Build succeeds, single coherent generation story, public-only contract validated.

---

## DO NOT BREAK

1. WASM example **MUST NOT** add `Varlock.Extensions.Configuration` package reference
2. WASM example **MUST NOT** use runtime bridge APIs (`AddVarlock()`, `VarlockRuntime`, etc.)
3. `.env.schema` **MUST** keep `publicOnly=true` in decorator
4. Generated file **MUST NOT** be checked into git (belongs in `obj/` only)
5. MSBuild integration is appropriate for WASM because it's build-time only, not runtime invocation

---

## Related Work

- Tuvok's P3-A1c boundary contract (publicOnly generation)
- Geordi's MSBuild integration implementation
- Data's earlier P3-A1c WASM repair (component fixes)


---

# Decision: P3-A1c publicOnly Support and WinForms net48 Example

**Date:** 2026-03-16  
**Owner:** Geordi (MSBuild & Typegen Lead)  
**Status:** IMPLEMENTED

## Context

P3-A1c required two build/typegen deliverables:
1. `publicOnly` support for C# type generation (blocking Blazor WASM public-config boundary)
2. WinForms net48 example as minimum legacy desktop bridge proof

## Decision

### publicOnly Implementation

Added `publicOnly?: boolean` option to C# type generation with the following contract-faithful behavior:

1. **Exclude sensitive items entirely**: When `publicOnly=true`, items where `isSensitive === true` are filtered out before generating C# class properties
2. **Strip sensitivity metadata**: Public-only generation excludes:
   - `SensitiveKeys[]` array
   - `PropertyBinding` class (which exposes `IsSensitive` property)
   - `PropertyBindings[]` collection
3. **Fail loudly on empty type**: Throws error when all items are sensitive and public-only generation would produce an empty type
4. **Preserve essential metadata**: `PropertyKeys` dictionary remains in public artifacts (maps property names to original env keys without sensitivity information)

Implementation location: `packages/varlock/src/env-graph/lib/type-generation.ts`

### WinForms net48 Example

Created `examples/dotnet-winforms-net48/` with narrowest honest scope:
- Runtime loading via direct `VarlockCliRuntime` API usage
- No MSBuild integration or generated types
- Proves `netstandard2.0` targeting works on legacy .NET Framework 4.8
- **Proof mode**: `--dump-config` flag emits machine-readable JSON to stdout (for automated proof harness)
- **Interactive mode**: Normal run displays MessageBox with configuration (for manual verification)
- **Platform requirement**: Windows + .NET Framework 4.8 Developer Pack required for runtime execution; builds successfully on all platforms

## Validation

1. **Unit tests**: Added 4 new tests covering publicOnly behavior, all passing with `bunx vitest` (36 total tests)
2. **Build verification**: `bun run build:libs` succeeds with no errors
3. **WinForms build**: `dotnet build` on WinForms example succeeds, produces valid PE32 Windows executable
4. **Console example**: Still builds and runs correctly, produces expected JSON output
5. **Proof path**: WinForms example accepts `--dump-config` flag and will emit JSON to stdout (Windows-only runtime execution)

## Rationale

- publicOnly support unblocks Blazor WASM public-config example (owned by Data)
- WinForms example proves legacy desktop targeting without overpromising integration features
- `--dump-config` flag enables honest automated proof without weakening to build-only validation
- Implementation respects v1 scope: no analyzer work, no native runtime expansion
- Generated output remains deterministic and incremental-build friendly

## Platform-Specific Runtime Constraint

**WinForms net48 example runtime execution requires Windows**. The example:
- Builds successfully on all platforms (macOS, Linux, Windows)
- Requires Windows + .NET Framework 4.8 runtime to execute
- Proof harness should conditionally execute on Windows runners only (similar to other net48-specific proof paths)

This is an honest constraint of the .NET Framework 4.8 target, not a limitation of the Varlock implementation.

## Handoff Notes

- **For Data**: publicOnly contract is ready for Blazor WASM example implementation. Use `@generateTypes(lang=cs, publicOnly=true, ...)` decorator in WASM client schema.
- **For Tuvok**: Security boundary contract is implemented as specified in Gap 1 analysis. Sensitive values and sensitivity metadata cannot leak into public bundles.
- **For O'Brien**: WinForms example proof path is `dotnet run -- --dump-config` with Windows-only execution guard. Expected payload shape matches console example (appName, httpPort, featureEnabled, secretIsSensitive, redactLogs, preventLeaks, sourceLabels).


---

# P3-A1c Proof Harness & Support-Matrix Prep Pass

- **Initiative:** `dotnet-support` P3-A1c
- **Agent:** O'Brien (Distribution & Proof Lead)
- **Date:** 2026-03-16
- **Status:** Complete — Ready for implementation once examples land
- **Scope:** Two-wave preparation per Picard's P3-A1c design review

---

## Executive Summary

The current proof harness structure (`scripts/test-dotnet-proof.ts`) exercises console, ASP.NET, and Worker examples through `bun run proof:dotnet`. P3-A1c requires four new examples (Azure Functions isolated, Blazor Server, Blazor WASM public-only, and WinForms net48) distributed across two waves.

**Outcome:** Documented exact proof hooks, output shapes, ledger rows, and implementation blockers. The Wave 1 proof pass is completely conflict-free and can be implemented immediately once Data/Geordi deliver their examples. Wave 2 is blocked pending Geordi+Tuvok's `publicOnly` contract design.

---

## Current Proof Structure (Baseline)

### Execution Model
- **Entry point:** `bun run proof:dotnet` → `scripts/test-dotnet-proof.ts`
- **Pattern:** Single TypeScript file; builds library first, then invokes examples via `dotnet run --no-build`
- **Output parsing:** Tagged JSON lines (prefixes: `CONSOLE_`, `ASPNET_`, `WORKER_`, etc.)

### Example Types & Payloads

#### 1. **Console Example** (`dotnet-console-net8`)
- **Proof modes:** repo-local lookup, package-local lookup, local `.bin` lookup, opt-in PATH lookup
- **JSON payload type:** `ConsolePayload`
  ```typescript
  { appName, httpPort, featureEnabled, secretIsSensitive, redactLogs, preventLeaks, sourceLabels }
  ```
- **Invocation:** `dotnet run --no-build --no-launch-profile` (no CLI flags for base run)
- **Output assertion:** Single JSON object written to stdout

#### 2. **ASP.NET MVC Example** (`dotnet-aspnet-mvc-net8`)
- **Proof modes:** config override validation, user-secrets coexistence, reload-proof (successful), reload-fail-proof, snapshot-proof
- **JSON payload type:** `AspNetPayload`
  ```typescript
  { AppName, AppPort, FeatureEnabled, AppSettingsOnly, SecretTokenPresent, UserSecretsOnly }
  ```
- **Invocation flags:** `--dump-config`, `--reload-proof`, `--reload-fail-proof`, `--snapshot-proof`
- **Output:** Single JSON payload for dump-config; tagged lines for reload proofs
- **Tagged line patterns:**
  - `RELOAD_SUCCESS_*` (successful reload notifications)
  - `RELOAD_FAIL_*` (failed reload behavior)
  - `SNAPSHOT_PROOF_*` (scope isolation validation)

#### 3. **Worker Service Example** (`dotnet-worker-net8`)
- **Proof modes:** dump-config, reload-proof (long-lived monitor), reload-fail-proof
- **JSON payload type:** `WorkerPayload`
  ```typescript
  { AppName, AppPort, FeatureEnabled }
  ```
- **Invocation flags:** `--dump-config`, `--reload-proof`, `--reload-fail-proof`
- **Output:** Single JSON payload for dump-config; tagged `WORKER_*` lines for reload proofs
- **Proof harness:** BackgroundService mutates `.env.schema`, listens to `IOptionsMonitor<T>.OnChange`, emits tagged lines, stops host

---

## Wave 1 Examples (Unblocked — Parallel Track)

### New Example 1: Azure Functions Isolated (`dotnet-functions-isolated-net8`)
**Status:** ✅ Already exists (checked in)

**Configuration:**
- `.env.schema`: `APP_NAME`, `APP_PORT`, `FEATURE_ENABLED` (same structure as console/worker)
- **Options type:** `VarlockFunctionsOptions`
- **Config snapshot:** `FunctionsConfigSnapshot`
- **Program.cs pattern:**
  - Uses `HostBuilder.ConfigureFunctionsWorkerDefaults()`
  - `AddVarlock()` integrated via `IConfiguration` chain
  - **Key behavior:** Loads `local.settings.json` first (Functions-specific), then Varlock on top
  - Supports `--dump-config` flag only (no reload proofs; Functions runtime doesn't support file-watching from the isolated worker process)

**Proof obligations:**
1. **Build check:** `dotnet build` succeeds (same as console/worker)
2. **Startup check:** `dotnet run --no-build -- --dump-config` emits valid JSON
3. **Config assertion:** Varlock keys override `local.settings.json` values
4. **Ledger row:** "Azure Functions isolated worker usage" → **proven**
5. **Additional:** Verify `local.settings.json` coexistence (functions-specific caveat in ledger)

**New proof assertions needed:**
```typescript
const functionsResult = runDotnet(functionsProjectDir, ['run', '--no-build', '--', '--dump-config']);
const functionsPayload = parseJsonOutput<FunctionsPayload>('dotnet-functions-isolated-net8', functionsResult);
// Assert: appName override, appPort coercion, featureEnabled boolean
// Assert: functionsOnlyKey is preserved from local.settings.json
assert(functionsPayload.AppName === 'varlock-functions', '...');
// etc.
```

**New payload type needed:**
```typescript
type FunctionsPayload = {
  AppName: string;
  AppPort: number;
  FeatureEnabled: boolean;
  FunctionsOnlyKey?: string; // Proves local.settings.json is still read
};
```

---

### New Example 2: Blazor Server (`dotnet-blazor-server-net8`)
**Status:** ✅ Already exists (checked in)

**Configuration:**
- `.env.schema`: `APP_NAME`, `APP_PORT`, `FEATURE_ENABLED` (same structure)
- **Options type:** `VarlockBlazorOptions`
- **Config snapshot:** `BlazorConfigSnapshot`
- **Program.cs pattern:**
  - Uses `WebApplication.CreateBuilder()`
  - `AddVarlock()` integrated via `builder.AddVarlock()`
  - Razor components for server-side rendering
  - **Key behavior:** Server-side config only; no client-side public boundary concerns at this stage

**Proof obligations:**
1. **Build check:** `dotnet build` succeeds
2. **Startup check:** `dotnet run --no-build -- --dump-config` emits valid JSON
3. **Config assertion:** Varlock keys properly bound to options
4. **Ledger row:** "Blazor Server usage" → **proven**

**New proof assertions:**
```typescript
const blazorResult = runDotnet(blazorProjectDir, ['run', '--no-build', '--', '--dump-config']);
const blazorPayload = parseJsonOutput<BlazorPayload>('dotnet-blazor-server-net8', blazorResult);
assert(blazorPayload.AppName === 'varlock-blazor-server', '...');
// etc.
```

**New payload type:**
```typescript
type BlazorPayload = {
  AppName: string;
  AppPort: number;
  FeatureEnabled: boolean;
};
```

---

### New Example 3: WinForms Legacy (.NET 4.8)
**Status:** ❌ Not yet created — blocked on Geordi

**Scope:** Non-hosted desktop application, `net48` TFM only

**Configuration:**
- `.env.schema`: Likely minimal (legacy bridge proof, not options binding)
- **Program.cs pattern:** Direct runtime consumption (likely no `IConfiguration` at all; uses low-level APIs like console example)
- **Key behavior:** Proves legacy Windows desktop can use Varlock CLI bridge

**Proof obligations:**
1. **Build check:** `dotnet build -f net48` succeeds on Windows CI
2. **Startup check:** Application invokes Varlock CLI, receives config
3. **Assertion:** Varlock bridge works in legacy non-hosted context
4. **Ledger row:** "Windows Forms legacy/non-hosted usage" → **proven**

**Proof scaffold (awaiting Geordi's example):**
- Likely reuses console pattern or creates a minimal `--dump-config` variant
- May not need new payload type if it mirrors console

---

## Wave 2 (Blocked Pending `publicOnly` Contract)

### New Example 4: Blazor WebAssembly Public-Config-Only (`dotnet-blazor-wasm-net8-public`)
**Status:** ❌ Not yet created — blocked on publicOnly contract design

**Blocking issue:** Geordi+Tuvok must design and deliver the `publicOnly` C# generation contract. This includes:
- CLI flag or MSBuild property for public-only generation
- Definition of how `sensitive: true` items are excluded from generated types
- Fixture expectations for proof assertions

**Proof obligations (post-contract):**
1. **Build check:** `dotnet build` with `publicOnly=true` succeeds
2. **Generated code assertion:** Sensitive fields are absent from generated `.g.cs`
3. **Binary validation:** WASM binary does not contain sensitive values (hardest part; may require binary inspection tooling)
4. **Ledger row:** "Blazor WebAssembly public-config-only usage" → **proven**
5. **Security sign-off:** Tuvok must validate sensitive-value leak prevention

**Placeholder proof scaffold:**
```typescript
// Wave 2: Blazor WASM public-only proof
// Awaiting publicOnly contract from Geordi+Tuvok
// const wasmResult = ...;
// assert(generatedType.sensitiveField === undefined, 'Generated public-only types must exclude sensitive items');
```

---

## Support-Matrix Ledger Changes

### Current Ledger (P3-A1a/b)
7 rows marked **proven**; 14 rows marked **planned**

### P3-A1c Wave 1 Ledger Rows (Status: `planned` → `proven`)

| Row | Support Claim | Proving Example | Proving Test | Caveat | Wave 1? |
| --- | --- | --- | --- | --- | --- |
| +1 | Azure Functions isolated worker usage | `dotnet-functions-isolated-net8` | `bun run proof:dotnet` functions startup check | must document coexistence with `local.settings.json` | ✅ |
| +2 | `local.settings.json` coexistence | `dotnet-functions-isolated-net8` | `bun run proof:dotnet` config layering test | functions-specific only | ✅ |
| +3 | Blazor Server usage | `dotnet-blazor-server-net8` | `bun run proof:dotnet` blazor server startup check | should prove server-side config access only | ✅ |
| +4 | Windows Forms legacy/non-hosted usage | `dotnet-winforms-net48` | legacy desktop bridge smoke test | minimum supported legacy target | ✅ |

**New ledger rows for Wave 1:** 4 rows transition from `planned` → `proven`

### P3-A1c Wave 2 Ledger Row (Status: `planned` → `proven` post-contract)

| Row | Support Claim | Proving Example | Proving Test | Caveat | Wave 2? |
| --- | --- | --- | --- | --- | --- |
| +5 | Blazor WebAssembly public-config-only usage | `dotnet-blazor-wasm-net8-public` | WASM public-config build validation test | must prove sensitive values do not cross the public boundary | ⏳ Blocked |

---

## Implementation Conflict Analysis

### Safe-to-implement-now (Wave 1):
- ✅ Add `FunctionsPayload` and `BlazorPayload` types to `scripts/test-dotnet-proof.ts`
- ✅ Add `functionsProjectDir` and `blazorProjectDir` path constants
- ✅ Add build checks for Functions and Blazor (same `dotnet build` pattern as console/worker)
- ✅ Add dump-config assertions for Functions and Blazor (new assertion blocks, no shared test logic)
- ✅ Update ledger rows for all four Wave 1 examples: 4 rows move from `planned` → `proven`

### Safe-to-prepare-but-NOT-commit (Wave 1):
- 🔒 WinForms probe scaffold (awaiting Geordi's example project and exact payload shape)
- 🔒 Functions reload-proof harness (Functions runtime doesn't support file watching; may need to be explicitly deferred or removed from ledger row)

### Blocked until contract delivery (Wave 2):
- 🚫 Blazor WASM proof harness and payload type
- 🚫 `publicOnly` C# generation validation
- 🚫 Sensitive-value leak prevention assertions

---

## File Inventory

### Files That WILL Change (Wave 1)
1. **`scripts/test-dotnet-proof.ts`** — Add payload types, path constants, build checks, dump-config assertions
   - **Lines to add:** ~200–250 (conservative estimate)
   - **Change type:** Append new test blocks; do NOT edit existing console/worker/aspnet blocks
   - **Risk level:** Low (disjoint additions; existing logic stays intact)

2. **`docs/proposals/dotnet-support.md`** (Support-Matrix Ledger section)
   - **Lines to change:** 4 ledger rows (lines ~800–803)
   - **Change type:** Flip `planned` → `proven` status and update caveats if needed
   - **Risk level:** Low (documentation-only change)

### Files That DO NOT Change Yet (Wave 1)
- ✅ Example projects themselves (already checked in)
- ✅ `.squad/agents/o'brien/history.md` (update post-implementation)
- ✅ Workflow definitions (no new CI lanes needed yet)

### Files Awaiting Blocker Clearance (Wave 2)
- 🚫 `examples/dotnet-blazor-wasm-net8-public/` (awaits Data + `publicOnly` contract)
- 🚫 `scripts/test-dotnet-proof.ts` (Wave 2 payload + assertions)
- 🚫 `docs/proposals/dotnet-support.md` (Wave 2 ledger row)

---

## Exact Proof Hook Output Shapes

### Functions Example — `--dump-config` Output
```json
{
  "appName": "varlock-functions",
  "appPort": 7071,
  "featureEnabled": true,
  "functionsOnlyKey": "preserved-from-local-settings-json"
}
```
**Assertion:** `appPort` is coerced to integer; `functionsOnlyKey` proves `local.settings.json` was still processed.

### Blazor Server Example — `--dump-config` Output
```json
{
  "appName": "varlock-blazor-server",
  "appPort": 5280,
  "featureEnabled": true
}
```
**Assertion:** Config matches Varlock bindings; server-side proof only (no public boundary concerns at this stage).

### WinForms Example (Awaiting Geordi)
- Likely mimics console payload or uses a minimal variant
- Proof will be startup smoke test only (verify CLI bridge works in legacy context)

### Blazor WASM Example (Awaiting Contract)
- Proof must validate generated `.g.cs` excludes sensitive fields
- May require binary inspection to validate leak prevention
- Exact assertion shape depends on `publicOnly` contract details

---

## Summary of Work

**Current session (O'Brien):**
1. ✅ Audited current proof harness structure and ledger
2. ✅ Identified exact proof hooks and output shapes for Wave 1
3. ✅ Documented which proof/ledger changes are conflict-free
4. ✅ Created implementation inventory and file-impact analysis

**Next session (O'Brien, post-examples):**
1. ⏳ Add payload types and test blocks to `scripts/test-dotnet-proof.ts`
2. ⏳ Update ledger rows in proposal (4 rows: `planned` → `proven`)
3. ⏳ Validate proof passes on all three examples before sign-off

**Parallel track (Geordi+Tuvok):**
1. ⏳ Design and deliver `publicOnly` contract spec
2. ⏳ Unblock Blazor WASM example

**Reviewer assignments (per Picard's design review):**
- O'Brien signs off on proof pass
- Tuvok signs off on security boundaries
- Picard accepts the ledger state as meeting P3-A1c closure criteria

---

## Risk Assessment

**Low risk:**
- Functions and Blazor Server examples follow established patterns (hosted, options binding, dump-config)
- New proof assertions are **isolated** from existing console/worker/aspnet blocks
- No shared test infrastructure changes needed

**Medium risk:**
- WinForms proof shape is TBD (awaiting Geordi's deliverable)
- May need platform-specific (Windows-only) assertions in CI

**High risk:**
- Blazor WASM public-only validation is genuinely complex (binary leak detection)
- Requires `publicOnly` contract to be design-complete before implementation can proceed

---

## Handoff Notes for Implementation Session

1. **Wave 1 proof pass is fully unblocked once Data delivers Functions + Blazor examples.**
2. **WinForms scaffold will need careful coordination with Geordi to align proof assertions with example payload shape.**
3. **Wave 2 cannot begin until Geordi+Tuvok lock the `publicOnly` contract.**
4. **All new proof additions must use the `parseTaggedLines()` pattern and emit prefixed JSON for consistency.**
5. **Ledger update must preserve caveats even after `proven` status, per ledger maintenance rules.**



---

# Decision: P3-A1c Proof Harness Repair

**Date:** 2026-03-16  
**Owner:** O'Brien (Distribution & Proof Lead)  
**Status:** IMPLEMENTED

---

## Context

The P3-A1c proof harness in `scripts/test-dotnet-proof.ts` had four known issues preventing honest validation:

1. WinForms build assertion used `getBuildOutputPath()` hardcoding `bin/Debug/net8.0/*.dll`, but WinForms net48 output is `bin/Debug/net48/dotnet-winforms-net48.exe`
2. WinForms runtime proof didn't use `--dump-config` flag despite the example supporting it
3. WinForms runtime proof had no Windows-only guard, causing false failures on non-Windows platforms
4. WASM `PropertyKeys` assertion had been corrected to `["AppName"] = "APP_NAME"` and needed preservation

---

## Decision

### 1. Build Output Path Generalization

Modified `getBuildOutputPath()` signature:

```typescript
// Before
function getBuildOutputPath(projectDir: string, assemblyName: string)

// After
function getBuildOutputPath(
  projectDir: string, 
  assemblyName: string, 
  targetFramework = 'net8.0', 
  extension = 'dll'
)
```

This allows proof assertions to specify non-default target frameworks and output types without duplicating path construction logic.

### 2. WinForms Build Proof Fix

Updated WinForms build assertion:

```typescript
assert(
  fs.existsSync(getBuildOutputPath(winFormsProjectDir, 'dotnet-winforms-net48', 'net48', 'exe')),
  'dotnet build proof should produce the winforms example assembly under bin/Debug/net48.',
);
```

Correctly validates `bin/Debug/net48/dotnet-winforms-net48.exe` output.

### 3. WinForms Runtime Proof Improvements

**Flag addition:**
```typescript
runDotnet(winFormsProjectDir, [
  'run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet',
  '--', '--dump-config'  // NEW: machine-readable JSON output
]);
```

**Platform guard:**
```typescript
if (isWindows) {
  // WinForms runtime assertions
} else {
  console.log('WinForms runtime proof skipped (Windows-only).');
}
```

This aligns WinForms proof with the honest constraint: builds on all platforms, runs on Windows only.

### 4. WASM PropertyKeys Assertion

Verified preservation of corrected assertion:

```typescript
assert(
  wasmGeneratedTypeSrc.includes('["AppName"] = "APP_NAME"'),
  'WASM public-only PropertyKeys must map public property names to original env keys.',
);
```

No changes needed; existing fix preserved.

---

## Validation

1. **TypeScript syntax:** `bunx tsc --noEmit` passes with no errors
2. **WinForms build:** Manually confirmed `dotnet build` produces `bin/Debug/net48/dotnet-winforms-net48.exe`
3. **Surgical targeting:** Only modified WinForms-related code and `getBuildOutputPath()` helper
4. **Existing tests:** Functions, Blazor Server, and WASM proof assertions remain intact

---

## Rationale

- **Honesty:** Proof harness must validate what actually exists, not ideal paths
- **Platform correctness:** net48 WinForms runtime requires Windows; build proof works everywhere
- **Consistency:** `--dump-config` flag is used by console, ASP.NET, Worker, Functions, Blazor Server examples—WinForms should match
- **Maintainability:** Parameterized `getBuildOutputPath()` avoids path-construction duplication

---

## Handoff Notes

- **For CI:** When .NET 8.0 SDK is installed, full `bun run proof:dotnet` will validate all repairs
- **For Geordi:** No MSBuild integration changes needed; repairs are proof-side only
- **For Data:** WinForms example behavior unchanged; proof now correctly invokes `--dump-config`
- **For Tuvok:** Security boundary assertions (WASM public-only) remain untouched and validated

---

## Related Work

- Geordi's P3-A1c decision: `.squad/decisions/inbox/geordi-p3-a1c-publiconly-winforms.md`
- Tuvok's P3-A1c boundary contract: `.squad/decisions/inbox/tuvok-p3-a1c-wasm-boundary.md`
- O'Brien's history entry: 2026-03-16 repair record in `.squad/agents/o'brien/history.md`


---

### P3-A1c Design Review: Two-wave execution with publicOnly contract gate

- **Initiative:** `dotnet-support`
- **Source:** Picard (design review ceremony)
- **Decision Date:** 2026-03-16
- **Status:** Active

---

#### Context

P3-A1c ("Remaining framework examples") requires four new examples: Azure Functions isolated, Blazor Server, Blazor WASM public-config-only, and WinForms net48 legacy. The Blazor WASM example is blocked on a `publicOnly` generation contract that has not been designed or locked. The other three examples have no blocking prerequisites — P3-A1b (Hosting package) is already approved-closed.

#### Decision

P3-A1c executes in **two waves**, not one atomic batch.

**Wave 1 — Unblocked examples (parallel):**
- Data: `dotnet-functions-isolated-net8/` and `dotnet-blazor-server-net8/`
- Geordi: `dotnet-winforms-net48/` (net48 TFM, non-hosted direct runtime)
- O'Brien: Proof harness for all three Wave 1 examples; ledger rows → proven
- Tuvok: Boundary review of all three

**Wave 1 parallel track — publicOnly contract design:**
- Geordi + Tuvok (joint): Design and lock the `publicOnly` C# generation contract
  - Must define: CLI typegen flag or MSBuild property for public-only generation
  - Must define: how `sensitive: true` items are excluded from generated type
  - Must define: fixture expectations for proof harness
  - Deliverable: written contract spec in the proposal or a decisions.md entry

**Wave 2 — After publicOnly contract is locked:**
- Data: `dotnet-blazor-wasm-net8-public/`
- O'Brien: Blazor WASM proof harness; ledger row → proven
- Tuvok: Security boundary sign-off (sensitive values do not cross public boundary)

#### Rationale

1. Three of four examples are unblocked. Holding them for a design gap on the fourth violates the established "prove infrastructure before building on top" principle.
2. The publicOnly contract is a real design decision that touches the Varlock CLI typegen surface — it cannot be hand-waved into an implementation detail.
3. Wave split lets the contract design happen in parallel with example work, then Wave 2 is a fast follow-on.

#### Constraints

- **Gate 1:** Wave 2 CANNOT begin until Geordi+Tuvok deliver the publicOnly contract design and Picard accepts it.
- **Gate 2:** P3-A1c CANNOT close until all four ledger rows are proven and Tuvok signs off on the Blazor WASM boundary proof.
- **Reviewer lockout:** If any Wave 1 example fails review, the revising agent must differ from the original author (standard rule).

#### Scope Boundaries

- No Serilog work in P3-A1c (deferred to P3-A1d).
- No P4-A1 leakage (native runtime, analyzer).
- WinForms targets net48 only — no net472 or older.
- Azure Functions uses isolated worker model only — no in-process.


---

# P3-A1c Final Lead Review — APPROVE-CLOSE

**Date:** 2026-03-16
**Owner:** Picard (Initiative Lead)
**Status:** APPROVED — P3-A1c is complete

---

## Verdict

**APPROVE-CLOSE.** All four P3-A1c deliverables are honestly implemented and proven. The support-matrix ledger language is narrow enough to be true. No scope leakage into P3-A1d or P4-A1 was detected.

---

## Deliverable-by-Deliverable Assessment

### 1. Azure Functions Isolated (`dotnet-functions-isolated-net8/`) — ✅ PASS

- **Integration pattern:** `ConfigureAppConfiguration` → `IConfigurationBuilder.AddVarlock()` (correct for Functions isolated worker where `HostBuilder` doesn't expose `Configuration` directly)
- **Coexistence:** `local.settings.json` with `FUNCTIONS_ONLY_KEY` preserved alongside Varlock keys; proven via `--dump-config` assertion that `typeof functionsPayload.FunctionsOnlyKey === 'string'`
- **Ledger language:** "must document coexistence with `local.settings.json`; proven via `--dump-config` assertion that Varlock overrides function configuration and local.settings.json keys are preserved" — narrow and true
- **Dedicated coexistence row:** "functions-specific only; proven via `--dump-config` assertion that functionsOnlyKey from local.settings.json is preserved when Varlock is added to configuration chain" — honest scope bounding

### 2. Blazor Server (`dotnet-blazor-server-net8/`) — ✅ PASS

- **Integration pattern:** `builder.Configuration.AddVarlock()` on `WebApplicationBuilder` (correct — `WebApplicationBuilder` is not a `HostApplicationBuilder`, so hosting extension doesn't apply)
- **Proof:** Runtime `--dump-config` assertions for AppName, AppPort, FeatureEnabled
- **Ledger language:** "should prove server-side config access only; proven via `--dump-config` assertion that Varlock overrides Blazor Server configuration" — narrow and true

### 3. Blazor WASM Public-Only (`dotnet-blazor-wasm-net8-public/`) — ✅ PASS

- **Integration pattern:** Build-time-only POCO generation via `publicOnly=true` in `@generateTypes` decorator. No runtime bridge, no `IConfiguration` provider, no CLI invocation. The generated `.g.cs` is the only Varlock artifact in the WASM bundle.
- **Schema design:** Includes `@sensitive` `API_KEY` alongside three non-sensitive items — correct test of the boundary
- **Proof:** 9 assertions on generated `.g.cs`: 4 negative (SensitiveKeys, PropertyBinding, IsSensitive, API_KEY absent), 3 positive (AppName, AppPort, FeatureEnabled present), 2 metadata (PropertyKeys dictionary, key mapping)
- **Ledger language:** "proves build-time public-only type generation boundary; no runtime Varlock bridge is present in WASM bundles; sensitive metadata (SensitiveKeys, PropertyBinding, IsSensitive, and sensitive property names) are excluded from generated `.g.cs`; proven via generated-file validation" — thorough and honest. Correctly uses "generation boundary" not "security boundary."
- **Contract alignment:** Fully satisfies Tuvok's locked `publicOnly` contract (all 4 guarantees met, DO NOT BREAK constraints respected)

### 4. WinForms net48 (`dotnet-winforms-net48/`) — ✅ PASS (with minor note)

- **Integration pattern:** Direct `VarlockCliRuntime` API usage (non-hosted) — correct for legacy desktop target without `IConfiguration`
- **Proof:** `--dump-config` JSON payload with 7 assertions (appName, httpPort, featureEnabled, secretIsSensitive, redactLogs, preventLeaks, sourceLabels). Runtime proof properly gated: `if (isWindows) { ... } else { console.log('WinForms runtime proof skipped (Windows-only).') }`
- **Build proof:** Cross-platform `dotnet build` assertion for `net48` binary in `bin/Debug/net48/`
- **Ledger language:** "non-hosted direct runtime usage on .NET Framework 4.8; proven via `dotnet run` assertion that legacy desktop applications can invoke the Varlock CLI bridge and consume validated configuration" — the implicit Windows constraint is defensible since .NET Framework 4.8 is inherently Windows-only, but see recommendation below

---

## Ledger Honesty Assessment

| Row | Verdict | Notes |
|-----|---------|-------|
| Functions isolated worker | ✅ Honest | Coexistence narrowly scoped and proven |
| `local.settings.json` coexistence | ✅ Honest | Functions-specific; key preservation asserted |
| WinForms legacy/non-hosted | ✅ Honest | Platform constraint implicit but defensible |
| Blazor Server | ✅ Honest | Server-side only, narrow claim |
| Blazor WASM public-only | ✅ Honest | "generation boundary" framing is precise |
| Explicit `dotnet build` (all 7 examples) | ✅ Honest | Cross-platform build proof for full set |

---

## Scope Leakage Check

- **Serilog references:** 0 (correctly deferred to P3-A1d) ✅
- **Analyzer/SourceGenerator references:** 0 (correctly deferred to P4-A1) ✅
- **New packages beyond P3-A1c scope:** 0 ✅
- **P4-A1 native runtime work:** 0 ✅

No leakage detected.

---

## Type Generation — publicOnly Contract

- **Implementation:** Filtering at line 467 in `type-generation.ts` — `items.filter((info) => !info.isSensitive)`
- **Metadata stripping:** PropertyBinding, SensitiveKeys, IsSensitive all omitted when `publicOnly=true`
- **Safety check:** Error thrown when all items are sensitive (empty-type guardrail)
- **Golden fixture:** `PublicOnlyConfig.g.cs` anchors regression detection — excludes sensitive items, retains PropertyKeys
- **Tests:** 5 dedicated publicOnly tests within the 36-test suite, all passing
- **Contract match:** Implementation satisfies Tuvok's locked contract in full

---

## Proof Harness Verification

`bun run proof:dotnet` passes on this machine with:
- All 7 examples built successfully
- Functions, Blazor Server runtime proofs pass
- WinForms runtime proof skipped with explicit message (Windows-only)
- WASM public-only boundary assertions pass (9 assertions)
- Console, ASP.NET, Worker proofs continue to pass (no regressions)

---

## Minor Recommendation (Non-Blocking)

The WinForms ledger row caveats column should add "runtime proof is Windows-only; build proven cross-platform" for clarity. Currently the implicit .NET Framework 4.8 = Windows constraint is defensible, but an explicit callout would be more self-documenting. This is a polish item, not a blocker.

---

## Decision

**P3-A1c → APPROVE-CLOSE.**

All four framework examples are implemented, the proof harness covers each honestly, the `publicOnly` generation contract is locked and tested, the ledger language is narrow enough to be true, and no scope has leaked forward. P3-A1c is complete.

**Next node:** P3-A1d (Serilog + security boundary specimen) is now unblocked.


---

# Decision: P3-A1c Blazor WASM Public-Only Boundary Contract

**Date:** 2026-03-16  
**Owner:** Tuvok (Contracts & Security Lead)  
**Status:** APPROVED — publicOnly generation contract is locked

---

## Executive Summary

The `publicOnly` generation contract is implemented, tested, and anchored by a golden-file fixture. It satisfies the Gap 1 blocking requirement from the P3-A1 boundary analysis. The Blazor WASM example can now be built by Data using `@generateTypes(lang=cs, publicOnly=true, ...)`.

---

## Contract Definition

### What `publicOnly=true` generation guarantees

When `@generateTypes(lang=cs, publicOnly=true, ...)` is used in a `.env.schema`:

1. **Sensitive items excluded from generated class:** Properties where `isSensitive === true` are not emitted as C# class members. Their names, types, and default values are absent from the generated file.

2. **Sensitivity metadata excluded entirely:**
   - `SensitiveKeys[]` static array: **absent**
   - `PropertyBinding` class (which carries `IsSensitive`): **absent**
   - `PropertyBindings[]` collection: **absent**

3. **Preserved metadata (safe for public bundles):**
   - `PropertyKeys` dictionary: **present** (maps PascalCase property names to original env keys — no sensitivity information)
   - XML doc comments on remaining properties: **present** (no sensitivity-related comments because filtered items are non-sensitive by definition)

4. **Empty-type guardrail:** If all items in the schema are sensitive, `publicOnly=true` generation throws a build-time error: `"all items are sensitive; public-only generation would produce an empty type"`.

5. **Type validation:** `publicOnly` must be a boolean. Non-boolean values (including string `"true"`) are rejected at parse time.

### Implementation location

- **Source:** `packages/varlock/src/env-graph/lib/type-generation.ts` (lines 387–561)
- **Tests:** `packages/varlock/src/env-graph/test/type-generation.test.ts` (5 tests)
- **Golden fixture:** `packages/varlock/src/env-graph/test/fixtures/typegen-cs/PublicOnlyConfig.g.cs` (NEW)

### Pipeline verification

The `publicOnly` option flows through the full decorator pipeline:
1. `@generateTypes(lang=cs, publicOnly=true, ...)` in `.env.schema`
2. Decorator parser resolves → `typeGenSettings.obj.publicOnly === true`
3. `EnvGraph.generateTypesIfNeeded()` → `EnvGraph.generateTypes(lang, path, obj)` → `generateCsTypesSrc(items, options)`
4. `resolveCsTypeGenerationOptions(options)` validates and resolves `publicOnly`
5. `generateCsTypesSrc` filters items before emission

---

## DO NOT BREAK Constraints

### For Data (Blazor WASM example builder)

1. The WASM example's `.env.schema` **MUST** include `publicOnly=true` in its `@generateTypes` decorator.
2. The schema **MUST** have at least one non-sensitive item (otherwise generation fails by design).
3. The WASM example **MUST NOT** use `VarlockConfigurationProvider` or `AddVarlock()` at runtime — WASM apps cannot invoke the Varlock CLI. The example proves generated-type consumption only, not bridge loading.
4. The generated `.g.cs` is the **only** Varlock artifact in the WASM bundle. There is no runtime bridge, no CLI invocation, and no `IConfiguration` provider.

### For O'Brien (proof harness and ledger)

1. The proof **MUST** assert that the generated `.g.cs` from `publicOnly=true` contains **none** of: `SensitiveKeys`, `IsSensitive`, `PropertyBinding`, or any property name matching a sensitive env var from the schema.
2. The proof does **NOT** need binary inspection. If the generated `.g.cs` passes the above assertions and is the only generated Varlock artifact consumed by the WASM app, the boundary is proven. Binary inspection is overkill for v1.
3. The ledger row for "Blazor WebAssembly public-config-only usage" **MUST** say "public-only generation boundary" — not "security boundary" or "sensitive value protection." This is scope control, not defense-in-depth.
4. The ledger caveat **MUST** include: "WASM example proves build-time public-only type generation; no runtime Varlock bridge is present."

### For everyone

1. The golden fixture at `fixtures/typegen-cs/PublicOnlyConfig.g.cs` is a regression artifact. If the generated output for `publicOnly=true` changes shape, this test breaks visibly in diffs.
2. The `publicOnly` default is `false`. Existing examples and the existing golden fixtures (`VarlockConfig.g.cs`, `CustomizedAppConfig.g.cs`) are unaffected.
3. No new bridge error categories, no new CLI flags, no new .NET packages are needed for this boundary.

---

## Scope Clarification: What This Is NOT

1. This is **not runtime protection.** There is no `VarlockConfigurationProvider` in WASM, no bridge invocation, no environment variable loading. The WASM app consumes a generated POCO at compile time.
2. This is **not defense-in-depth.** If someone manually copies sensitive values into a WASM project's `appsettings.json`, Varlock's `publicOnly` generation cannot prevent that. The contract is: "Varlock's own generated artifacts do not contain sensitive information."
3. This is **not binary leak detection.** The proof validates the source artifact (generated `.g.cs`), not the compiled assembly. This is sufficient because the generated file is the only Varlock-controlled artifact entering the WASM bundle.

---

## Reviewer Gates

1. **Tuvok** (this document): Contract locked. Generation boundary is sound.
2. **Geordi**: Implementation delivered and tested. APPROVED.
3. **Data**: May now build the Blazor WASM example using the constraints above.
4. **O'Brien**: May now add Wave 2 proof assertions using the constraints above.
5. **Picard/Coordinator**: No further design review needed for the publicOnly generation contract. The WASM example itself should undergo normal code review.

---

## Related Decisions

- P3-A1 boundary gap analysis (Gap 1: blocking → resolved)
- Geordi's P3-A1c publicOnly+WinForms implementation decision
- O'Brien's P3-A1c proof prep (Wave 2 now unblocked)

---

## 2026-03-16: P3-A1d Security-Boundary Contract

- Initiative: `dotnet-support`
- Source: Tuvok
- Decision: Locks the narrowest true security-boundary contract for .NET v1 with exactly two new public APIs: `WithVarlockRedaction()` and `VarlockRedactionHelper.Redact()`.
- Serilog scope: Destructuring policy that replaces sensitive values with `[REDACTED]` via exact case-sensitive key matching. Does NOT intercept string-template parameters, console, MEL, or non-Serilog pipelines.
- Non-Serilog helper: Manual per-value redaction in `Varlock.DotNet`. No automatic interception. Caller must explicitly invoke `VarlockRedactionHelper.Redact()`.
- Metadata enrichment: `WithVarlockMetadata()` appends `VarlockRedactLogs` boolean to log events. This is metadata-only; it does not cause redaction.
- Forbidden language: Do NOT use "protection" for either API. Do NOT claim "automatic" redaction for non-Serilog path. Do NOT claim `PreventLeaks` is "enforced" in v1.
- Deferred: Reload-aware Serilog re-registration, MEL integration, HTTP middleware, configurable redaction placeholder.
- Proof obligations: Unit tests for destructuring redaction, metadata enrichment, and helper fallback. ASP.NET MVC example shows Serilog destructuring. Console example shows manual helper AND unredacted raw output (proves nothing automatic).

---

## 2026-03-16: P3-A1d Design Review & Phase 3 Closeout Plan

- Initiative: `dotnet-support`
- Source: Picard
- Decision: Design review issues PROCEED verdict. P3-A1a, P3-A1b, P3-A1c are APPROVE-CLOSED. Remaining work is P3-A1d: Serilog package, security-boundary specimen, ledger completion.
- Minimum batch: `Varlock.Serilog` at `packages/dotnet/Varlock.Serilog/` (netstandard2.0, depends on Serilog + Varlock.DotNet). `VarlockRedactionHelper` in `Varlock.DotNet`. ASP.NET MVC Serilog example. Console non-Serilog fallback example. Proof harness exercises both paths. Support-matrix ledger complete. Proposal Phase 3 exit criteria updated.
- Acceptance criteria (10): Varlock.Serilog exists. VarlockRedactionHelper exists. ASP.NET MVC example proven. Console example proven. `bun run proof:dotnet` exercises both, all pass. No remaining "planned" ledger rows for Phase 3. Phase 3 exit criteria updated. Tuvok approves language. Serilog vs. non-Serilog distinction concrete. WASM public-only boundary maintained.
- Gating: Tuvok's contract lock before implementation. Standard reviewer lockout (if rejected, other agent revises). Picard final review requires all three streams (Tuvok, Data, O'Brien) to pass independently.

---

## 2026-03-16: P3-A1d Redaction Implementation

- Initiative: `dotnet-support`
- Source: Data
- Decision: Implementation complete. `Varlock.Serilog` ships with exactly two locked extension methods. `WithVarlockRedaction()` uses private `IDestructuringPolicy` snapshots sensitive-key set at registration time, applies exact case-sensitive replacement to destructured properties and `IDictionary` string keys. Plain scalar message-template parameters remain outside. `WithVarlockMetadata()` uses private enricher appending `VarlockRedactLogs` boolean from captured graph snapshot. `VarlockRedactionHelper.Redact()` in `Varlock.DotNet` is manual non-Serilog fallback using same exact key lookup with literal `"[REDACTED]"`.

---

## 2026-03-16: Phase 3 APPROVE-CLOSE

- Initiative: `dotnet-support`
- Source: Picard
- Decision: Final lead review issued APPROVE-CLOSE for Phase 3 complete. All 10 acceptance criteria met. Tuvok's locked security-boundary contract satisfied. No scope leakage detected.
- Deliverable verification: `Varlock.Serilog` exists with exact API surface (netstandard2.0, Serilog + Varlock.DotNet dependencies). `VarlockRedactionHelper` in `Varlock.DotNet` with manual `Redact()` method. ASP.NET MVC demonstrates Serilog destructuring redaction. Console demonstrates non-Serilog manual redaction and raw leak (proving nothing automatic). `bun run proof:dotnet` exercises both paths, all assertions pass (32 tests total).
- Support-matrix complete: Four P3-A1c rows proven (Azure Functions, Blazor Server, WinForms, WASM public-only). Two P3-A1d rows proven (Serilog destructuring, non-Serilog fallback). Phase 3 exit criteria all struck through with proof references.
- Scope leakage: Zero analyzer, source-gen, or native-runtime references. `publicOnly` changes are P3-A1c carry-forward. Remaining planned rows (3: `dotnet watch`, `IOptions<T>`, plugin resolution) correctly marked deferred.
- Phase 3 status: APPROVED-CLOSED. Phase 4 DEFERRED (no active implementation).


---
