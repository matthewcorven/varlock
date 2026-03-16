# Tuvok — History

## Core Context

- **Project:** A first-class Varlock .NET support initiative built around a v1 CLI bridge, proof artifacts and support-matrix validation, and an explicit path to future native runtime or analyzer evolution.
- **Role:** Security/Contracts Lead
- **Joined:** 2026-03-13T10:56:25.546Z

## P4-B1 Wave 1 Documentation Delivery (W1-7, W1-9)

- 2026-03-16T22:15:00Z: Tuvok completed P4-B1 Wave 1 documentation deliverables W1-7 (security-and-logging guide) and W1-9 (migration guide). Commit: 32f2b56. Both guides prioritize narrow, testable security claims grounded in P3-A1d contract and P2-A1 reload contract.

**W1-7 (security-and-logging.mdx) key decisions:**
- Serilog destructuring redaction is the primary v1 path: exact case-sensitive key matching, `[REDACTED]` replacement, destructuring only (not string templates)
- VarlockRedactionHelper is the non-Serilog fallback: manual, caller-invoked, per-value, zero automatic interception
- @redactLogs and @preventLeaks are metadata only in .NET v1 — no enforcement by Varlock code
- Blazor WASM public-only boundary is generation-time gate, not runtime check — generated file excludes sensitive properties, SensitiveKeys[], PropertyBindings
- Explicit comparison table shows what JS runtime does (console redaction, response scanning, env injection) vs. .NET v1 (none automatic; Serilog + helper only)
- Forbidden language enforced: "redactable through" (not "protected"), "manually redactable" (not "safe"), "metadata" (not "enforcement")

**W1-9 (migration.mdx) key decisions:**
- appsettings.json coexistence: Varlock as additional IConfigurationSource with configurable precedence (OverrideExisting default, PreserveExisting option)
- DotEnv/dotenv migration: replace loader with AddVarlock(), define schema, use generated types or direct Items access
- Optional configuration: `Optional = true` allows missing .env file but requires valid schema
- Type generation: setup via @generateTypes() decorator, happens at build time via MSBuild, binds with Configure<T>()
- Hosted patterns: WebApplicationBuilder, HostApplicationBuilder.AddVarlock() extension, low-level ConfigurationBuilder for WinForms/Console
- Environment-specific: .env.{EnvironmentName} sourcing, EnvironmentName property configuration
- Reload: ReloadOnChange = true with KeepLastKnownGood behavior; detailed linking to configuration.mdx guide
- Azure Functions (isolated + in-process), User Secrets coexistence, Docker/container, CI/CD env-var setup
- Troubleshooting linked to diagnostics guide (not written yet, but placeholders established)

**Boundary enforcement:**
- security-and-logging.mdx explicitly states what Serilog redaction does NOT do (Console.WriteLine, MEL, HTTP, etc.) and provides code examples showing manual helper requirement
- Both guides link together and to related guides (configuration.mdx, type-generation.mdx, diagnostics.mdx, getting-started.mdx) — establishing expected doc tree
- No support claims beyond contract scope; all statements are testable against existing P3-A1d proofs

## Learnings

<!-- Append learnings below -->
- 2026-03-13: Picard assigned Tuvok first ownership of the machine-readable bridge contract, including a versioned success shape, stable error categories, and reproducible fixtures for success, missing executable, version mismatch, schema invalid, resolution failed, and plugin load failed.
- 2026-03-13: Data's bridge slice depends on those contract outputs before the initial `Varlock.DotNet` and configuration-provider skeleton should advance, so contract stability is the gating artifact for the first runtime implementation.
- 2026-03-13: The first engine-side bridge slice now uses `varlock load --format json-full --bridge-contract 1` with JSON envelopes on stdout for both success and failure; executable discovery remains a caller-side contract because the CLI cannot truthfully self-report launch failures.
- 2026-03-13: Data fixed the bridge scaffold so structured values flatten into ordinary `IConfiguration` child keys without JSON shadow entries, and the low-level parser keeps today's unversioned `json-full` success payload behind an internal seam until the explicit handshake is enforced end to end.
- 2026-03-13: The current P1-A2 diagnostics proof should reuse the checked-in CLI load-bridge fixtures from the `.NET` alignment tests instead of duplicating payloads, while keeping location-aware failure parsing covered by a narrow targeted test until the CLI fixture set includes that shape.
- 2026-03-13: A malformed schema parse error (`# @defaultSensitive(` followed by a normal entry) yields a real `schema-invalid` bridge envelope with `.env.schema:3:1`, which now serves as the shared location-bearing fixture for both CLI and .NET bridge-alignment tests.
- **2026-03-15T23:08:17Z (P3-A1d Security Boundary):** Tuvok locked the security-boundary contract for .NET v1, defining exact scope of Serilog and non-Serilog redaction guarantees and constraints. Serilog scope: `WithVarlockRedaction()` destructuring policy using exact case-sensitive key matching, replacing sensitive values with `[REDACTED]` during object destructuring (`{@obj}` only, not string templates). `WithVarlockMetadata()` enricher appending `VarlockRedactLogs` metadata (no redaction enforcement). Non-Serilog scope: `VarlockRedactionHelper.Redact()` in Varlock.DotNet — manual per-value redaction, zero automatic interception. Explicitly unsupported: process-wide console interception, MEL/ILogger integration, HTTP middleware, configurable placeholder. Deferred: reload-aware Serilog re-registration, reload-time policy update, process output filtering. Proof obligations: Serilog destructuring unit tests, non-Serilog fallback unit tests, ASP.NET MVC example demonstration, console example showing both helper AND raw leak (no automatic). Ledger constraints: forbidden words list (no "protection", no "automatic" for non-Serilog, no "enforcement" of PreventLeaks). DO NOT BREAK block: exact API shapes, existing public API signatures, all 7 bridge error categories, package constraints (netstandard2.0, Serilog + Varlock.DotNet dependencies). Decision locked before implementation; Picard approved language as honest. **P3-A1d APPROVE-CLOSED.**

## P2-A1 Contract Analysis (2026-03-15)

- 2026-03-15: Tuvok completed contract-consistency pass for P2-A1 reload work; machine-readable boundaries documented:
  - Public API stability preserved on `VarlockConfigurationSource` (new: `ReloadOnChange` and `ReloadFailureBehavior` properties only)
  - Existing bridge envelope shapes (success + 7 error categories) remain parseable via `VarlockCliRuntime.ParseCliOutput()`
  - Optional-schema startup semantics unchanged; reload extends observation window
  - Last-known-good preservation is non-negotiable: failed reloads keep previous `Data`, no change-token fire
  - Atomic configuration swap required: single-assignment semantics into `Data` before token fires
  - Watch-set recomputation from new graph only after successful reload, not after failed attempts
  - Change-token integration: fire at most once per successful reload cycle, never after failure
  - Proof-harness output shapes (console and ASP.NET payloads) remain observable and testable
  - All 7 existing bridge error categories reused in reload path; no new categories
  - BridgeContractAlignmentTests fixtures and assertions must pass unchanged

## P2-B1 Contract Review (2026-03-15T16:54:02Z)

- 2026-03-15T16:54:02Z: Tuvok completed P2-B1 contract-stability pass. Reload work preserves public API surface stability (`VarlockConfigurationSource` additions are additive only). Bridge envelope shapes remain unchanged; all 7 existing error categories reusable in reload path. Contract boundaries preserved: last-known-good preservation, atomic swap semantics, change-token fire rules, and watch-set recomputation all validated against P2-A1 proof fixtures. No new error categories; existing `BridgeContractAlignmentTests` assertions remain valid. Approved for closure.

## P3-A1 Boundary & Security Gap Analysis (2026-03-15)

- 2026-03-15: Tuvok completed P3-A1 gap inventory across public/private config, diagnostics, security boundaries, and support claims. Full analysis written to `.squad/decisions/inbox/tuvok-p3a1-boundary-gaps.md`.
- 2026-03-15: The Blazor WASM public-config-only boundary is the hardest security contract in P3-A1. Current type generation emits ALL properties including sensitive metadata (`SensitiveKeys[]`, `PropertyBindings[].IsSensitive`). For WASM, generated output must provably EXCLUDE sensitive items and their values, not merely mark them. This is a generation-time gate, not a runtime check, and requires a joint design decision with Geordi before the example can be built.
- 2026-03-15: `RedactLogs` and `PreventLeaks` are parsed from bridge metadata and stored as booleans on `VarlockResolvedGraph`, but no .NET code acts on them. The JS runtime patches console output and HTTP responses; the .NET side passes the flags through without enforcement. This is the correct v1 stance per the proposal's Security Behavior Scope, but it means the security-boundary specimen must explicitly demonstrate what is NOT automatic.
- 2026-03-15: No `Varlock.Serilog` package exists. The proposal's Phase 3 exit criteria require it. Its public API surface (`WithVarlockMetadata()`, `WithVarlockRedaction()`) needs a contract analysis before implementation to prevent the API from implying broader redaction guarantees than Serilog destructuring actually provides.
- 2026-03-15: Plugin support documentation does not exist in the .NET packages. The proposal requires documenting supported plugin packaging, discovery modes, executable layouts, and how plugin failures surface. The `plugin-load-failed` bridge fixture proves the failure path but no positive-path plugin example exists. Plugin documentation must be written before any plugin-backed secret resolution example can be claimed as "proven".
- 2026-03-15: Worker Service, Azure Functions isolated, and Blazor Server examples are safe to build now using existing `AddVarlock()` + bridge APIs. No new .NET package code is needed for these three. They exercise existing contracts in new hosting contexts.
- 2026-03-15: The `examples/README.md` correctly notes planned features as planned, not proven. The package READMEs (`Varlock.DotNet/README.md`, `Varlock.Extensions.Configuration/README.md`) are minimal and do not mention security caveats. When these packages are documented for consumers, the security boundary (Serilog-only redaction, metadata-only PreventLeaks, no automatic HTTP interception) must be stated plainly.

## P3-A1a Contract Review (2026-03-16)

- 2026-03-16: Tuvok reviewed P3-A1a diff (CI matrix expansion, platform-adaptive proof harness, scope-state updates). No contract or security regression found. Bridge contract v1, 7 error categories, and public API surface are all untouched. The proof harness changes are purely platform-adaptation (Windows `.cmd` wrappers, EEXIST symlink handling, chmod skip).
- 2026-03-16: Flagged CI build dependency: `proof:dotnet` on Windows/macOS runs without `build:libs`, but the ASP.NET example's MSBuild target (`VarlockGenerateTypes`) invokes `node packages/varlock/bin/cli.js typegen`, which imports from `../dist/cli/cli-executable.js`. The `dist/` directory is gitignored and only produced by `build:libs`. On a clean CI checkout without `build:libs`, the proof will fail on Windows/macOS. This is an operational issue, not a contract regression — the failure is visible and honest, not silent.
- 2026-03-16: Scope-state updates (now.md, progression.md) are accurate. P3-A1a is marked "IN PROGRESS" and does not claim completion. The P3-A1 decomposition into sub-batches (a through d) correctly sequences security-boundary work (P3-A1d) last. No support claims are overclaimed.
- 2026-03-16: Final P3-A1a review pass — all prior findings resolved. CI now builds libs on all platforms before proof. `CreateProcessStartInfo()` detects `.js` on Windows and prepends `node`, matching the MSBuild targets' existing pattern. `FindExecutableInBinDirectory()` prefers `.cmd` over `.js` on Windows. New `Load_executes_repo_local_js_entrypoint_without_explicit_executable_path` test proves the full round-trip (resolve → handshake → load) with a temp fake CLI script, including a path-with-spaces test. Bridge contract v1, 7 error categories, and public API surface all untouched. APPROVED.

## P3-A1b Contract Analysis (2026-03-16)

- 2026-03-16: Tuvok completed P3-A1b contract analysis for `Varlock.Extensions.Hosting` package, Worker Service example, and `IOptionsSnapshot<T>` proof. Full analysis written to `.squad/decisions/inbox/tuvok-p3a1b-contract-analysis.md`. APPROVED FOR IMPLEMENTATION.
- 2026-03-16: `Varlock.Extensions.Hosting` minimum API is exactly two `HostApplicationBuilder.AddVarlock()` overloads that delegate to `builder.Configuration.AddVarlock()`. Zero new contracts, zero new bridge interaction, zero DI registrations. The package is pure convenience sugar (~30-40 lines of production code).
- 2026-03-16: `IServiceCollection.AddVarlock()` proposed in `dotnet-support.md` line 376 — DEFERRED. No clear semantics without `IConfigurationBuilder` access. Overclaim risk. If future work needs `IVarlockRuntime` in DI, that belongs in a purpose-built extension.
- 2026-03-16: `ReloadOnChange` default MUST remain `false` in the hosting package. Changing it would create a behavioral split between `builder.Configuration.AddVarlock()` and `builder.AddVarlock()`, violating least surprise.
- 2026-03-16: Worker Service example exercises existing `VarlockConfigurationProvider` reload semantics in `BackgroundService` context. No new reload behavior, no new error categories, no custom hosted-service lifecycle. Provider disposal handled by `IConfiguration` root disposal during host shutdown — no Worker-specific code needed.
- 2026-03-16: `IOptionsSnapshot<T>` is entirely Microsoft.Extensions.Options infrastructure — zero Varlock code changes. Proof must demonstrate per-scope isolation (two requests spanning a reload see different values), NOT "per-request reload." Documentation claim: "reflects the latest successful configuration state per scope/request."
- 2026-03-16: Hosting package dependency chain: `Varlock.Extensions.Hosting` → `Varlock.Extensions.Configuration` → `Varlock.DotNet`. Must NOT reference `Varlock.DotNet` directly. Package targets `netstandard2.0` with `Microsoft.Extensions.Hosting.Abstractions` dependency.

## P3-A1b Final Review (2026-03-16)

- 2026-03-16: Tuvok completed final P3-A1b contract review. All four mandatory constraints from the contract analysis are satisfied: (1) exactly two `HostApplicationBuilder.AddVarlock()` overloads that delegate to `builder.Configuration.AddVarlock()`, (2) `IServiceCollection.AddVarlock()` deferred and removed from proposal, (3) `ReloadOnChange` default unchanged, (4) `IOptionsSnapshot<T>` proved as per-scope isolation not per-request reload. Worker example exercises existing contracts without extending them. Proposal ledger updates are honest — Worker Service and `IOptionsSnapshot<T>` rows moved to proven, remaining items stay planned. No new error categories, diagnostics, security claims, or plugin boundaries. APPROVED.

## P3-A1 Boundary & Contract Analysis (2026-03-15)

Performed comprehensive boundary, security, and contract gap analysis for P3-A1:

**Key Findings:**
1. **Blazor WASM public-config boundary (BLOCKING):** Needs `publicOnly` generation flag design (joint with Geordi)
2. **Security enforcement (DOCUMENTED):** Correct v1 stance (metadata-only, Serilog-only). Needs three sub-proofs: Serilog, non-Serilog fallback, WASM boundary
3. **Diagnostics:** Stable; minor Serilog enrichment contract needed
4. **Plugin behavior:** Documentation missing; no new categories needed
5. **Support claims:** 5 unproven app types identified (Worker, Functions, Blazor variants, WinForms)

**Blocked Design Decisions (Before P3-A1b/c/d):**
1. Blazor WASM public-only generation boundary (Geordi + Tuvok)
2. Varlock.Serilog API surface (Tuvok contract → Data implementation)

**P3-A1a Boundary Review:** No contract regression. Approved as contract-safe.

**Status:** P3-A1a boundary cleared; queued for P3-A1b design work (parallel start).

## P3-A1c publicOnly Contract Pass (2026-03-16)

- 2026-03-16: Tuvok completed P3-A1c contract/security pass for the Blazor WASM public-only boundary. The `publicOnly` generation contract is implemented by Geordi, tested with 4 unit tests, and now anchored by a new golden-file fixture at `packages/varlock/src/env-graph/test/fixtures/typegen-cs/PublicOnlyConfig.g.cs`.
- 2026-03-16: The `publicOnly=true` contract provably excludes: sensitive C# properties, `SensitiveKeys[]` array, `PropertyBinding` class (with `IsSensitive`), and `PropertyBindings[]` collection. It preserves `PropertyKeys` dictionary (no sensitivity info). It throws at build time if all items are sensitive.
- 2026-03-16: The full decorator pipeline is verified: `@generateTypes(lang=cs, publicOnly=true)` → decorator parser → `resolveCsTypeGenerationOptions` (validates boolean) → `generateCsTypesSrc` (filters sensitive items before emission). No runtime component exists; this is generation-time scope control only.
- 2026-03-16: Key constraint for Data: the WASM example MUST NOT use `VarlockConfigurationProvider` or `AddVarlock()`. WASM apps cannot invoke the CLI. The example proves generated-type consumption only.
- 2026-03-16: Key constraint for O'Brien: proof does NOT need binary inspection. Assert the generated `.g.cs` excludes `SensitiveKeys`, `IsSensitive`, `PropertyBinding`, and sensitive property names. That is sufficient for v1. Ledger language must say "public-only generation boundary" — not "security boundary."
- 2026-03-16: Gap 1 from the P3-A1 boundary analysis (Blazor WASM public-config boundary) is now resolved. Decision written to `.squad/decisions/inbox/tuvok-p3-a1c-wasm-boundary.md`.

## P3-A1d Security-Boundary Contract (2026-03-16)

- 2026-03-16: Tuvok locked the P3-A1d security-boundary contract for implementation. Decision written to `.squad/decisions/inbox/tuvok-phase3-security-boundary.md`.
- 2026-03-16: `Varlock.Serilog` public API is exactly two extension methods: `WithVarlockRedaction(LoggerDestructuringConfiguration, VarlockResolvedGraph)` and `WithVarlockMetadata(LoggerEnrichmentConfiguration, VarlockResolvedGraph)`. No other public types. The destructuring policy uses exact case-sensitive key matching against `IsSensitive` keys — not substring, not regex, not value-content matching.
- 2026-03-16: `VarlockRedactionHelper.Redact(VarlockResolvedGraph, string, string)` is the non-Serilog fallback, living in `Varlock.DotNet`. It is manual, caller-invoked, per-value. The console example must prove both the redaction AND the absence of automatic redaction (raw value output without the helper).
- 2026-03-16: Neither `WithVarlockRedaction` nor `VarlockRedactionHelper` checks or enforces `graph.RedactLogs`. They operate unconditionally when called. `RedactLogs` is metadata for consumer decisions, not a library switch.
- 2026-03-16: Reload-aware Serilog policy re-registration is explicitly deferred to Phase 4. The v1 policy captures a graph snapshot at registration time. This is a documented limitation, not a bug.
- 2026-03-16: Forbidden language: "protection" for redaction, "automatic" for non-Serilog, "enforced" for PreventLeaks, "safe" for sensitive values. Correct language: "redactable through Serilog destructuring" or "manually redactable via helper."
- 2026-03-16: If Serilog's `IDestructuringPolicy` cannot match by property name alone (type-based dispatch), Data may use an alternative Serilog extension point. The contract binds the observable behavior (sensitive → `[REDACTED]` in output), not the internal mechanism. Data must flag mechanism changes to Tuvok before shipping.

## P4-A1/E3 Contract Evolution Assessment (2026-03-16)

- 2026-03-16: Tuvok completed P4-A1/E3 contract evolution assessment. Written to `docs/proposals/dotnet-phase4-contract-evolution.md`. Bridge-contract v1 is sufficient for all proven scenarios. No v2 trigger conditions are currently met.
- 2026-03-16: v2 would only be justified if (a) E1 latency data shows >500ms p95 reload delays requiring incremental protocol, (b) a Roslyn analyzer needs schema introspection without full load, or (c) a .NET feature requires plugin capability discovery. None currently apply.
- 2026-03-16: The handshake probe mechanism (`--bridge-contract 0` → version-mismatch response) already provides forward-compatible version negotiation. A future CLI supporting v2 would advertise `supportedContractVersion: 2` and the .NET client could opt in without breaking v1 consumers.
- 2026-03-16: JS runtime's console patching (`kWriteToConsole` hook) and HTTP response patching (`ServerResponse.prototype.write/end`) are architecturally impossible to replicate in .NET without invasive runtime hooks. The CLR does not expose equivalent interception surfaces.
- 2026-03-16: The only justified security expansion is `BuildEnvironmentVariables()` — a trivial utility projecting graph items into `Dictionary<string, string>` for child-process setup. Already in proposal design (line 317). Must be labeled "environment preparation" not "varlock run parity."
- 2026-03-16: Future security packages (`Varlock.Logging` for MEL, `Varlock.AspNetCore` for response scanning) are architecturally valid but not justified without demonstrated demand. Both must be opt-in and must not claim parity with JS automatic behavior.
- 2026-03-16: Full .NET-native plugin parity (C# resolvers/decorators) is a native-runtime decision, not a plugin extension. It requires a .NET parser for `@env-spec`, a .NET resolution engine, and ongoing maintenance parity with the JS engine. Not justified by any current evidence.

---

## P4-A1 Closeout — E3 Evaluation Accepted (2026-03-16)

- 2026-03-16: Picard accepted E3 (Contract & Security Boundary Evolution) without revision. All four required sections present: contract stability assessment (v1 sufficient), security boundary completeness (honestly documented gaps vs. Node.js), plugin evolution tiering (minimum viable vs. full parity), and go/no-go recommendations. Nine scenarios evaluated against v1. Security-boundary language precise: no "protection" claims, honest about what .NET architecture allows vs. prevents. Recommendations tied to proposal exit criteria lines 992–995. `BuildEnvironmentVariables()` convenience utility authorized as follow-on (small, non-blocking). Decision grounds both native-runtime NO-GO and plugin-expansion NO-GO verdicts.

- 2026-03-16: P4-B1 Wave 1 W1-7 & W1-9 completion: Completed security-and-logging guide (Serilog destructuring primary, manual helper fallback, metadata-only flags, WASM boundary, forbidden language enforcement) and migration guide (appsettings coexistence narrative, DotEnv path, type generation, hosted vs. non-hosted patterns, Azure/Docker scenarios). Both grounded in P3-A1d security boundary contract and P2-A1 reload contract. Submitted inbox decision capturing design decisions, boundary enforcement, and cross-links. Wave 1 marked DONE.
