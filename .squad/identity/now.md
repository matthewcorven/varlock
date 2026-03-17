updated_at: 2026-03-17T10:49:46Z
focus_area: .NET DX overhaul kickoff with first-wave control-set governance and Wiggum-first execution
active_issues:
	- Control-set readiness signal: `DX-A1`, `DX-B3`, and `DX-X1` are green; `DX-B1` stays yellow until the baseline docs/proof anchor is stable
	- Every autonomous lane needs explicit definition of done, proof mapping, reviewer, and out-of-scope boundaries before execution
---

# What We're Focused On

**The current focus is the `.NET` DX overhaul kickoff.** The proposal in `docs/proposals/dotnet-dx-overhaul.md` is now the planning baseline, but it remains subordinate to existing `dotnet-support` v1 boundaries and established NO-GO decisions.

## Current Position

- Kickoff is authorized from commit `8a438ed`
- Wave 0 decomposition, oversight, and proof-governance seams are recorded
- Wiggum-first execution is the preferred mode, but only for lanes that are fully specified and reviewer-safe
- The first-wave control set is the gating mechanism for further autonomous fan-out

## First-Wave Control Set

- `DX-A1` — baseline example lane
- `DX-B1` — `WebApplicationBuilder` entry point lane
- `DX-B3` — static `Env.Load()` lane
- `DX-X1` — proof/docs/ledger sync lane

Current signal: `DX-A1`, `DX-B3`, and `DX-X1` are green. `DX-B1` remains yellow until `DX-A1` stabilizes the baseline docs/proof anchor.

If any control-set node turns red, the coordinator should slow or stop additional autonomous fan-out until the set returns to green.

## Execution Rules In Force

- Prefer Wiggum-driven execution for overhaul work
- No lane is ready until it has an owner, reviewer, proof artifact or proof command, bounded definition of done, and explicit out-of-scope list
- No lane is done until code, proof coverage, docs, and ledger state all match
- New examples remain documentation assets only until `bun run proof:dotnet` exercises them
- `.NET 10`, metapackage, executable-distribution, reload, security, and logging claims must stay pinned to exact proven boundaries

## Scope Boundaries Maintained

- ✓ Thin source-generation wrapper (no Roslyn, no analyzers)
- ✓ Build-backed type generation with external CLI
- ✓ Documentation coherence (Wave 1 + Wave 2 + bridge-limits closeout)
- ✓ Package README clarity
- ✓ DX overhaul kickoff stays subordinate to current v1 support boundaries
- ✗ Native .NET runtime — **NO-GO**
- ✗ Full Roslyn source-generator — **NO-GO**
- ✗ .NET-native plugin expansion — **NO-GO**
- ✗ Support-contract widening without proof/ledger alignment — blocked
- ✗ Default flips or package-surface expansion without explicit approval — blocked
