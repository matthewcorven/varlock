# Work Routing

How to decide who handles work for the Varlock `.NET` initiative.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, phase gates, support claims, native-runtime or analyzer decisions | Picard | v1 vs phase-4 cuts, package boundaries, support-matrix acceptance, long-term evolution calls |
| CLI bridge, configuration provider, hosting, options, reload behavior | Data | `AddVarlock()`, `IConfiguration`, `IOptions<T>`, `reloadOnChange`, `dotnet watch`, User Secrets coexistence |
| MSBuild, C# type generation, IDE build integration, analyzer exploration | Geordi | `@generateTypes(lang=cs)`, `.g.cs` output, incremental builds, Roslyn follow-on work, build-loop hygiene |
| Machine-readable contracts, diagnostics, redaction, plugin boundaries, public/private config boundaries | Tuvok | bridge fixtures, error categories, version handshake, Serilog redaction, plugin load failures, Blazor WASM public-only rules |
| Executable distribution, examples, support matrix, CI/release, docs | O'Brien | acquisition/versioning, runnable examples, GitHub Actions, release packaging, migration docs, ledger maintenance |
| Phase-gate tracking and proof-artifact completeness | Ralph | blocked exits, missing artifacts, stale dependencies, readiness nudges |
| Code review and final acceptance | Picard | cross-cutting review, routing disputes, phase-exit sign-off |
| Testing and support-claim validation | O'Brien | example smoke tests, matrix-ledger coverage, cross-platform CI proof |
| Security review | Tuvok | sensitive-value handling, diagnostic exposure, plugin/discovery risk |
| Scope & priorities | Picard | what ships in v1, what waits for later phases, trade-off calls |
| Async issue work (bugs, tests, small features) | @copilot 🤖 | bounded issues with clear acceptance criteria and an existing pattern |
| Session logging | Scribe | automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage against the proposal, evaluate @copilot fit, assign `squad:{member}` label(s) | Picard |
| `squad:{name}` | Pick up the issue and complete the work | Named member |
| `squad:copilot` | Assign to @copilot for autonomous work (if enabled) | @copilot 🤖 |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, **Picard** triages it against `docs/proposals/dotnet-support.md`, the phase plan, and the proof-artifact requirements.
2. **@copilot evaluation:** Picard only routes to `squad:copilot` when the issue is bounded, pattern-following, non-security-sensitive, and does not establish a new first-class support claim.
3. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
4. When `squad:copilot` is applied and auto-assign is enabled, `@copilot` is assigned on the issue and picks it up autonomously.
5. Members can reassign by removing their label and adding another member's label when the real ownership seam is elsewhere.
6. The `squad` label is the inbox for untriaged `.NET` initiative work.

### Lead Triage Guidance for @copilot

When triaging, Picard should ask:

1. **Does the issue have explicit acceptance criteria and a proposal anchor?** Clear artifact, example, or package scope → likely 🟢
2. **Does it follow an existing proof-artifact, example-app, or package pattern?** Pattern-following implementation → likely 🟢
3. **Does it change support boundaries, phase gates, or package architecture?** Design judgment → 🔴
4. **Does it touch diagnostics, redaction, plugin boundaries, or public/private config separation?** Security or contract risk → 🔴 or 🟡 with Tuvok review
5. **Does it create a new support claim in docs or the ledger?** Likely 🟡 unless proof artifacts are already defined and reviewers are assigned

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream proof and review work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for straightforward factual questions.
4. **Route by contract ownership, not file location.** The agent who owns the behavior seam gets the work, even if the code lives elsewhere.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **New v1 behavior automatically pulls proof work.** If a change adds or expands a support claim, launch O'Brien in parallel for examples, CI, docs, and ledger updates.
7. **Data owns short-term v1 bridge delivery.** Native-runtime work only becomes active when Picard explicitly opens it as a phase-4 decision lane.
8. **Geordi owns analyzer and Roslyn exploration.** Do not default analyzer work into v1; open it only after bridge and typegen behavior are stable.
9. **Tuvok reviews all sensitive-boundary work.** Anything involving secrets, diagnostics contracts, plugin semantics, or public/private config boundaries routes through Tuvok.
10. **A support claim is not accepted until it is proven.** Every first-class claim needs a proof artifact and a support-matrix ledger entry before Picard closes it.
11. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. Picard handles the `squad` inbox triage.
12. **@copilot routing** — when evaluating issues, check @copilot's capability profile in `team.md`. Route 🟢 good-fit tasks to `squad:copilot`, flag 🟡 tasks for review, and keep 🔴 tasks with squad members.

## Work Type → Agent

| Work Type | Primary | Secondary |
|-----------|---------|----------|
| architecture, phase gates, support claims, native evolution | Picard | O'Brien |
| bridge, configuration, hosting, options, reload | Data | Tuvok |
| MSBuild, C# generation, IDE builds, analyzers | Geordi | Data |
| diagnostics, contracts, redaction, plugins, public/private boundaries | Tuvok | Data |
| executable distribution, examples, support matrix, CI, docs | O'Brien | Picard |
| proof-artifact tracking, exit criteria, blockers | Ralph | Picard |
