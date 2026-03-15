# Data — History

## Core Context

- **Project:** A first-class Varlock .NET support initiative built around a v1 CLI bridge, proof artifacts and support-matrix validation, and an explicit path to future native runtime or analyzer evolution.
- **Role:** Runtime Lead
- **Joined:** 2026-03-13T10:56:25.544Z

## Learnings

<!-- Append learnings below -->
- 2026-03-16: The `.NET` bridge must preserve lookup order but change launch hosting on Windows when the resolved Varlock executable ends in `.js`; with `UseShellExecute=false`, repo-local and package-local JS entrypoints need `node "<cli.js>" ...` rather than direct execution, and a proof test should use a path containing spaces so quoting regressions fail loudly.
- **2026-03-15: P3-A1 Inventory Complete.** The wider-platform proof node is unblocked with a clear gap analysis: 2 of 7 required example platforms proven (console, ASP.NET MVC), 5 pending (Worker, Functions, Blazor Server, Blazor WASM, WinForms). The bridge runtime is solid; all new work is examples and proof tests. Key dependencies: Serilog scope decision (likely defer to Phase 4), Blazor WASM public-config generation design (Geordi ownership), and platform priority callout (Matthew decision). Recommend phased execution: Worker + Functions (Phase A), Blazor (Phase B pending WASM design), WinForms (Phase C), ledger closure (Phase D). No new runtime packages required unless Serilog or unexpected Hosting patterns emerge from examples.
- The first `.NET` bridge slice should stop at startup-only `IVarlockRuntime` plus `IConfigurationProvider` integration; pulling in hosting helpers or reload semantics before the bridge contract is stable mixes too many failure modes.
- 2026-03-13: Picard fixed the slice boundary so the bridge skeleton should not start before Tuvok's contract fixtures and O'Brien's executable-acquisition specimen are concrete, and the first delivery must stay honest to the proof scope those artifacts support.
- 2026-03-13: The startup configuration provider should flatten structured Varlock values into ordinary `IConfiguration` child paths and avoid synthetic JSON shadow entries, so binder behavior stays predictable while the CLI contract hardens.
- 2026-03-13: Tuvok fixed the first bridge handshake at `varlock load --format json-full --bridge-contract 1` with JSON stdout envelopes for both paths, so the runtime can key off explicit failure categories while keeping `executable-not-found` on the caller side.
- 2026-03-13: The .NET runtime must parse the bridge envelope even on nonzero CLI exits, because contract failures now arrive as stdout JSON plus exit status rather than as stderr-only process failures.
- 2026-03-13: Keeping a tiny internal raw-graph parsing fallback behind the runtime seam lets local validation cover the contract migration without broadening the public API or delaying the bridge handshake switch.
- 2026-03-13: The canonical ledger now fixes the post-launch runtime contract on the stdout bridge envelope and narrows the raw `json-full` fallback to cases where no envelope markers are present, so future bridge work should not reintroduce mixed success-path parsing.
- 2026-03-13: The smallest honest P1-A1 step is to separate launch-time acquisition from post-launch contract parsing by doing a preflight mismatch probe against the existing `load --bridge-contract` surface instead of inventing a new CLI command.
- 2026-03-13: Runtime lookup hardening can improve beyond the explicit repo-relative proof by preferring package-local install layouts and repo-local development layouts before optional `PATH` discovery, which keeps deterministic local behavior without broadening into packaging automation.
- 2026-03-13: O'Brien fixed the proof ledger so P1-A1 can advance with deterministic lookup and preflight handshake hardening while docs and support rows stay pinned to the explicit `ExecutablePath` proof until checked-in examples or CI cover fallback lookup.
- 2026-03-13: The checked-in examples can now honestly omit `ExecutablePath` and prove the runtime's repo-local development lookup from `examples/*` up to `packages/varlock/bin/cli.js`, but that still does not prove package-local, `node_modules/.bin`, or opt-in `PATH` acquisition.
- 2026-03-13: The squad ledger now records that the checked-in proof path really does run without `ExecutablePath` and proves only the repo-local development layout, so broader acquisition fallbacks stay implementation-only until separately exercised.
- 2026-03-13: The smallest honest next P1-A1 proof step after repo-local lookup is a proof-only package-local wrapper under `examples/dotnet-console-net8/node_modules/varlock/bin/cli.js` that delegates to the real CLI and leaves a marker, because that proves actual acquisition precedence without distorting the checked-in example or widening support claims beyond `node_modules/.bin` and opt-in `PATH`.
- 2026-03-13: The package-local proof harness is now a squad decision, so the honest proven no-`ExecutablePath` surface for P1-A1 is repo-local plus package-local lookup only; `node_modules/.bin` and opt-in `PATH` remain unproven until separate artifacts exercise them.
- 2026-03-13: The next honest P1-A1 increment was to prove `node_modules/.bin/varlock` with the same marker-wrapper pattern used for package-local lookup, which let the docs and ledger advance one lookup branch without silently promoting opt-in PATH fallback.
- 2026-03-13: The checked-in proof harness and successful `bun run proof:dotnet` validation now prove repo-local, package-local, and local `node_modules/.bin/varlock` acquisition with `ExecutablePath` omitted, leaving opt-in `PATH` fallback as the only still-unproven P1-A1 lookup branch.
- 2026-03-13: The smallest honest PATH proof is an env-guarded console-example seam that disables local lookup only for `bun run proof:dotnet`, letting a temporary PATH wrapper prove the opt-in branch without changing default example behavior or silently broadening runtime semantics.
- 2026-03-13: After the env-guarded PATH seam landed and fresh `dotnet test ... --filter ResolveExecutable` plus `bun run proof:dotnet` validation passed, the stale proof-planning inbox note was superseded by the checked-in repo state and the canonical P1-A1 lookup surface is now fully proven through repo-local, package-local, local `.bin`, and opt-in PATH acquisition.
- 2026-03-13: The smallest honest P1-A2 expansion on the current startup/runtime slice is User Secrets coexistence on the existing ASP.NET MVC example, because WebApplicationBuilder already loads User Secrets in Development and the proof only needs to show User Secrets-only keys survive while `AddVarlock(...)` still overrides overlapping keys by provider order.

## P3-A1a Runtime Fixes (2026-03-16)

Fixed two critical P3-A1a blocking issues:

1. **Windows JS Entrypoint Hosting Decision:** When resolved executable path ends in `.js` on Windows, prefix with `node` in `VarlockCliRuntime.RunProcess`. Rationale: preserves executable-resolution honesty; matches MSBuild bridge pattern.
   - Verification: `dotnet test` + `bun run proof:dotnet` both pass locally
   - Regression test added for `.js` path with spaces (Windows quoting)

2. **Proof Harness `.js` vs `.cmd` Fix (Issue 2):** Package-local harness must create Node.js-executable wrappers (`.js`) on all platforms, not batch files (`.cmd`) on Windows. Runtime's `FindNodeModulesPackageExecutable` is hard-coded to search for `.js`.
   - Assigned from O'Brien via Picard reviewer lockout
   - Status: Fix in progress

## P3-A1 Inventory & Phasing (2026-03-15)

Provided implementation guidance for P3-A1:
- Mapped 5 unproven app-type examples (Worker, Functions, Blazor variants, WinForms)
- Recommended four-phase execution: Phase A (Worker/Functions foundation), Phase B (Blazor, requires design), Phase C (WinForms legacy), Phase D (closure)
- Identified 4 critical decisions needed before P3-A1b starts (platform priority, Serilog scope, snapshot proof, CI matrix timing)

**Status:** P3-A1a fixes pending; P3-A1 phasing documented and ready for Matthew's decisions.
