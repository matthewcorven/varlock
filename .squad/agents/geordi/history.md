# Geordi — History

## Core Context

- **Project:** A first-class Varlock .NET support initiative built around a v1 CLI bridge, proof artifacts and support-matrix validation, and an explicit path to future native runtime or analyzer evolution.
- **Role:** Tooling Lead
- **Joined:** 2026-03-13T10:56:25.545Z

## Learnings

<!-- Append learnings below -->
- 2026-03-16: P3-A1c publicOnly support implemented: Added `publicOnly` boolean option to C# type generation that excludes sensitive items from the generated class, strips `SensitiveKeys[]` and `PropertyBinding.IsSensitive` metadata from public artifacts, and fails loudly when all items are sensitive. The implementation faithfully follows the security boundary contract: sensitive config never crosses into Blazor WASM bundles.
- 2026-03-16: P3-A1c WinForms net48 example added: Created minimal legacy desktop bridge proof at `examples/dotnet-winforms-net48/` using the narrowest honest scope (runtime loading only, no MSBuild integration or generated types). The example builds successfully and proves `netstandard2.0` Varlock.DotNet targeting works on legacy .NET Framework 4.8.
- 2026-03-16: Type-generation test suite uses `vitest` CLI (`bunx vitest`), not `bun test`, to avoid `expect.getState()` compatibility issues with Bun's test runner. All 35 existing tests plus 4 new publicOnly tests pass cleanly with vitest.
- 2026-03-16: WinForms proof path fixed with `--dump-config` flag: Added explicit command-line argument handling to emit machine-readable JSON to stdout when flag is present, otherwise display MessageBox for interactive runs. Enables honest automated proof on Windows runners without blocking on UI or weakening to build-only validation.
- 2026-03-16: P4-A1 E2 (Roslyn evaluation) completed: Documented current CLI-generated MSBuild DX baseline (build-time types, incremental via Inputs/Outputs, zero .NET dependencies, ~50ms first-build overhead), hypothetical Roslyn IIncrementalGenerator benefits (real-time IDE preview, build-without-CLI stubs, analyzer diagnostics), implementation costs (2-8 weeks + C# schema parser maintenance or Node interop), and dotnet watch loop analysis (low risk, clean separation between build-time typegen and runtime reload). Recommendation: create thin wrapper package satisfying DoD line 1020 minimally, defer Roslyn implementation until demonstrated DX friction in real projects.

- 2026-03-13: Picard assigned Geordi the first C# generation specimen as phase-1 work that can advance in parallel once naming and output expectations are fixed.
- 2026-03-13: O'Brien's proof scope means the generation slice should travel with representative schema, `.g.cs` golden output, and binder-validation proof if `lang=cs` is included in the first implementation slice.
- 2026-03-13: The first isolated `lang=cs` slice can live entirely inside `packages/varlock` by emitting a flat POCO plus sidecar metadata for original keys and sensitive items, leaving binder attributes and MSBuild packaging for the next phase.
- 2026-03-13: The canonical specimen shape is now fixed at a flat `Varlock.Generated.VarlockConfig` POCO with PascalCase property names, metadata sidecar output, and checked-in schema plus `.g.cs` golden fixtures, with binder proof and packaging still deferred.
- 2026-03-13: For P1-B1, adding direct `ConfigurationKeyNameAttribute` output would prematurely force a Microsoft.Extensions compile-time dependency into every generated C# consumer, so the safer deepening move is a richer sidecar `PropertyBindings` shape that preserves key-to-property, required, and sensitive metadata for later binder/MSBuild work.
- 2026-03-13: O'Brien kept P1-B1 honest in the proof ledger, so richer generated metadata can move forward as implementation work, but support-matrix claims stay planned until an example app consumes the output or compiled binder validation exists.
- 2026-03-13: A small but durable follow-on seam for P1-B1 is letting `@generateTypes(lang=cs, ...)` override the emitted namespace and root type name while deriving the metadata sidecar name predictably, so future MSBuild integration can place generated symbols correctly without post-processing.
- 2026-03-13: That naming seam is now an active squad decision: `namespace` and `typeName` overrides are the binder-friendly next step, `${typeName}Metadata` stays derived, and invalid overrides should fail inside `packages/varlock` without adding binder or MSBuild dependencies.
- 2026-03-15: P1-B1 autonomous completion via Ralph: identified TypeScript typing gap in `packages/varlock/src/env-graph/lib/type-generation.ts` (items not explicitly typed as `Array<Promise<TypeGenItemInfo>>` before `Promise.all()`), applied fix autonomously, verified via `bun run build:libs`, type-generation tests (31/31), and `bun run proof:dotnet`.
- 2026-03-15: The first safe `P2-B1` cut is an opt-in `Varlock.MSBuild` props/targets layer that drives the existing deterministic `varlock typegen` flow into `obj/Varlock/`, adds the generated file to `@(Compile)`, and proves unchanged builds do not rewrite the `.g.cs` output.
- 2026-03-15: P2-B1 kickoff: produced first MSBuild typegen cut wiring `varlock typegen` into MSBuild with opt-in `.props` / `.targets`, generating C# into `obj/Varlock/`, and proving no rewrite on unchanged second build. Decision documented in `decisions.md`.
- 2026-03-15: Shipping validation for the paired P2-A1/P2-B1 slice is green on targeted ReloadTests, `bun run proof:dotnet`, and a follow-up `dotnet build -p:DesignTimeBuild=true` against the ASP.NET example; claim deterministic MSBuild generation and design-time compile inclusion only, with `dotnet watch` still explicitly unproven.

## Learnings (Continued)

- 2026-03-15: P3-A1a implementation: `.github/workflows/test.yaml` refactored to matrix strategy across three OS (ubuntu-latest, windows-latest, macos-latest). ESLint/build/tests remain Linux-only via conditional `if: matrix.full-suite`; `.NET` proof runs on all platforms. Zero package changes, zero proof harness changes. Locally validated on Linux: `bun run proof:dotnet` still passing.
- 2026-03-15: Cross-platform CI parity ready for merge and GitHub Actions execution. Known blockers (Windows .exe discovery, macOS binary availability) are proof-harness concerns owned by O'Brien, not workflow syntax. CI matrix syntax valid and minimal.

---

## Learnings (Original Session Boundary)
- 2026-03-15: P3-A1 build slice is deterministic and low-risk: CI matrix expansion (Windows + macOS jobs), net48 console example using direct VarlockRuntime API, proof script platform detection for executable naming and conditional example runs. No changes to core MSBuild packages (props/targets already platform-agnostic).
- 2026-03-15: net48 example cannot use HostApplicationBuilder or IHost; must use legacy non-async pattern and direct `VarlockRuntime.Load()`. This validates netstandard2.0 targeting, not modern hosting parity.
- 2026-03-15: P3-A1 does not include analyzer/native-runtime work, dotnet watch/IDE-only behaviors, WinForms/Worker/Functions app examples, or Blazor WebAssembly variants; those remain explicitly deferred per proposal and Picard's charter boundaries.
- 2026-03-15: Proof script platform detection strategy: `process.platform === 'win32'` includes net48 example (Windows only), others use net8. Deterministic validation (second build unchanged, `.g.cs` hash match) applies to all platforms. User secrets / reload proofs stay ASP.NET-only, already proven platform-agnostic.

---

## 2026-03-15 — Product Commit (P2-B1 First Cut)

**Status:** DONE  
**Commit:** d978a13  
**Message:** Add reload support and MSBuild typegen integration

**Validation:**
- ✅ Reload Tests: `dotnet test ... --filter ReloadTests` passed
- ✅ Proof Step: `bun run proof:dotnet` passed
- ✅ Product diff clean; no conflicts

**Next Phase:** Proceed with packageability work (NuGet `Varlock.MSBuild`). O'Brien proof lane proceeds in parallel.

**Orchestration Log:** `.squad/orchestration-log/2026-03-15T16:20:33Z-geordi-product-commit.md`

## P3-A1a CI Matrix Decision (2026-03-15)

Defined CI matrix strategy for P3-A1a: expand `.github/workflows/test.yaml` to run `.NET` proof across three OS platforms (Ubuntu, Windows, macOS) using GitHub Actions matrix. Full lint/build/test remain Linux-only; proof runs unconditionally on all platforms.

Build-owned scope is narrow and sound: no package changes, no new APIs. Implementation by O'Brien exposed critical bug (Issue 1: missing `build:libs` on non-Linux runners). Geordi's scope definition remains valid; workflow fix assigned to O'Brien.

**Status:** Awaiting O'Brien's workflow fix. P3-A1a scope unaffected.

---

## P3-A1c: publicOnly Implementation & WinForms net48 Example (2026-03-15T20:50:57Z)

**Session:** P3-A1c closeout consolidation  
**Role:** MSBuild & Typegen Lead

Implemented two critical P3-A1c deliverables:

1. **publicOnly C# Generation:** Filtering + metadata stripping at line 467 in `type-generation.ts`. Contract locked: excludes sensitive items and sensitivity metadata (SensitiveKeys, PropertyBinding, IsSensitive). Error guard for empty-type case. Golden fixture `PublicOnlyConfig.g.cs` anchors regression. 5 new unit tests added (36 total passing).

2. **WinForms net48 Example:** Direct `VarlockCliRuntime` API usage. .NET Framework 4.8 target (Windows-only runtime, cross-platform build). Proof mode via `--dump-config` flag. `dotnet build` succeeds, produces PE32 executable.

Both deliverables meet specification. publicOnly contract is production-ready for Blazor WASM and future use.

**Status:** COMPLETE

---

## P4-A1 Closeout — E2 Evaluation Accepted (2026-03-16)

- 2026-03-16: Picard accepted E2 (Roslyn Source-Generator Evaluation) without revision. DX baseline, three implementation options, and `Varlock.SourceGeneration` thin-wrapper recommendation all present and costed. `dotnet watch` interaction analysis resolves multiple O'Brien DoD gaps (1077–1082) and grounds the recommendation not to implement Roslyn generator in Phase 4. Thin wrapper authorized as P4-B1 item. Decision grounds Roslyn NO-GO + wrapper-authorization verdict.
