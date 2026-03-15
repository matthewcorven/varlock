# SKILL: Contract Consistency Analysis for Feature Implementation

**Version:** 1.0  
**Author:** Tuvok (Contracts & Security Lead)  
**Context:** Varlock .NET support, machine-readable bridge contracts  
**Last Updated:** 2026-03-15

## Purpose

Before an autonomous agent (Ralph) implements a feature that extends or interacts with existing public APIs, bridge contracts, or proof harnesses, perform a read-only contract-consistency pass to surface:

1. Existing public API names/signatures that must remain stable
2. Existing proof-harness or example output shapes that should not silently change
3. Existing bridge error/diagnostic categories or failure behavior that the new work must respect
4. Risky contract edges where the new feature could violate invariants (atomicity, last-known-good, etc.)
5. A machine-readable "DO NOT BREAK" block the coordinator can paste directly into the agent's prompt

## When to Use

- Before an implementation task that touches: public configuration APIs, error-handling paths, bridge contracts, proof fixtures, or example output shapes
- When scope boundaries are tight and existing contracts must be preserved to avoid downstream breakage
- When the feature involves state management (e.g., caching, reload, persistence) that could violate atomicity or consistency guarantees

## How to Perform

### 1. Read scope and intent

Start by understanding what the new feature is supposed to do, *not* what it is, from:
- Initiative/node-level scope decision (e.g., Picard's P2-A1 scope boundary)
- Acceptance checklist or deliverables (e.g., Ralph's acceptance criteria)
- Existing proposal or design document sections related to the feature

### 2. Map the public API surface

For each package/namespace that will be touched:
- List all existing public types (classes, interfaces, enums)
- List all existing public members (properties, methods, events)
- Mark which are read-only (e.g., properties that should not be modified)
- Mark which are entry points (e.g., extension methods, constructor overloads)
- Document default values and null semantics

**Example from P2-A1:**
- `VarlockConfigurationSource.SchemaPath { get; set; }` (default `.env.schema`)
- `VarlockConfigurationSource.Optional { get; set; }` (boolean flag)
- `VarlockConfigurationBuilderExtensions.AddVarlock(IConfigurationBuilder)` (entry point)

### 3. Identify what MUST be new

From the acceptance checklist or scope decision, identify what the agent must add:
- New public properties or methods
- New enum values or types
- New internal state or behavior

Keep this list tight. Scope creep here breaks the contract.

**Example from P2-A1:**
- NEW: `ReloadOnChange { get; set; }` property
- NEW: `ReloadFailureBehavior { get; set; }` property
- NEW: `VarlockReloadFailureBehavior` enum
- (NOT new: extension methods, hosting package, or new error categories)

### 4. Trace existing bridge/fixture contracts

For each public API or protocol, identify the "last-mile" data shape that consumers see:

- **Bridge contracts:** stdout/stderr envelope format, error category mapping, exit codes
- **Configuration fixtures:** JSON payloads, property names, types
- **Proof-harness shapes:** test payload structures, assertion values
- **Example output:** observable behavior from running code

**Example from P2-A1:**
- Bridge envelope: `{ contractVersion, cliVersion, ok, [graph | category, message, ...] }`
- Error categories: 7 existing (`SchemaMissing`, `SchemaInvalid`, `ResolutionFailed`, `PluginLoadFailed`, `ExecutableVersionMismatch`, `ExecutableNotFound`, `BridgeInternalError`)
- Success fixture: FOO → "bar", isSensitive → true, sources → [directory, .env.schema]
- Console proof payload: appName, httpPort (coerced), featureEnabled (coerced), secretIsSensitive, redactLogs, preventLeaks, sourceLabels

### 5. Find test and fixture files

Locate the test classes, test fixtures, and proof scripts that validate these contracts:
- Unit/integration test files (e.g., `BridgeContractAlignmentTests.cs`)
- Fixture files (e.g., `success.json`, `schema-missing.json`)
- Proof harness scripts (e.g., `scripts/test-dotnet-proof.ts`)
- Example applications (e.g., `examples/dotnet-aspnet-mvc-net8/`)

**Why:** These tests are the *executable specification* of the contract. They must pass unchanged (or only be extended, not modified).

### 6. Identify risky edges

Look for scenarios where the new feature could violate invariants:

- **Atomicity:** Does the feature atomically replace state, or could consumers see partial updates?
- **Last-known-good:** Can the feature degrade to a safe fallback, or does failure poison state?
- **Change notifications:** Does the feature fire notifications on partial updates, failures, or only after success?
- **Backward compatibility:** Does the feature require new configuration that older consumers don't provide?
- **Optional/fallback semantics:** When the primary path fails, does the feature gracefully degrade?

**Example from P2-A1:**
- **Atomic swap edge:** Reload must replace `Data` in a single assignment before firing change token
- **Last-known-good edge:** Failed reload must not mutate existing configuration
- **Token edge:** Change token must fire only after successful reload, never after failure
- **Watch-set edge:** Recompute watched files only after success, not after failed attempts
- **Optional edge:** `Optional: true` + `ReloadOnChange: true` must watch for schema arrival, not assume it exists at startup

### 7. Write the analysis

Document in this order:

1. **Executive Summary:** What the agent must preserve in 2-3 sentences
2. **Public API Stability:** Existing names/signatures (MUST REMAIN) vs. new additions (agent may add)
3. **Proof-Harness Shapes:** Bridge envelope, error categories, fixture data, test assertions
4. **Failure Behavior & Diagnostics:** Error categories, failure paths, diagnostics surface
5. **Risky Edges:** Atomicity, last-known-good, token semantics, optional/fallback behavior
6. **DO NOT BREAK:** Structured, copy-paste-ready constraints for the agent

### 8. Create the outputs

- **Decision record** (detailed, narrative form): `.squad/decisions/inbox/tuvok-{slug}.md`
- **DO NOT BREAK block** (compact, copy-paste-ready): `.squad/decisions/inbox/tuvok-{slug}-ralph-do-not-break.txt` (or inline in decision)
- **History entry** (lessons learned): append to `.squad/agents/tuvok/history.md`

## Pattern Example

```markdown
# P2-A1 Contract Consistency Analysis – Tuvok

## Executive Summary
Ralph must add reload mechanics to VarlockConfigurationProvider while preserving:
1. Existing public API on VarlockConfigurationSource (8 properties + Build method)
2. Bridge envelope parsing from varlock CLI (success + 7 error categories)
3. Optional-schema startup semantics + last-known-good preservation
4. Atomic configuration swap + change-token firing only after success
5. Proof-harness observable shapes (console and ASP.NET example payloads)

## 1. PUBLIC API NAMES & SIGNATURES TO PRESERVE

### VarlockConfigurationSource
**MUST REMAIN:**
- Property SchemaPath { get; set; }
- Property Optional { get; set; }
- ...

**NEW PUBLIC SURFACE:**
- Property ReloadOnChange { get; set; }
- Property ReloadFailureBehavior { get; set; }
- Enum VarlockReloadFailureBehavior { KeepLastKnownGood }

## 2. PROOF-HARNESS OUTPUT SHAPES...
## 3. BRIDGE ERROR/DIAGNOSTIC CATEGORIES...
## 4. RISKY CONTRACT EDGES...
## 5. DO NOT BREAK...
```

## Lessons Learned (P2-A1 Application)

1. **Bridge contracts are load-bearing:** The 7 error categories and envelope shape must be honored in reload because consumers depend on deterministic error mapping. Adding new categories or changing envelope structure breaks downstream diagnostics.

2. **Last-known-good is non-negotiable:** Reload failures must not mutate active configuration. This is the single most important invariant. If a reload attempt fails, the provider must behave as if the load was never attempted.

3. **Change-token semantics are observable:** Tests use `IOptionsMonitor<T>.OnChange` callbacks to verify reload worked. If you fire tokens on failed reloads or partial updates, tests will break. Fire tokens only after atomic swap completes.

4. **Watch-set recomputation depends on success:** Only recompute watched files from the newly-loaded graph. If you recompute after failed attempts, you risk watching stale files or importing invalid sources.

5. **Optional-schema behavior extends to reload:** `Optional: true` means "allow missing schema at startup." If `ReloadOnChange: true` is also set, the provider must *continue* to watch and activate when the schema appears later. This is not a startup-only guarantee.

6. **Proof harnesses are executable specs:** If the console example payload shape changes, the test script breaks. If bridge error mapping changes, the fixture-based assertions fail. These are your guardrails. Test them first before implementation.

## Lessons Learned (P3-A1 Application)

7. **Generation-time security boundaries are harder than runtime boundaries:** For Blazor WASM, sensitivity must be enforced at code-generation time, not runtime. The generated file IS the public surface. If `SensitiveKeys[]` metadata appears in a WASM bundle, it is a leak regardless of runtime checks. Always ask: "Where does this generated artifact end up?" before approving a generation contract.

8. **Metadata flags without consumers are overclaim risks:** `RedactLogs` and `PreventLeaks` exist as parsed bridge properties in .NET, but nothing reads them. When documenting, say "bridge metadata for consumer use" not "protection feature." A flag that nobody acts on is information, not enforcement.

9. **New package API surfaces need contract analysis BEFORE implementation:** `Varlock.Serilog` must have its public API shape defined and reviewed before Data writes code. The API name (`WithVarlockRedaction`) implies a guarantee; the contract analysis determines what that guarantee actually is (property-match destructuring, not substring scanning).

10. **Positive-path tests are as important as failure-path fixtures:** The bridge has thorough failure fixtures (all 7 categories) but plugin-backed success paths have no .NET fixture. A support claim needs both a failure test and a success test. "It doesn't crash on failure" is not the same as "it works."

11. **Golden-file fixtures are the strongest contract guardrails for generation boundaries:** Inline assertions (`toContain`/`not.toContain`) prove individual properties. Golden-file comparison (`toBe(readFixture(...))`) proves the *entire output shape* and makes regressions visible in diffs. For security-critical generation boundaries like `publicOnly`, always add a golden-file fixture alongside inline assertions. The `PublicOnlyConfig.g.cs` fixture proves the complete public-only output including what is absent.

12. **"Proof does not need binary inspection" is a scoping decision, not a concession:** When the generated source file is the only controlled artifact entering the target bundle, validating the source is sufficient. Binary inspection (e.g., scanning WASM assemblies for sensitive strings) adds test complexity without improving the guarantee — the compiler does not invent new configuration values. State this explicitly in proof constraints to prevent overclaiming in the harness.

## Related Decisions

- **P1-A1 executable lookup and handshake hardening** — defines executable resolution order and bridge handshake semantics that reload reuses
- **P1-A1 lookup proof harnesses** — defines proof-script approach; reload proof extends, does not replace
- **First CLI bridge contract slice** — defines 7 error categories and envelope shape; reload must honor these
- **P3-A1 boundary gap analysis** — defines Blazor WASM public-config boundary contract, Serilog API contract, and overclaim audit findings

## Coordinator Notes

When Ralph begins the reload implementation, paste the "DO NOT BREAK" block (item 5) directly into their prompt. If Ralph encounters a scenario that seems to violate one of these boundaries, escalate to Picard/Coordinator before proceeding. Do not allow Ralph to "work around" these constraints; they are test-backed and load-bearing.

