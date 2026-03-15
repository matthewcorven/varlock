---
updated_at: 2026-03-16T12:00:00.000Z
focus_area: P3-A1b Hosting + Worker example
active_issues: []
---

# What We're Focused On

**P3-A1a** ("Cross-platform CI parity") is **done**. CI now runs on Ubuntu, Windows, and macOS. The .NET runtime handles Windows `.js` execution via `node` prefix, proof harnesses align with `.cmd` preference, and all validation passes cross-platform. Approved-closed by Picard.

The active slice is **P3-A1b** ("Hosting package + Worker Service example"). Scope from the P3-A1 sequencing decision:

1. `packages/dotnet/Varlock.Extensions.Hosting` — `AddVarlock()` on `IHostBuilder` / `IHostApplicationBuilder`, clean DI registration
2. `examples/dotnet-worker-net8/` — Worker Service using Generic Host with `IOptionsMonitor<T>` reload proof
3. `IOptionsSnapshot<T>` scoped-reload proof in the existing ASP.NET example (deferred from P2)
4. Proof:dotnet expanded for worker example
5. Support-matrix ledger updated: Worker Service row → proven

Routing:
- **Data** — `Varlock.Extensions.Hosting` implementation, worker example
- **O'Brien** — Proof harness expansion, ledger updates
- **Tuvok** — Contract analysis before implementation (hosting package touches public API surface)

Prerequisite met: P3-A1a green on all platforms.

P3-A1c (framework examples) and P3-A1d (Serilog + security boundary) are sequenced behind P3-A1b. P4-A1 remains deferred.
