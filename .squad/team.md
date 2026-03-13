# Squad Team

> Varlock's `.NET` initiative strike team — built to ship the v1 CLI bridge, prove support claims with runnable artifacts, and make disciplined native-runtime or analyzer decisions later.

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs, and keeps proof-artifact and reviewer gates intact. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Picard | Initiative Lead | `.squad/agents/picard/charter.md` | ✅ Active |
| Data | Bridge/Hosting Lead | `.squad/agents/data/charter.md` | ✅ Active |
| Geordi | MSBuild & Typegen Lead | `.squad/agents/geordi/charter.md` | ✅ Active |
| Tuvok | Contracts & Security Lead | `.squad/agents/tuvok/charter.md` | ✅ Active |
| O'Brien | Distribution & Proof Lead | `.squad/agents/o'brien/charter.md` | ✅ Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Phase-Gate Monitor | `.squad/agents/ralph/charter.md` | 🔄 Monitor |

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Proof-artifact scaffolding with explicit acceptance criteria
- Example-app fixes or additions that follow the proposal's support matrix
- MSBuild, CI, or test updates that follow an established pattern
- Contract fixture additions, golden-output updates, and docs sync
- Small `.NET` package wiring tasks with bounded scope

**🟡 Needs review — route to @copilot but require a squad-member review:**
- Medium provider or hosting changes with clear proposal anchors
- C# generation or MSBuild refactors backed by tests
- Example-driven platform support work with a defined checklist
- Release and documentation work that depends on existing proof artifacts

**🔴 Not suitable — route to a squad member instead:**
- Architecture or phase-gate decisions
- Native `.NET` runtime or Roslyn analyzer go/no-go calls
- Security, redaction, or plugin-boundary decisions
- New support claims without committed proof artifacts and matrix coverage

## Project Context

- **Owner:** Matthew Corven
- **Project:** varlock
- **Stack:** Bun workspaces, Turborepo, TypeScript, Vitest, GitHub Actions, planned `.NET 8` and `.NET Framework` packages
- **Description:** Varlock adds declarative schema, validation, coercion, and sensitive-value handling to `.env` workflows; this cast is specialized for the `.NET` initiative in `docs/proposals/dotnet-support.md`.
- **Initiative focus:** Proof-artifact-driven `.NET` support from v1 CLI bridge delivery through phase-gated native-runtime and analyzer evaluation
- **Created:** 2026-03-13
