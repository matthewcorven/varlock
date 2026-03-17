---
name: "dotnet-dx-proof-shadowing"
description: "Reusable pattern for keeping .NET DX overhauls honest by shadowing every new claim with proof, CI, and docs sync from the start"
domain: "dotnet-dx-proof-shadowing"
confidence: "high"
source: "2026-03-17 DX overhaul oversight kickoff"
---

# SKILL: Shadow DX claims before implementation fans out

Use this pattern when a `.NET` overhaul adds many new examples, convenience APIs, or onboarding claims faster than the existing proof matrix can absorb them.

## Pattern

1. **Name the claim first**
   - Write the exact user-facing claim the lane is trying to advance.
   - If the claim is vague enough to sound like marketing, narrow it before work starts.

2. **Create the proof shadow immediately**
   - Add or update the corresponding row in `docs/proposals/dotnet-support-ledger.yml`.
   - Mark it `planned` until the example and automated check both exist.

3. **Pair each example with one automated assertion path**
   - A new example is not proof by itself.
   - Extend `bun run proof:dotnet` or a tightly scoped follow-on proof command in the same lane.

4. **Keep docs pinned to proven behavior**
   - Getting-started pages may link only to proven onboarding paths.
   - README files for examples must state the exact behavior proved and the caveat boundary.

5. **Treat convenience APIs as product claims**
   - `Env.Load()`, `AddVarlock<TConfig>()`, metapackages, new builder extensions, and generated attributes each need compile-time or runtime proof before they become recommended usage.

6. **Refine Wiggum definition of done**
   - Every autonomous task prompt should include: claim, proof artifact, CI command, docs-sync files, ledger row, and caveat text.
   - Reject “implementation complete” outputs that omit any of those items.

## Why

DX work creates documentation pressure early. Shadowing the claim in the ledger and proof harness prevents the repo from teaching end-state intent as if it were already proven behavior.
