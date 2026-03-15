---
updated_at: 2026-03-16T14:00:00.000Z
focus_area: P3-A1c Framework examples
active_issues: []
---

# What We're Focused On

**P3-A1a** ("Cross-platform CI parity") and **P3-A1b** ("Hosting + Worker") are **done**. The hosting package, worker example, `IOptionsSnapshot<T>` scoped-reload proof, and worker reload proofs are all approved-closed with honest ledger entries.

The active slice is **P3-A1c** ("Remaining framework examples"). Scope from the P3-A1 sequencing decision:

1. `examples/dotnet-functions-isolated-net8/` — Azure Functions isolated worker startup, `local.settings.json` coexistence documented
2. `examples/dotnet-blazor-server-net8/` — Blazor Server hosting, server-side config access
3. `examples/dotnet-blazor-wasm-net8-public/` — Blazor WASM public-config-only, proves sensitive values do not cross the public boundary
4. `examples/dotnet-winforms-net48/` — Legacy desktop bridge smoke test (minimum supported legacy target)
5. Proof:dotnet expanded for each example
6. Support-matrix ledger updated: all four rows → proven

Routing:
- **Data** — Azure Functions and Blazor examples (framework integration)
- **Geordi** — WinForms legacy target (build/TFM concerns, `net48` targeting)
- **O'Brien** — Proof harness for all four, ledger updates
- **Tuvok** — Blazor WASM boundary review (security-critical: sensitive value leak prevention)

Prerequisite met: P3-A1b ✅ (hosting package needed for Functions and Blazor Server)

P3-A1d (Serilog + security boundary) is sequenced behind P3-A1c. P4-A1 remains deferred.
