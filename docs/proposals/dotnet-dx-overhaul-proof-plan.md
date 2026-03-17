# .NET DX Overhaul — Evidence Plan

## Purpose

Translate the DX overhaul proposal into proof obligations that can be executed in parallel with implementation instead of being deferred until the end.

## Operating Rule

No overhaul lane is done when code lands alone. A lane is done only when the public claim, proving example, automated check, documentation update, and ledger status all agree.

For autonomous execution, every Wiggum prompt for this overhaul should declare:

- the exact user-facing claim being advanced
- the proving example or fixture that must exist when the lane closes
- the CI command or targeted proof command that must go green
- the docs and ledger files that must be updated in the same lane
- the explicit caveat text that keeps the claim honest if proof is still partial

## Waves

### Wave 0 — Proof Accounting Before Feature Expansion

Allowed public outcome:

- the repo has a stable way to track which overhaul claims are planned, in progress, or proven

Required evidence work:

- treat this plan as the umbrella proof artifact for the overhaul
- add or update `docs/proposals/dotnet-support-ledger.yml` rows whenever a new DX claim appears, even if the row remains `planned`
- make `scripts/test-dotnet-proof.ts` capable of adding new example checks incrementally instead of forcing a one-shot example rewrite
- require every new example directory to include a minimal README that states the exact behavior it proves and the caveat boundary

CI requirement:

- no new example claim merges without being wired into `bun run proof:dotnet` or a documented follow-on proof task created in the same lane

Documentation sync points:

- `docs/proposals/dotnet-dx-overhaul.md`
- `docs/proposals/dotnet-support.md`
- `docs/proposals/dotnet-support-ledger.yml`

### Wave 1 — Baseline and First Feature Slices

Allowed public outcomes:

- a new default `.NET` happy-path example exists and is the documented entry point
- the first console siblings prove a small number of high-value feature stories without widening the supported matrix beyond what CI runs

Required proof artifacts:

- baseline specimen: `examples/dotnet-console/` replacement or renamed successor with a green `dotnet build` and `dotnet run` proof path
- direct-load specimen: proves `Varlock.DotNet` low-level runtime usage without `IConfiguration`
- typed-config specimen: proves generated C# plus normal binder usage in the simplified example shape
- sensitive/logging specimen: proves either manual helper behavior or Serilog behavior, with exact caveats matching existing support boundaries
- reload specimen: proves successful reload, failed reload last-known-good behavior, and no claim beyond the exercised path

CI requirement:

- `bun run proof:dotnet` grows targeted assertions for each first-wave specimen before docs call it a recommended example
- the main workflow continues to prove the current matrix while adding new DX specimens incrementally; no temporary doc-only examples

Documentation sync points:

- getting-started guidance must point only to proven examples
- support-ledger rows for direct load, typed config, logging/redaction, and reload must cite the new specimen paths once they are real

### Wave 2 — API Convenience Claims and Example Expansion

Allowed public outcomes:

- convenience APIs are documented only after a compile-time and runtime specimen exists for each one
- example proliferation stays bounded by proof cost instead of proposal enthusiasm

Required proof artifacts:

- `WebApplicationBuilder.AddVarlock()` compile and runtime proof in the ASP.NET example or a focused sibling
- static `Env.Load()` proof in the direct-load specimen
- DI registration proof for `IVarlockRuntime` and `VarlockResolvedGraph`
- `AddVarlock<TConfig>()` proof through a generated-type or user-authored options example
- metapackage proof via a temporary package-consumer harness, not just monorepo project references
- actionable error-message proof using stable message assertions for the named failure categories

CI requirement:

- targeted build or smoke coverage for any new package or entry point claimed as the default onboarding path

Documentation sync points:

- package READMEs
- distribution guidance
- troubleshooting guidance for the new convenience APIs

### Wave 3 — Framework Simplification and Distribution Story

Allowed public outcomes:

- framework examples are simplified without losing already-proven matrix rows
- distribution and release guidance reflects the new example layout and onboarding flow

Required proof artifacts:

- ASP.NET MVC, Worker, Functions, Blazor Server, WinForms, and WASM examples keep their existing proof rows green after simplification
- framework-specific examples prove only framework-specific behavior; features moved to console siblings keep their own proof rows
- distribution proof covers the recommended executable acquisition story for the new default examples and any metapackage onboarding flow

CI requirement:

- no framework example simplification merges if it removes a previously proven assertion without replacing it elsewhere in the same lane

Documentation sync points:

- framework guides
- migration docs
- release/distribution docs
- support matrix caveats if the teaching layout changes but the supported behavior does not

## First-Wave Proof Work To Start Immediately

1. Refactor `bun run proof:dotnet` so it can register feature specimens individually instead of assuming the current fixed example set is the whole story.
2. Define baseline proof for the new happy-path console example: build, run, repo-local executable discovery, and exact printed values.
3. Stand up the first three high-value siblings in parallel with implementation: direct load, typed config, and reload. Those three cover the most product surface with the least documentation drift.
4. Add a focused logging/security slice in parallel, but keep the claim narrow: manual helper or Serilog destructuring only, whichever is actually exercised.
5. Prepare package-consumer proof scaffolding early for the metapackage and executable acquisition story so release claims do not lag behind API work.

## Support-Claim Guardrails

- Do not describe the new console examples as the recommended path until `bun run proof:dotnet` and CI exercise them.
- Do not widen the supported target-framework story to `.NET 10` just because the overhaul proposal prefers it. That becomes a public claim only after CI and docs explicitly prove the SDK story.
- Do not treat a new sibling example as evidence by itself. A checked-in README and `Program.cs` are not proof until a repeatable command asserts the behavior.
- Do not let reload wording collapse into generic “hot reload works” language. The supported claim must stay tied to the exact provider/watch path and failure behavior that is exercised.
- Do not let sensitive-value or leak-prevention language outrun the current boundary. Manual helper behavior, Serilog destructuring redaction, and metadata-only `PreventLeaks` are distinct claims.
- Do not present the metapackage as the default onboarding story until a consumer outside the monorepo project-reference path proves installation, build, and executable acquisition.
- Do not claim `[VarlockSensitive]` or other emitted metadata unlocks downstream serializer or logging behavior unless a proving example actually consumes it.
- Do not flip MSBuild defaults or auto-enable generation without a watch/build proof slice that shows no pathological rebuild loop.

## Coordinator Checklist For Every Lane

- Update `docs/proposals/dotnet-support-ledger.yml` for any new or narrowed support claim.
- Update the proving example README with the exact behavior and caveat.
- Update `scripts/test-dotnet-proof.ts` and CI wiring, or record the bounded follow-on proof task in the same change.
- Update the relevant user-facing docs only to the level already proven.
- Write a `.squad/decisions/inbox/` note if the lane changes definition of done, caveat text, target framework stance, or distribution expectations.
- Reject any lane that merges a new public claim without a named proof artifact and a named automated check.
