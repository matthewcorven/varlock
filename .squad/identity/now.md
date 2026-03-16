---
updated_at: 2026-03-16T00:00:00.000Z
focus_area: Phase 4 kickoff preparation
active_issues: []
---

# What We're Focused On

**Phase 3 is committed and closed.** All sub-batches (P3-A1a through P3-A1d) are approved-closed with proof artifacts, and the Phase 3 commit is on `main`.

**Phase 4 is now authorized for kickoff.** The next active node is **P4-A1** ("Analyzer/native-runtime decisions").

## What Phase 4 start means

Phase 4 is an **evaluation and decision phase**, not an implementation phase. The first work is:

1. **Audit CLI bridge limits** — document concrete friction points, latency characteristics, and capability gaps that the CLI bridge imposes on the .NET developer experience, drawn from Phase 1–3 proof artifacts and example usage
2. **Cost-benefit analysis** — evaluate native .NET parser/runtime against the proven bridge, using measured evidence rather than speculative parity concerns
3. **Analyzer/source-generator scoping** — determine whether Roslyn analyzer or source-generator enhancements are justified, and if so, define their support contract boundaries
4. **Plugin expansion assessment** — evaluate .NET-native plugin authoring model feasibility

## Explicitly deferred even within Phase 4 start

- No product code implementation until the evaluation deliverables are accepted
- No `Varlock.SourceGeneration` Roslyn-native package until cost-benefit analysis is complete
- No native .NET parser/runtime implementation — only the decision artifact
- No new .NET packages beyond what Phase 3 shipped
- No expansion of the v1 support matrix — Phase 3 ledger rows remain the v1 boundary

## Phase 3 accomplishments (for reference)

1. Cross-platform CI parity for the .NET proof slice
2. Hosted and worker .NET configuration-provider flows
3. Azure Functions isolated, Blazor Server, Blazor WASM public-only generation boundary, WinForms net48
4. `Varlock.Serilog` with Serilog-specific destructuring redaction and metadata enrichment
5. `VarlockRedactionHelper.Redact(...)` for non-Serilog paths
6. Explicit security boundary naming what is automatic, manual, and unsupported in v1
