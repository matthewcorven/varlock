---
name: "autonomous-phase-gate-monitoring"
description: "Reusable pattern for turning an approved proposal into an autonomous-ready phase-gate board before issue decomposition"
domain: "phase-gate-monitoring"
confidence: "high"
source: "dotnet-dx-overhaul oversight kickoff"
---

# SKILL: Autonomous phase-gate monitoring before issues exist

Use this pattern when a proposal has been approved and the team wants autonomous agents to start execution, but the work has not yet been decomposed into GitHub issues.

## Pattern

1. **Create stable node IDs from the proposal before spawning work**
   - Name bounded deliverables up front.
   - Keep one stable ID per user-facing promise or proof-bearing slice.

2. **Keep one proof/docs sync lane open for the whole initiative**
   - Do not treat proof accounting as closeout-only work.
   - Any claim that ships without a live proof/docs lane is already drifting.

3. **Require five fields before declaring a node execution-ready**
   - owner
   - reviewer
   - proof artifact or proof command
   - definition of done
   - explicit out-of-scope list

4. **Define first-wave control nodes**
   - Pick the baseline user path, the lowest-friction API surface, and the proof/docs sync lane.
   - If any control node is red, stop adding autonomous fan-out.

5. **Use stale-work thresholds that force escalation**
   - `yellow` when an active node has no meaningful update for 24 hours.
   - `red` when an active or blocked node has no meaningful update for 48 hours.

## Why

Autonomous execution needs a completion contract, not just a backlog. Stable node IDs plus proof-bearing gate rules let the coordinator tell the difference between visible activity and real phase advancement.
