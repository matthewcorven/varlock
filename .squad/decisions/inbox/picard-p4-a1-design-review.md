# Decision: P4-A1 Design Review — evaluation batch shape and agent assignments

- **Initiative:** dotnet-support
- **Node:** P4-A1
- **Source:** Picard
- **Date:** 2026-03-16
- **Ceremony:** Design Review

## Verdict: PROCEED

Phase 4 opens as a pure evaluation batch. No product code is authorized until all four evaluation deliverables are accepted and Picard issues a go/no-go on each evolution axis.

## Context

The CLI bridge is feature-complete for its design scope: contract v1 with handshake, 4-tier executable resolution, FileSystemWatcher reload with 300ms debounce and last-known-good preservation, Serilog destructuring redaction, publicOnly type generation for WASM boundary, 7 error categories with file/line/column location, and cross-platform CI proof across 7 framework examples. Zero TODO/FIXME/phase-4 markers exist in the .NET packages.

The proposal (lines 986–995) requires that Phase 4 work be "justified by demonstrated limits of the CLI bridge rather than by speculative parity concerns" and that "any expanded plugin or analyzer scope is documented as a new support contract, not assumed retroactively."

The Definition of Done (line 1020) also requires that `Varlock.SourceGeneration` exists "in at least the initial CLI-generated form, with a clear evolution path to richer analyzer/source-generator support." This is a DoD item that Phase 4 evaluation must address — either by recommending the package be created (wrapping the existing CLI-generated flow) or by documenting why the MSBuild integration already satisfies the intent.

## P4-A1 Evaluation Deliverables

### E1: CLI Bridge Limits Audit — Data

**Deliverable:** A written document at `docs/proposals/dotnet-phase4-bridge-limits.md` containing:

1. **Measured cold-start latency** — time from `Process.Start()` to parsed `VarlockResolvedGraph` across representative schema sizes (small/medium), on Linux and Windows CI runners
2. **Measured reload latency** — same measurement for the reload path (file change → debounce → re-invoke → graph swap)
3. **Capability gap inventory** — concrete things the bridge architecture cannot do that a native .NET parser could:
   - Incremental/partial reload (bridge always re-resolves full graph)
   - In-process schema introspection without CLI round-trip
   - Node.js runtime dependency on the host
   - Process-spawn overhead for high-frequency reload scenarios
4. **Materiality assessment** — for each gap, state whether it causes real user friction in the proven example flows or is theoretical

**Evidence standard:** Latency numbers must come from actual runs against the existing proof examples, not estimates. Capability gaps must reference the specific code paths that impose the limitation.

### E2: Roslyn Source-Generator Evaluation — Geordi

**Deliverable:** A written document at `docs/proposals/dotnet-phase4-roslyn-evaluation.md` containing:

1. **Current DX baseline** — what a .NET developer experiences today with CLI-generated `.g.cs` via MSBuild targets (build-time only, no IDE real-time feedback, incremental-build behavior)
2. **Roslyn incremental source-generator DX** — what would change if `Varlock.SourceGeneration` were a Roslyn `IIncrementalGenerator`: IDE real-time type preview, build-without-CLI for type stubs, analyzer diagnostics for schema problems
3. **Implementation cost** — what the source generator would need to read (schema files, not resolved env values), what it would generate (same POCO + PropertyKeys + PropertyBinding as today), and what new dependencies it introduces
4. **`Varlock.SourceGeneration` package recommendation** — should the package exist as (a) a thin wrapper around the CLI-generated flow (satisfying DoD line 1020 minimally), (b) a Roslyn incremental source generator, or (c) deferred with documented rationale
5. **`dotnet watch` interaction** — document whether MSBuild regen and provider reload interact cleanly or create pathological loops under `dotnet watch`

**Constraint:** This evaluation must not produce a working Roslyn source generator. Only the decision artifact.

### E3: Contract & Security Boundary Evolution — Tuvok

**Deliverable:** A written document at `docs/proposals/dotnet-phase4-contract-evolution.md` containing:

1. **Contract stability assessment** — is bridge-contract v1 sufficient for the foreseeable .NET surface, or are there scenarios requiring v2 (e.g., incremental reload, schema introspection, plugin metadata)?
2. **Security boundary completeness** — what security behaviors from the JS runtime (HTTP leak prevention, process-output redaction, `preventLeaks` enforcement) remain architecturally impossible through the bridge, and which could be approximated with .NET-side middleware or interceptors?
3. **Plugin contract evolution** — if .NET-native plugin authoring were added, what contract changes would be needed? What is the minimum viable plugin surface vs. full CLI-engine parity?
4. **Recommendation** — for each axis (contract version, security boundary, plugin model), state whether evolution is justified now, justified later, or not justified

### E4: Definition-of-Done Gap Analysis — O'Brien

**Deliverable:** A written document at `docs/proposals/dotnet-phase4-dod-gap-analysis.md` containing:

1. **DoD section-by-section audit** — for each of the 16 DoD sections (lines 1001–1209), mark every bullet as:
   - ✅ **Complete** — proven by existing code, tests, or proof artifacts
   - 📝 **Documentation-only** — implementation exists but docs are not yet written
   - 🔍 **Depends on P4-A1 evaluation** — requires E1/E2/E3 outcomes before status can be determined
   - ⏳ **Deferred beyond v1** — explicitly out of scope per proposal or team decisions
2. **Critical path identification** — which DoD items block "first-class .NET support" claim and are not yet complete?
3. **Documentation work estimate** — rough scope of the documentation-only gaps (this informs whether a P4-B1 documentation batch is needed)

**Constraint:** This is an audit, not a fix. O'Brien does not close any gaps — only catalogs them.

## Hard Dependencies

```
E1 (Data) ──────────┐
                     ├── Picard go/no-go on native runtime
E3 (Tuvok) ─────────┘
E2 (Geordi) ─────────── Picard go/no-go on Roslyn source generator
E4 (O'Brien) ────────── Picard go/no-go on v1 completeness scope
```

- **E1 and E3 must both be accepted** before native-runtime go/no-go
- **E2 is independent** — can proceed in parallel
- **E4 is independent** — can proceed in parallel
- All four deliverables must be accepted before P4-A1 closeout

Within the batch: **all four agents can work in parallel.** No serialization required.

## Agent Assignments

| Agent | Deliverable | Output path | Estimated scope |
|-------|------------|-------------|-----------------|
| Data | E1: Bridge limits audit | `docs/proposals/dotnet-phase4-bridge-limits.md` | Measurement + analysis |
| Geordi | E2: Roslyn evaluation | `docs/proposals/dotnet-phase4-roslyn-evaluation.md` | Research + recommendation |
| Tuvok | E3: Contract evolution | `docs/proposals/dotnet-phase4-contract-evolution.md` | Contract analysis |
| O'Brien | E4: DoD gap analysis | `docs/proposals/dotnet-phase4-dod-gap-analysis.md` | Section-by-section audit |

## Reviewer Gates & Lockout Rules

1. **Picard reviews all four deliverables.** No deliverable is accepted without lead review.
2. **Reviewer lockout applies on rejection:** if an evaluation artifact is rejected, a different agent revises it:
   - E1 rejected → Tuvok revises (contract perspective on bridge limits)
   - E2 rejected → Data revises (runtime perspective on source generation)
   - E3 rejected → Geordi revises (build-system perspective on contracts)
   - E4 rejected → Data revises (implementation perspective on DoD gaps)
3. **No partial acceptance.** Each deliverable is accepted or rejected as a unit.
4. **Evidence standard:** Latency claims require measured numbers. Capability claims require code-path references. Recommendations require explicit rationale tied to proposal exit criteria.

## Acceptance Criteria for P4-A1 Closeout

Picard will issue APPROVE-CLOSE for P4-A1 when all of the following are true:

1. E1 exists with measured latency data and a materiality assessment for each bridge gap
2. E2 exists with a concrete `Varlock.SourceGeneration` recommendation (create, defer, or evolve)
3. E3 exists with go/no-go recommendations for contract v2, security boundary expansion, and plugin model
4. E4 exists with every DoD bullet categorized and a critical-path identification
5. A documented **native-runtime go/no-go** decision exists, grounded in E1 + E3 evidence
6. A documented **Roslyn source-generator go/no-go** decision exists, grounded in E2 evidence
7. A documented **.NET-native plugin expansion go/no-go** decision exists, grounded in E3 evidence
8. The DoD gap analysis (E4) identifies what remains for v1 completion vs. what is deferred beyond v1
9. **Zero product code has been implemented** — only evaluation and decision artifacts
10. All four deliverables have passed Picard's lead review

## Scope That Remains Deferred Even Within P4-A1

- No native .NET parser/runtime implementation
- No Roslyn source-generator implementation
- No new .NET packages
- No new example applications
- No v1 support-matrix expansion beyond Phase 3 ledger rows
- No `varlock run` or `varlock scan` .NET wrappers
- No HTTP leak prevention or non-Serilog global redaction implementation
- Documentation authoring (likely P4-B1) — E4 sizes it but does not execute it
