# Decision: Phase 4 kickoff — evaluation before implementation

- **Initiative:** dotnet-support
- **Node:** P4-A1
- **Source:** Picard
- **Date:** 2026-03-16

## Decision

Phase 4 is now authorized to begin at node **P4-A1** ("Analyzer/native-runtime decisions"). Phase 4 start is an **evaluation gate**, not an implementation gate.

### What P4-A1 delivers

1. A written CLI bridge limits audit drawn from Phase 1–3 proof artifacts — concrete friction, latency, and capability gaps, not speculation
2. A cost-benefit analysis of native .NET parser/runtime vs. the proven bridge
3. A scoping document for Roslyn analyzer/source-generator enhancements (if justified)
4. A go/no-go recommendation for .NET-native plugin authoring model expansion

### What P4-A1 does NOT authorize

- No product code implementation until evaluation deliverables are accepted by Picard
- No new .NET packages (Varlock.SourceGeneration or otherwise) until the cost-benefit analysis is complete
- No native .NET parser/runtime implementation — only the decision artifact
- No expansion of the v1 support matrix; Phase 3 ledger rows remain the v1 boundary
- No new example applications

### Phase gate alignment

Per the proposal (lines 992–995), Phase 4 exit criteria require that "native evolution work is justified by demonstrated limits of the CLI bridge rather than by speculative parity concerns" and "any expanded plugin or analyzer scope is documented as a new support contract, not assumed retroactively." The evaluation-first shape of P4-A1 directly enforces these criteria.

### Rationale

Phase 3 is committed with full proof coverage across 7 framework examples, Serilog redaction, security boundary, and cross-platform CI. The bridge is mature enough to evaluate honestly. Starting Phase 4 with evidence-gathering rather than implementation prevents premature native-runtime ambition from undermining the stable bridge foundation.
