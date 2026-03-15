# Prompt Engineering for Ralph Work Item Execution

Guide for generating effective Ralph prompts from dog-trials work item markdown files. The orchestrating agent reads this reference when composing the prompt file that Ralph will loop on.

## Prompt Structure Template

Every generated prompt MUST follow this structure:

```markdown
# Work Item: {WI-ID} — {Title}

## Goal
{Paste the Goal section verbatim from the work item.}

## Scope

### In Scope
{Paste the In scope bullet list from the work item.}

### Out of Scope
{Paste the Out scope bullet list from the work item. Ralph must respect these boundaries.}

## Project Conventions (MUST FOLLOW)

- All API endpoints are under `/api`; responses are JSON only.
- Errors use `application/problem+json` (RFC 7807 ProblemDetails) with validation `errors` map.
- Every response includes `x-support-id` header; ProblemDetails includes matching `traceId`.
- IDs are GUID strings; timestamps are UTC ISO 8601 with `Z` suffix.
- EF Core migrations: use `dotnet ef migrations add ...` — never hand-author Designer.cs files.
- Never log PII (emails, phones, addresses, breeder names). Use stable `errorCode` + `traceId`.
- TestAuth: `POST /api/testauth/token` gated by `ENABLE_TEST_AUTH` env var.
- If adding/modifying endpoints or DTOs, update `docs/prd/PRD_MVP_API_Contract.md` in the same change.

## Implementation Notes
{Paste the Implementation Notes section from the work item.}

## Acceptance Criteria
{Paste acceptance criteria as a numbered list. Each item must be independently verifiable.}

## Test Plan

### Unit Tests
{Include test commands and expected behavior. Example:}
Run: `cd src/web && npx vitest run --reporter=verbose 2>&1 | tee workitems/artifacts/{WI-ID}/unit/test-results.txt`
OR: `cd src/api && dotnet test DogTrials.Api.Tests --filter "Category={WI-ID}" -v minimal 2>&1 | tee workitems/artifacts/{WI-ID}/unit/test-results.txt`

### Integration Tests (if applicable)
Run: `DOGTRIALS_TEST_SQL='Server=localhost,1433;Database=DogTrialsTests;...' dotnet test src/DogTrials.sln -v minimal`

### E2E Tests (if applicable)
Run: `cd src/tests/e2e && npx playwright test --reporter=list`
Screenshots: Save to `workitems/artifacts/{WI-ID}/playwright/screenshots/` using naming convention `{NN}-{use-case}-{action}.png`

### DB Verification (if applicable)
{Include specific SQL queries or verification commands.}

## Artifact Requirements

Create the following artifact directories and files:
- `workitems/artifacts/{WI-ID}/unit/test-results.txt` — unit test output
- `workitems/artifacts/{WI-ID}/playwright/screenshots/` — E2E screenshots (if applicable)
- `workitems/artifacts/{WI-ID}/playwright/test-results.txt` — E2E test output (if applicable)
- `workitems/artifacts/{WI-ID}/db/verification-output.txt` — DB verification (if applicable)
- `workitems/artifacts/{WI-ID}/telemetry/` — telemetry verification (if applicable)

Create artifact directories with: `mkdir -p workitems/artifacts/{WI-ID}/{unit,playwright/screenshots,db,telemetry}`

## Completion Rules

> **Note:** Completion and abort rules are defined in the agent prompt templates (`.github/skills/autonomous-executor-skill/templates/frontend-builder.md` and `.github/skills/autonomous-executor-skill/templates/backend-builder.md`). The templates inject these rules into every Ralph iteration via `{{completion_promise}}` and `{{abort_promise}}` variables. **Do NOT duplicate completion/abort rules in the generated prompt file** — the template already provides them.

The generated prompt file (from `--prompt-file`) should focus on task-specific content: goal, scope, acceptance criteria, implementation notes, artifact requirements. It must NOT include:
- Completion promise phrasing (template provides this)
- Abort/BLOCKED/FAILURE instructions (template provides this)
- Loop iteration instructions (template provides this)
- Build/test commands (template provides this)

## Abort Rules

Abort rules are provided by the agent prompt template. See `.github/skills/autonomous-executor-skill/templates/` for the canonical definitions. Two abort types exist:
- **BLOCKED** — unresolvable external issue → writes `../../.tmp/ralph/blocked-WI-{ID}.json`
- **FAILURE** — exhausted approaches → writes `../../.tmp/ralph/failed-WI-{ID}.json`

Both output `<promise>ABORT</promise>` after writing the JSON file.
```

## Prompt Composition Rules

### DO

1. **Include the full work item content** — Ralph's agent sees only the prompt; it has no other context about what to build.
2. **Include concrete commands** — Ralph's agent should know exactly how to run tests, builds, and linters without guessing.
3. **Include file paths** — Reference exact source file locations (e.g., `src/web/src/components/dt-button.ts`, `src/api/DogTrials.Api/Endpoints/TrialsEndpoints.cs`).
4. **Include the "Out of Scope" section** — Prevents the agent from wandering into adjacent work items.
5. **Use verifiable conditions** — "All 12 unit tests pass" not "tests work."
6. **Do NOT include completion/abort instructions** — The agent prompt template (`--prompt-template`) injects these via `{{completion_promise}}` and `{{abort_promise}}` variables. The generated prompt file should focus on task-specific content only.

### DON'T

1. **Don't include ambient project context** — The agent will discover the codebase through iteration. Only include conventions it couldn't reasonably infer.
2. **Don't ask open-ended questions** — Ralph loops don't support interactive Q&A. All decisions must be made in the prompt.
3. **Don't include instructions for multiple work items in one prompt** — One prompt per work item. Use Ralph's tasks mode only if the work item itself has explicit sub-tasks.
4. **Don't reference external URLs** — The agent can't browse the web. Include all necessary content inline.
5. **Don't set conflicting goals** — If the work item has both "create" and "don't modify" for related files, resolve the conflict before generating the prompt.

## Adapting for Work Item Types

### API/Backend Work Items

Work items that touch entities, migrations, endpoints, or background processing. Emphasize:
- Entity/migration commands: `dotnet ef migrations add {Name} --project src/api/DogTrials.Api`
- Endpoint patterns: reference existing endpoints in `src/api/DogTrials.Api/Endpoints/`
- ProblemDetails error handling
- DB constraint verification queries
- Integration test patterns from `src/api/DogTrials.Api.Tests/`

### UI/Frontend Work Items

Work items that touch web components, styles, or page layouts. Emphasize:
- Web Components pattern: `BaseElement` + light DOM + Tailwind CSS
- Component file locations: `src/web/src/components/`
- Vitest commands: `cd src/web && npx vitest run`
- Accessibility requirements (ARIA, keyboard navigation)
- Screenshot requirements for visual verification
- Reference `docs/prd/PRD_MVP_UI_Tenets.md` for design token and styling conventions

### Infrastructure/Config Work Items

Work items that touch build config, orchestration, CI/CD, or Azure infrastructure. Emphasize:
- Configuration file locations
- Environment variable requirements
- Docker/container commands if applicable
- Verification commands that prove the config works

## Example: Generating a Prompt for a UI Config Work Item

Given a work item like `workitems/items/WI_XXX_Token_Palette_Alignment.md`, the orchestrator would:

1. Read the full work item
2. Extract: Goal (palette alignment), Scope (tokens.css + tailwind.config.js changes), Acceptance criteria (specific color values match)
3. Note: no DB, no E2E, unit tests for token loading only
4. Note: "Decision Required" section → orchestrator must resolve with user BEFORE generating prompt (Step 2 validation)
5. Generate prompt with:
   - Concrete file paths: `src/web/src/styles/tokens.css`, `src/web/tailwind.config.js`
   - Target values table from the work item
   - Build verification: `cd src/web && npm run build`
   - Test command: `cd src/web && npx vitest run`
   - Artifact: `workitems/artifacts/WI-{ID}/unit/test-results.txt`
   - Completion promise

## Anti-Patterns

| Anti-Pattern | Why It Fails | Fix |
|--------------|-------------|-----|
| "Make the code better" | No verifiable criteria | List specific changes with pass/fail conditions |
| Including 3 work items in one prompt | Agent loses focus, burns tokens | One prompt per work item |
| "Update all the tests" | Unbounded scope | "Run `npm test`, fix any failures, ensure 0 exit code" |
| Referencing PRD URLs | Agent can't fetch URLs | Inline the relevant PRD sections |
| Missing Out-of-Scope section | Agent refactors unrelated code | Always include boundaries |
| No test commands | Agent guesses wrong test runner | Include exact shell commands |
| "Output DONE when ready" | Mismatches template's completion promise variable | Don't include completion instructions in prompt — template handles this |
