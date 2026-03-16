# Picard — History

## Core Context

- **Project:** A first-class Varlock .NET support initiative built around a v1 CLI bridge, proof artifacts and support-matrix validation, and an explicit path to future native runtime or analyzer evolution.
- **Role:** Lead
- **Joined:** 2026-03-13T10:56:25.543Z

## Learnings

<!-- Append learnings below -->
- 2026-03-13: The first executable .NET slice must finish phase-0 contract and executable-distribution work before broader package implementation; no existing .NET package tree or .csproj artifacts are present, so early work should bias toward fixtures, acquisition, and C# generation proof rather than hosting or native/runtime ambition.
- 2026-03-13: Data and O'Brien aligned the first mergeable slice around a startup-only CLI bridge into `.NET` configuration, and they require that slice to carry executable acquisition, contract fixtures, console proof, ASP.NET provider proof, and one CI path rather than broadening into hosted reload features.
- 2026-03-13: When asking Matthew to choose priorities, delegation, creation, or next `.NET` steps, Picard should anchor the prompt to `.squad/progression.md` and cite the relevant stable node IDs instead of offering free-floating options.
- 2026-03-13: P1-A1 now has a checked-in proof path for repo-local executable lookup, so coordination can treat that narrow development layout as proven while still keeping broader acquisition-matrix expansion as follow-on proof work.
- 2026-03-13: Picard narrowed the next P1-A2 proof row to machine-readable diagnostics, and that row is now accepted only through the shared CLI bridge fixtures consumed by the `.NET` alignment tests, with location-bearing coverage called out as a separate caveat until the fixture set expands.
- 2026-03-15: P2-A1 scope decision: O'Brien's tighter boundary accepted over Data's broader recommendation. `Varlock.Extensions.Hosting` and `examples/dotnet-worker-net8/` are OUT for P2-A1. The single Ralph run focuses on `ReloadOnChange`, `ReloadFailureBehavior`, file watcher, debounced coalescing, atomic swap, change-token integration, and last-known-good preservation — all within the existing `Varlock.Extensions.Configuration` package. `IOptionsMonitor<T>.OnChange` proof reuses the existing ASP.NET MVC example. New packages and new example projects are deferred to P2-B1 or P3-A1.
- 2026-03-15: When two agents recommend different scope for a single autonomous run, prefer the tighter boundary. It is always safer to prove core mechanics first and layer convenience on top in a follow-on pass than to risk a half-built package across a broad surface area.
- 2026-03-15: P2-A1 complete. Next node is P2-B1 (MSBuild integration). Rationale: reload is stable, proof passes, no blockers remain. Phase gate alignment demands MSBuild completion before P3-A1 convergence. Decision written to `.squad/decisions/inbox/picard-next-node-after-p2a1.md`.
- 2026-03-15: P2-A1 closeout finalized. Orchestration log and session log written. Decisions merged from inbox into `decisions.md`. Progression board updated to mark P2-B1 as `next`. P2-B1 scope documented (MSBuild integration, generated C# to obj/generated, ASP.NET example proof). No blockers for P2-B1 commencement. Ready for Matthew's acknowledgment to delegate P2-B1 work.
- 2026-03-15: P2-B1 first cut reviewed and accepted. Geordi's `Varlock.MSBuild` slice is contract-respecting, deterministic, and proof-ready. Core findings: (1) Incremental proof is powerful — validating timestamp preservation (not just content equality) catches rebuild-loop regressions. (2) Design-time guards prevent IDE bloat — `DesignTimeBuild` guard is a critical MSBuild pattern when mixing code generation with IDE operations. (3) Fallback chains are resilient — reusing P1-A1's executable discovery in MSBuild targets scales well. (4) Tight scope pays off — deferring optional validation, watch, and packaging allows this slice to be small and proof-ready in a single pass. P2-B1 marked `in progress`; both Geordi and O'Brien can proceed in parallel with no phase blockers.
- 2026-03-16: P3-A1 decomposed into four ordered sub-batches (A1a through A1d) because the full Phase 3 scope (CI parity, hosting, 5 new examples, Serilog, security boundary, ledger completion) is too broad for one autonomous run. Key sequencing insight: cross-platform CI parity (P3-A1a) must come first because platform-specific bugs in executable resolution or path handling would block every subsequent example. This follows the established pattern: prove infrastructure before building on top of it.
- 2026-03-16: P3-A1 carries significant deferred work from earlier phases — `Varlock.Extensions.Hosting` (P2-A1 deferral), Worker Service example (P2-A1/P2-B1 deferral), WinForms legacy target (P1 deferral), `IOptionsSnapshot<T>` scoped proof (P2-A1 deferral). These deferrals were correct at the time but they stack up at the convergence point. Future initiatives should consider whether a convergence node can absorb all upstream deferrals or whether a separate "deferred cleanup" node is needed.

## P2-B1 Closure Review (2026-03-15T16:54:02Z)

- 2026-03-15T16:54:02Z: Picard completed P2-B1 phase-gate review. Evidence: real NuGet packageability proven (pack → consume → build → generate → bind chain); post-commit validation green (`dotnet test --filter ReloadTests`, `bun run proof:dotnet`); asset structure canonical (build/ + buildTransitive/ paths); proposal ledger updated. Scope boundaries honest: README documents no bundled executable, separate validation step, or watch behavior — all deferred to P3-A1. Decision: **APPROVE-CLOSE.** P2-B1 complete. Both P2-A1 and P2-B1 closed; P3-A1 (wider platform proof: Windows, macOS, CI parity) now unblocked.

## P3-A1 Completion (2026-03-15–16)

- **2026-03-15T20:50:57Z (P3-A1c):** Picard issued APPROVE-CLOSE for P3-A1c (remaining framework examples). Four new framework examples proven: Azure Functions isolated, Blazor Server, WinForms net48, Blazor WASM public-only generation boundary. publicOnly type generation contract locked by Tuvok and Geordi. Support-matrix ledger expanded by 4 rows. No scope leakage; P3-A1d unblocked.
- **2026-03-15T23:08:17Z (P3-A1d):** Picard conducted final lead review and issued APPROVE-CLOSE for Phase 3 complete. All 10 acceptance criteria met. Tuvok's security-boundary contract locked and satisfied. Data implemented `Varlock.Serilog` + `VarlockRedactionHelper` with exact API surface. O'Brien expanded proof harness to exercise both Serilog and non-Serilog paths. All 7 examples pass `bun run proof:dotnet`. Support-matrix complete (7 framework examples + 2 redaction rows proven). Phase 3 exit criteria all struck through with proof references. Zero scope leakage detected (zero analyzer, source-gen, native-runtime references). **Phase 3 → DONE. Phase 4 DEFERRED.**

## P3-A1a First Review (2026-03-16)

- 2026-03-16: P3-A1a lead review REJECTED. Two blocking issues found: (1) CI workflow runs proof:dotnet on Windows/macOS without building the JS libraries first — the varlock CLI dist is a build artifact, not tracked in git, so proof will fail immediately. (2) Proof harness creates `.cmd` wrappers for the package-local path on Windows, but the .NET runtime hard-codes `cli.js` for that lookup, so the harness is never found. Additionally, a pre-existing escalation surfaced: `VarlockCliRuntime.RunProcess` passes `.js` paths directly to `ProcessStartInfo.FileName` with `UseShellExecute=false`, which can't execute `.js` files on Windows. MSBuild targets already handle this (prepend `node`), but the C# runtime doesn't. Reviewer lockout applied: O'Brien revises test.yaml (Geordi's artifact), Data revises proof script and runtime (O'Brien's artifact). Key lesson: cross-platform CI expansion must be tested on actual CI runners, not just local machines where build artifacts already exist from prior runs.

## P3-A1a Final Review — APPROVE-CLOSE (2026-03-16)

- 2026-03-16: All three previously-blocking issues resolved. (1) CI workflow now builds libs on all platforms before proof:dotnet. (2) `FindExecutableInBinDirectory` prefers `.cmd` on Windows, matching proof harnesses. (3) `CreateProcessStartInfo` routes `.js` files through `node` on Windows with proper `QuoteArgument` quoting — mirrors what MSBuild targets already did. New `Load_executes_repo_local_js_entrypoint_without_explicit_executable_path` test covers the full resolution-to-execution chain with a mock CLI, including marker-file proof that the correct entrypoint ran. Scope stayed inside P3-A1a: no new packages, no new examples, no P4-A1 leakage. Minor recommendation logged: consider `fail-fast: false` on the matrix strategy in a follow-up so platform failures don't cancel the Linux full-suite job.

## P3-A1a Lead Review (2026-03-16)

Led review of P3-A1a (cross-platform CI parity) implementation. Identified two blocking issues and one escalation, assigned to O'Brien and Data for revision:

1. **CI workflow:** `build:libs` gated to Linux-only, causing immediate failure on Windows/macOS
2. **Proof harness:** Package-local harnessing creates `.cmd` instead of `.js`, failing discovery
3. **Runtime:** Pre-existing `.js` execution gap on Windows (no `node` prefix)

Reassignments issued per reviewer lockout rule. P3-A1a sequencing (four sub-batches A1a/b/c/d) locked and awaiting Matthew's approval. Boundary review from Tuvok cleared P3-A1a as contract-safe.

**Status:** Awaiting fixes and re-review.

## P3-A1b Lead Review — APPROVE-CLOSE (2026-03-16)

- 2026-03-16: P3-A1b approved-closed on first review pass. All four deliverables present and correct: (1) `Varlock.Extensions.Hosting` is a ~30-line pure delegation layer with two `HostApplicationBuilder.AddVarlock()` overloads matching the updated proposal surface. (2) Worker example properly uses `BackgroundService` with `IOptionsMonitor<T>` for reload proof in a long-lived hosted service. (3) `IOptionsSnapshot<T>` scoped-reload proof added to ASP.NET example — strongest possible demonstration with scope isolation across reload boundaries. (4) Proof script comprehensively expanded with worker build/dump/reload/reload-fail assertions plus snapshot assertions. Ledger updated honestly. No scope leakage. Minor follow-ons: `IHostBuilder` overloads deferred, `fail-fast: false` still pending, no dedicated hosting unit tests (acceptable for pure delegation). P3-A1b → DONE, P3-A1c → NEXT.

## Learnings

- 2026-03-16: P3-A1c design review identified that the `publicOnly` C# generation contract is an unlocked prerequisite for Blazor WASM but does NOT block the other three framework examples. Splitting into two waves (Functions+Blazor Server+WinForms first, then Blazor WASM after contract lock) follows the established "prove infrastructure before building on top" principle while avoiding unnecessary serial bottlenecks.
- 2026-03-16: WinForms net48 is a non-hosted target — it uses direct `VarlockCliRuntime` like the console example, not `HostApplicationBuilder.AddVarlock()`. Geordi owns this because it's a build/TFM concern (netstandard2.0 compatibility with net48, MSBuild targets for legacy SDK-style projects).
- 2026-03-16: The publicOnly contract is a real product-surface decision, not an implementation detail. It touches the Varlock CLI typegen surface (needs a `--public-only` flag or equivalent) and affects how MSBuild targets invoke generation for Blazor WASM projects. This must be designed by Geordi+Tuvok jointly and accepted by Picard before Data builds the example.

## P3-A1c Final Lead Review — APPROVE-CLOSE (2026-03-16)

- 2026-03-16: P3-A1c approved-closed after full deliverable audit. All four framework examples are honestly implemented and proven:
  1. **Azure Functions isolated** — `ConfigureAppConfiguration` → `AddVarlock()` with `local.settings.json` coexistence proven (FUNCTIONS_ONLY_KEY preservation assertion).
  2. **Blazor Server** — `builder.Configuration.AddVarlock()` with runtime `--dump-config` proof.
  3. **Blazor WASM public-only** — build-time POCO generation via `publicOnly=true`; 9 boundary assertions on generated `.g.cs` (sensitive metadata absent, non-sensitive properties present, PropertyKeys retained). No runtime bridge in WASM bundle. Satisfies Tuvok's locked contract.
  4. **WinForms net48** — direct `VarlockCliRuntime` (non-hosted), 7 runtime assertions on `--dump-config` payload. Runtime proof properly gated to Windows-only; build proof cross-platform.
- Ledger language verified narrow and honest across all four new rows. Functions coexistence and WASM boundary caveats are precise. WinForms implicit Windows constraint is defensible.
- `publicOnly` type generation: 5 dedicated tests, golden fixture anchored, implementation satisfies full contract (sensitive item filtering, metadata stripping, empty-type guardrail).
- Scope leakage check: 0 Serilog references, 0 analyzer/source-generator references, 0 new packages beyond scope. Clean.
- Minor non-blocking recommendation: WinForms ledger row could add "runtime proof is Windows-only; build proven cross-platform" for self-documentation.
- P3-A1c → DONE. P3-A1d (Serilog + security boundary) now unblocked.

### Learnings

- 2026-03-16: The two-wave execution strategy for P3-A1c (unblocked examples first, WASM after contract lock) delivered exactly as designed — Wave 1 and Wave 2 both completed without blocking each other, and the contract gate prevented premature WASM work. This pattern should be reused whenever a sub-batch has mixed dependency profiles.
- 2026-03-16: For platform-gated proof rows (like WinForms net48), the ledger caveats column should explicitly state the platform constraint on the proof execution, not just the runtime target. Implicit constraints are defensible but explicit ones are self-documenting.

---

## P3-A1c Lead Review & APPROVE-CLOSE (2026-03-15T20:50:57Z)

**Session:** P3-A1c closeout consolidation  
**Role:** Initiative lead, final design authority, APPROVE-CLOSE gate

Completed final lead review of all P3-A1c deliverables (Azure Functions isolated, Blazor Server, Blazor WASM public-only, WinForms net48). Verified:
- All four examples implement correctly and prove according to specification
- Ledger rows narrowly scoped and honestly proven
- Zero scope leakage (no P3-A1d or P4-A1 references)
- All team decisions consolidated to decisions.md

**Decision:** APPROVE-CLOSE P3-A1c. P3-A1d (Security boundary + Serilog + ledger completion) now unblocked.

**Status:** COMPLETE
