# .NET DX Overhaul Oversight

> Status: active
> Owner: Ralph, Phase-Gate Monitor
> Source anchors: `docs/proposals/dotnet-dx-overhaul.md`, `docs/proposals/dotnet-support.md`

This is the monitoring model for the `.NET DX overhaul` before GitHub issues exist. It exists to keep autonomous execution honest: every node needs a bounded deliverable, a proof artifact, a reviewer seam, and a stale-work signal.

## Oversight model

### Lanes

| Lane | Purpose | Stable nodes | Primary owner seam | Required reviewer seam |
| --- | --- | --- | --- | --- |
| Teaching surface | Make the happy path and examples obvious, runnable, and narrow | `DX-A1`, `DX-A2a`, `DX-A2b`, `DX-A2c`, `DX-A3`, `DX-A4` | O'Brien for example integrity, with Data or Geordi when behavior requires it | Picard for phase gate; Tuvok when sensitive/public boundary is demonstrated |
| Library surface | Reduce friction in the public `.NET` API and package story | `DX-B1` through `DX-B8` | Data for configuration/runtime seams; Geordi for MSBuild/typegen seams | Picard for public-shape acceptance; Tuvok for diagnostics/sensitivity work |
| Proof and support claims | Keep support claims, READMEs, and proof artifacts synchronized with reality | `DX-X1` | O'Brien | Picard |

### Stable nodes

| Node | Deliverable | Depends on |
| --- | --- | --- |
| `DX-A1` | Replace `examples/dotnet-console/` with the single-file baseline happy-path app and make it the docs anchor | none |
| `DX-A2a` | First sibling batch: direct load, typed config, sensitive, Serilog redaction, reload | `DX-A1` |
| `DX-A2b` | Second sibling batch: custom schema path, custom working dir, environment name, optional, custom runtime | `DX-A1` |
| `DX-A2c` | Third sibling batch: coercion, validation, public-only, exec, composition, DI/options, explicit executable, leak prevention | `DX-A1` |
| `DX-A3` | Simplify framework examples so each only proves framework-specific integration | `DX-A1`, `DX-A2a` |
| `DX-A4` | Add the shared schema-reference cheat sheet | `DX-A1` |
| `DX-B1` | `WebApplicationBuilder.AddVarlock()` entry point | `DX-A1` |
| `DX-B2` | Metapackage for the default install path | `DX-B1` |
| `DX-B3` | Static `Varlock.DotNet.Env.Load()` convenience entry point | none |
| `DX-B4` | DI registration for `IVarlockRuntime` and `VarlockResolvedGraph` | `DX-B3` |
| `DX-B5` | `[VarlockSensitive]` emission on generated properties | current typegen path |
| `DX-B6` | `AddVarlock<TConfig>()` typed binding helper | `DX-B1`, `DX-B4`, `DX-B5` |
| `DX-B7` | Auto-enable or default-on MSBuild generation behavior | `DX-B5` |
| `DX-B8` | Actionable bridge error messages | none |
| `DX-X1` | Proof/docs/ledger sync for every shipped overhaul claim | active from day one; never closes early |

## Minimum board state

The coordinator should track one row per stable node with exactly these fields:

| Field | Why it exists |
| --- | --- |
| `node` | Stable reference for prompts, decisions, and reviewer asks |
| `lane` | Distinguishes teaching-surface work from library-surface work |
| `status` | `not started`, `in progress`, `blocked`, `in review`, `done` |
| `owner` | One accountable implementation owner |
| `reviewer` | One named acceptance owner before work starts |
| `proof artifact` | Example, test command, README, or ledger row that proves the claim |
| `definition of done` | Explicit completion contract suitable for Wiggum execution |
| `blocked by` | Upstream node or missing decision, if any |
| `last meaningful update` | Stale-work detection |
| `next gate` | The next gate this node is trying to satisfy |

If any active node lacks `reviewer`, `proof artifact`, or `definition of done`, it is not execution-ready even if code work has started.

## Reviewer gates

| Gate type | Reviewer required | Applies to |
| --- | --- | --- |
| Phase/gate acceptance | Picard | any node that changes the claimed support boundary or closes a wave |
| Proof/docs/support claim acceptance | O'Brien | any example, README, matrix, or docs-facing claim |
| Sensitive/public boundary acceptance | Tuvok | `DX-A2a`, `DX-A2c`, `DX-B5`, `DX-B8`, and any WASM/public-only or redaction claim |
| Runtime/configuration acceptance | Data | `DX-B1`, `DX-B3`, `DX-B4`, `DX-B6`, `DX-B8`, and behavior-bearing examples |
| MSBuild/typegen acceptance | Geordi | `DX-A2a` typed config, `DX-A2c` public-only, `DX-B5`, `DX-B7` |

## Blocker signals

Raise a blocker immediately when any of the following is true:

- `DX-A1` is not owned or not runnable. The overhaul has no trustworthy happy-path anchor without it.
- Any active node bundles more than one user-facing promise. That is too large for autonomous execution and usually hides failure.
- A node changes a public API, generated output shape, sensitive boundary, or install story without a named reviewer.
- An example node has no committed `.env.schema`, safe example values, or README explaining what it proves.
- `DX-X1` lags behind shipped behavior. Claims without proof accounting are red until reconciled.
- A Wiggum-targeted node has no explicit proof command or no out-of-scope list.
- A node is blocked by another node that is still `not started`; that is sequencing drift, not active progress.

## Stale-work signals

- `yellow`: no meaningful board update for 24 hours on an `in progress` node
- `yellow`: implementation is moving but proof artifact or reviewer assignment is still missing after the first code change
- `red`: no meaningful board update for 48 hours on an `in progress` or `blocked` node
- `red`: code is merged or ready to merge while `DX-X1` still says the claim is unproven or undocumented
- `red`: first-wave nodes are open but nobody owns the baseline docs/demo path (`DX-A1`) or the proof/docs sync lane (`DX-X1`)

## Autonomous phase-gate checklist

### Gate 0 — Execution framing ready

- The board contains `DX-A1` through `DX-B8` plus `DX-X1`.
- Every first-wave node has a bounded definition of done, proof artifact, reviewer, and explicit out-of-scope list.
- The coordinator has split work so one autonomous run maps to one stable node or a clearly bounded slice of one node.
- Dependencies are recorded honestly; no node is marked executable while still waiting on an upstream design call.

### Gate 1 — First-wave execution credible

- `DX-A1` is in progress or done and has a runnable baseline proof path.
- At least one teaching-surface node and one library-surface node are independently executable without blocking each other.
- `DX-X1` is open and tracking which claims are still planned versus proven.
- Every first-wave node can be verified by a command, a checked example, or a generated artifact check instead of reviewer memory.

### Gate 2 — Track A teaching surface credible

- `DX-A1`, `DX-A2a`, `DX-A2b`, `DX-A2c`, `DX-A3`, and `DX-A4` are done or explicitly deferred.
- Each example says one thing clearly and does not depend on unrelated framework boilerplate to explain itself.
- Framework examples only prove framework-specific integration and do not duplicate sibling-console feature demos.
- README and schema artifacts match what the examples actually prove.

### Gate 3 — Track B library surface credible

- `DX-B1` through `DX-B8` are done or explicitly deferred with rationale.
- Public API additions have named owner and reviewer sign-off.
- Typegen and MSBuild behavior changes include proof of output location, determinism, and default install ergonomics where claimed.
- Diagnostics and sensitive-boundary changes preserve actionable messages without widening unsupported claims.

### Gate 4 — Overhaul closeout credible

- `DX-X1` shows every shipped overhaul claim as proven or intentionally deferred.
- The default install/use story is coherent across examples, APIs, docs, and package names.
- No active node is depending on tribal knowledge instead of an artifact or a board row.
- Picard can say what shipped, what deferred, and what proof backs each claim without reconstructing history from commits.

## Green / yellow / red for first-wave execution

### Green

- `DX-A1`, `DX-B1` or `DX-B3`, and `DX-X1` all have owners, reviewers, proof artifacts, and bounded definitions of done.
- At least one Track A slice and one Track B slice can run in parallel.
- No blocker is older than 24 hours.

### Yellow

- Work has started, but one of the first-wave nodes is missing reviewer assignment, proof mapping, or an explicit out-of-scope list.
- A node is large enough that it really contains two user-facing promises.
- Progress exists, but the proof/docs sync lane is lagging behind implementation.

### Red

- `DX-A1` is stalled, undefined, or unowned.
- Public API or sensitive-boundary work is moving without the correct reviewer seam.
- Claimed progress depends on “we’ll sort out docs/tests later.”
- Any first-wave node has been stale for 48 hours or is blocked by a node nobody has started.

## Immediate coordinator actions

1. Stand up the board with the stable node IDs in this document before opening implementation issues.
2. Treat `DX-A1`, `DX-B1`, `DX-B3`, and `DX-X1` as the first-wave control set; if any of those are red, stop spawning more work.
3. Write Wiggum-ready definitions of done for every first-wave node: code surface, proof command, artifact path, reviewer, and out-of-scope list.
4. Keep proof/docs sync (`DX-X1`) open from day one; do not wait until the end of the overhaul to reconcile claims.
5. Escalate to Picard immediately if a node needs a support-boundary or sequencing decision; escalate to O'Brien when proof artifacts or README surfaces are missing.
