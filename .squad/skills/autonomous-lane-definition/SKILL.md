---
name: "autonomous-lane-definition"
description: "Define Wiggum-safe execution lanes for multi-wave initiative work"
domain: "initiative-decomposition"
confidence: "medium"
source: "2026-03-17 .NET DX overhaul kickoff"
---

# SKILL: Wiggum-safe initiative lane definition

Use this pattern when a large initiative has to be delegated to autonomous executors without letting scope blur across architecture, contracts, proof, and implementation.

## Pattern

1. **Decompose by ownership seam, not by theme**
   - A lane should belong primarily to one lead domain: runtime/hosting, build/typegen, proof/docs, or contract/security.
   - If a lane needs two domains equally, split it.

2. **Keep each lane artifact-shaped**
   - Define the concrete outputs up front: code paths, example directories, docs pages, ledger rows, or fixtures.
   - Avoid vague objectives like "improve DX" or "clean up examples" without naming the files and artifacts that make the lane complete.

3. **Write explicit non-goals**
   - State what the lane may not change: package names, support claims, default behavior, security language, bridge contract, or deferred phase-4 work.
   - Non-goals are what keep the autonomous loop from "helpfully" widening scope.

4. **Attach proof before execution starts**
   - Every lane needs exact validation commands, expected artifact paths, and reviewer gates.
   - Proof should cover both the positive behavior and the boundary being preserved.

5. **Name the reviewer gate**
   - Pick one primary reviewer who decides whether the lane can close.
   - Use specialist reviewers when the lane crosses a sensitive seam:
     - Tuvok for diagnostics, plugin boundaries, security, public/private config
     - O'Brien for examples, CI, docs, support claims
     - Geordi for generated output or build defaults
     - Data for runtime/provider semantics
     - Picard for phase-gate or product-contract changes

6. **Define abort triggers, not just acceptance criteria**
   - A Wiggum lane should stop and escalate when it hits missing human judgment, an upstream contract change, missing proof infrastructure, or a cross-wave dependency that is not yet landed.

## Lane Template

- **Lane name:**
- **Primary owner:**
- **Primary reviewer:**
- **Allowed surface:**
- **Explicit non-goals:**
- **Dependencies:**
- **Required artifacts:**
- **Proof commands:**
- **Acceptance criteria:**
- **Abort triggers:**

## Ready-now test

A lane is ready for autonomous execution only if all of the following are true:

- The dependency list is empty or already proven.
- The lane does not require a new product-contract decision.
- The expected artifacts already have an obvious home in the repo.
- The validation commands already exist or are trivial extensions of existing proof commands.
- The reviewer can approve or reject based on repo evidence alone.

## Why

Autonomous loops perform best when success is concrete and bounded. This pattern keeps initiative work reviewable, retry-safe, and phase-gate honest.
