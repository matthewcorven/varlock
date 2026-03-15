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

## P2-B1 Closure Review (2026-03-15T16:54:02Z)

- 2026-03-15T16:54:02Z: Picard completed P2-B1 phase-gate review. Evidence: real NuGet packageability proven (pack → consume → build → generate → bind chain); post-commit validation green (`dotnet test --filter ReloadTests`, `bun run proof:dotnet`); asset structure canonical (build/ + buildTransitive/ paths); proposal ledger updated. Scope boundaries honest: README documents no bundled executable, separate validation step, or watch behavior — all deferred to P3-A1. Decision: **APPROVE-CLOSE.** P2-B1 complete. Both P2-A1 and P2-B1 closed; P3-A1 (wider platform proof: Windows, macOS, CI parity) now unblocked.
