---
updated_at: 2026-03-15T19:19:26.522Z
focus_area: P3-A1d Security boundary
active_issues: []
---

# What We're Focused On

**P3-A1a** ("Cross-platform CI parity"), **P3-A1b** ("Hosting + Worker"), and **P3-A1c** ("Remaining framework examples") are **done**. P3-A1c is approved-closed with honest proof: Azure Functions isolated, Blazor Server, Blazor WASM public-only generation boundary, and WinForms `net48` are all implemented and proven in the support ledger.

The active slice is now **P3-A1d** ("Security Boundary + Ledger Completion"). Scope from the P3-A1 sequencing decision:

1. `packages/dotnet/Varlock.Serilog/` — Serilog-specific redaction helpers targeting `netstandard2.0`
2. Security-boundary specimen — Serilog redaction example, non-Serilog fallback helpers, and the already-proven Blazor WASM public-only boundary
3. Non-Serilog fallback redaction helper proof in the console example
4. Final support-matrix completion for the remaining planned security/logging rows
5. Proposal updates documenting Phase 3 exit criteria as met

Routing:
- **Tuvok** — Serilog contract, security-boundary wording, and overclaim prevention
- **Data** — `Varlock.Serilog` implementation and any runtime/helper seams
- **O'Brien** — proof harness, examples, docs, and final ledger completion
- **Picard** — final acceptance and phase-exit review

Prerequisite met: P3-A1c ✅

P4-A1 remains deferred.
