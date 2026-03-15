---
name: autonomous-executor-skill
description: Orchestrate autonomous task execution using Open Ralph Wiggum's agentic loop with GitHub Copilot CLI. This skill should be used when the user asks to execute, run, or complete tasks autonomously — e.g. "run these with ralph", "execute this task", "use ralph to complete this". It downloads Open Ralph Wiggum into a workspace temp folder, parses execution specs to generate effective prompts, validates prerequisites and detects ambiguities, then delegates execution to a subagent that runs the ralph CLI with --agent copilot.
---

# Autonomous Executor Skill

Orchestrate autonomous task completion using [Open Ralph Wiggum](https://github.com/Th0rgal/open-ralph-wiggum) — an agentic loop that drives GitHub Copilot CLI (or other agents) through self-correcting iterations until a task is fully implemented and validated.

## When to Use

- User requests autonomous execution of one or more tasks (e.g., "run this with ralph", "ralph this")
- User wants to delegate multi-step implementation to an unattended agentic loop
- Tasks have clear acceptance criteria and testable outcomes (Wiggum's sweet spot)
- User says "execute", "run ralph", "ralph loop", or references completing tasks hands-off

## When NOT to Use

- Tasks requiring human judgment at every step (e.g., branding decisions, UX approval)
- One-shot operations that complete in seconds
- Requests with unclear or missing success criteria
- Production debugging or live incident response

## Orchestration Workflow

You are the executing agent, and will therefore act as **orchestrator** — you will NOT implement the task directly. Instead, you will:

1. **Parse** the assigned task(s)
2. **Validate** prerequisites, detect ambiguities, and ask the user questions if conflicts or missing information are found
3. **Prepare** the Wiggum environment (downloads/installs if needed)
4. **Generate** an effective Wiggum prompt from the task spec
5. **Delegate** execution to a subagent that runs the `ralph` CLI
6. **Verify** completion by checking artifacts, test results, and acceptance criteria

### Step 1: Parse Tasks

Read the task specification markdown file(s) from `tasks/items/TASK_{ID}.md` or the path provided by the user. Extract:

- **Goal** — what the task achieves
- **Status** — must be `Proposed` or `Ready` (skip `Done`, `Blocked`, or `In-Progress`)
- **Dependencies** — list of prerequisite task IDs
- **Scope (In/Out)** — boundaries of implementation
- **Acceptance criteria** — verifiable conditions
- **Test plan** — unit, integration, E2E, DB, telemetry requirements
- **Implementation notes** — API contract references, conventions
- **Artifact path** — `tasks/artifacts/TASK-{ID}/`

### Step 2: Validate Prerequisites and Detect Ambiguities

Before proceeding, check:

1. **Dependency status:** Read each dependency task's status. If any dependency is not `Done`, warn the user and ask whether to proceed anyway or abort.
2. **Missing acceptance criteria:** If the acceptance criteria section is empty or contains only template placeholders, flag this and ask the user to define concrete criteria.
3. **Conflicting scope:** If the task's "Out" scope conflicts with what the "In" scope implies, flag it.
4. **Database pre-conditions:** If the task touches the database, confirm whether `./scripts/reset-dev-db.sh` has been run (or offer to run it).
5. **Environment requirements:** Check for required environment variables mentioned in the task spec (e.g., `DOGTRIALS_TEST_SQL`, `ENABLE_TEST_AUTH`).
6. **UI design prompt presence:** If the task has a `<ui-design-agent-prompt>` section with real content (not "N/A"), note that Wiggum should process mockups as part of implementation.

7. **Retry budget:** Ask the user for the maximum number of retry-to-complete attempts per task. A "retry" is a full re-invocation of the Wiggum loop for the same task after a failed verification (Step 6). Default suggestion: **2 retries** (meaning up to 3 total attempts per task: 1 initial + 2 retries). Store this value as `maxRetriesPerTask` and enforce it strictly throughout orchestration — never exceed it.

Use the `ask_questions` tool to surface any ambiguities or conflicts to the user, **including the retry budget question**. Only proceed when all blockers are resolved and the retry budget is confirmed.

### Step 3: Prepare Wiggum Environment

Run `scripts/setup-ralph.sh` from this skill's directory to ensure Wiggum is available. The script:

1. Creates `.tmp/ralph/` in the workspace root if it doesn't exist
2. Clones or updates the open-ralph-wiggum repository into `.tmp/ralph/open-ralph-wiggum/`
3. Installs dependencies with `bun install`
4. Verifies the `ralph` command works

Execute the setup script:

```bash
bash <skill-dir>/scripts/setup-ralph.sh <workspace-root>
```

Where `<skill-dir>` is the absolute path to this skill's directory and `<workspace-root>` is the root of the working repository (e.g., the dog-trials workspace root).

If setup fails (e.g., `bun` not installed), report the error to the user with remediation steps.

### Step 4: Generate Wiggum Prompt

Transform the parsed task into an effective Wiggum prompt. Follow Wiggum's [recommended PRD format](https://github.com/Th0rgal/open-ralph-wiggum#recommended-prd-format) and the `references/prompt-engineering.md` reference in this skill.

To generate the prompt:

1. Read the `references/prompt-engineering.md` file from this skill for prompt structure guidance
2. Read the full task specification markdown
3. Read the project's `.github/copilot-instructions.md` for repo conventions
4. **Select the agent prompt template** (Step 4a below) based on task scope
5. **Run the Pre-Execution Consistency Check** (Step 4b below) and incorporate its constraints
6. Compose a prompt file and write it to `.tmp/ralph/prompts/TASK-{ID}-prompt.md`

The generated prompt file (`.tmp/ralph/prompts/TASK-{ID}-prompt.md`) contains the **task-specific content** that gets injected into the template's `{{prompt}}` variable. It MUST include:

- **Goal** from the task spec
- **Full scope** (In and Out)
- **Implementation notes** from the task spec
- **Project conventions** — key rules from copilot-instructions.md (API conventions, error format, auth, testing, artifacts)
- **Acceptance criteria** as verifiable checkpoints
- **Artifact generation** — instructions to create required artifacts under `tasks/artifacts/TASK-{ID}/`
- **Selector & structure constraints** — output from the consistency check (Step 4b): CSS class names, aria attributes, query selectors, and DOM structure that existing tests depend on

**Note:** The prompt file does NOT need to include completion promise phrasing, loop iteration instructions, or test/build commands — these are provided by the agent prompt template (Step 4a). The template's `{{completion_promise}}` variable is populated from Wiggum's `--completion-promise` flag (default: `COMPLETE`).

### Step 4a: Select Agent Prompt Template

**Purpose:** Each Wiggum run uses a `--prompt-template` file that embeds the correct agent identity, conventions, and build/test commands into every iteration prompt. Templates live in `.github/skills/autonomous-executor-skill/templates/`.

**Template selection rules:**

| Task scope                     | Template file                                                            | When to use                                                                 |
| ------------------------------ | ------------------------------------------------------------------------ | --------------------------------------------------------------------------- |
| `src/web/` only                | `.github/skills/autonomous-executor-skill/templates/frontend-builder.md` | UI components, pages, styles, client-side routing                           |
| `src/api/` only                | `.github/skills/autonomous-executor-skill/templates/backend-builder.md`  | Endpoints, entities, migrations, background processing                      |
| Both `src/web/` and `src/api/` | Two sequential Wiggum runs                                               | Backend first (`backend-builder.md`), then frontend (`frontend-builder.md`) |

**Note:** The test-writer is always dispatched as a direct subagent, never via Wiggum. There is no `test-writer.md` template.

**How to determine scope:** Read the task spec's `code-locations` frontmatter field and its "Scope (In)" section. If both `src/web/` and `src/api/` paths appear, split into two sequential Wiggum runs — backend first (API endpoints/entities), then frontend (components/pages that consume the API).

**Cross-layer tasks:** When a task spans both layers, the orchestrator generates two prompt files:

- `.tmp/ralph/prompts/TASK-{ID}-backend-prompt.md` (scoped to API changes)
- `.tmp/ralph/prompts/TASK-{ID}-frontend-prompt.md` (scoped to UI changes, referencing the API just built)

Each runs as its own Wiggum invocation with its respective template. The backend runs first so the frontend can consume the new/changed endpoints.

**How templates work:** Wiggum's `--prompt-template` flag provides a custom template that wraps `{{prompt}}` (the task prompt from `--prompt-file`) with agent identity, conventions, and loop instructions. This replaces Wiggum's default iteration prompt entirely, ensuring Copilot CLI operates with the correct agent persona throughout the loop.

Store the selected template path for use in Step 5.

### Step 4b: Pre-Execution Consistency Check (Subagent)

**Purpose:** Prevent test failures caused by Wiggum changing DOM structure, class names, or aria attributes that existing tests depend on. This was identified as the #1 source of retry-worthy failures during orchestration.

**Dispatch a subagent** (using `runSubagent`) with a research-only prompt that:

1. **Identifies affected files:** From the task's scope, determine which source files and test files will be modified or impacted.
2. **Extracts test selectors:** Read each affected test file and extract:
   - CSS selectors used in `querySelector` / `querySelectorAll` / `getByRole` / `getByText` calls
   - Class names referenced in assertions (e.g., `.option`, `.dropdown`, `.spinner`)
   - `aria-*` attributes checked in assertions (e.g., `aria-selected`, `aria-expanded`, `aria-label`)
   - `data-*` attributes used for test hooks
   - Component tag names and expected DOM hierarchy
3. **Extracts source contracts:** Read each affected source file and extract:
   - CSS class names currently rendered in the template/innerHTML
   - aria attributes currently set
   - Custom properties and CSS variables consumed
   - Slot names (if migrating from Shadow DOM) and their replacement strategy
4. **Cross-references:** Compare test expectations against source reality. Flag:
   - Class names tests assert that the source currently renders
   - Aria attributes tests check that must be preserved
   - DOM hierarchy assumptions (e.g., "test expects `.select > .dropdown > .option`")
5. **Returns a constraint block** formatted as:

```markdown
## Selector & Structure Constraints (DO NOT BREAK)

### {component-name}

- Test file: `src/web/src/components/{component}.test.ts`
- Required CSS classes: `.class1`, `.class2` (used in test lines X, Y)
- Required aria attributes: `aria-selected`, `aria-expanded` (asserted in test lines X, Y)
- Required DOM structure: `{tag} > .parent > .child`
- Required data attributes: `data-value`, `data-testid`
```

Embed this constraint block directly into the Wiggum prompt. This ensures Wiggum preserves all test-observable contracts even when refactoring internals.

**When to skip:** If the task creates entirely new files with no pre-existing tests, this step produces no constraints (but should still run to confirm that assumption).

### Step 5: Execute Wiggum via Subagent

> **Placeholder convention:** Throughout this document, `{ID}` and `{TASK-ID}` both refer to the **short identifier** (e.g., `F16`, `BUG01`). When the full identifier is needed, it's written as `TASK-{ID}` (e.g., `TASK-F16`). Examples: `.worktrees/{TASK-ID}` → `.worktrees/F16`, `blocked-TASK-{ID}.json` → `blocked-TASK-F16.json`.

Delegate execution to a subagent that runs the Wiggum CLI. The subagent prompt should instruct it to:

1. Change directory to the task's worktree (`.worktrees/{TASK-ID}`)
2. Execute Wiggum with the generated prompt file and **all mandatory flags**
3. Monitor for completion, abort, or max-iterations exhaustion
4. Return the final status and any error output

#### Mandatory CLI Arguments

Every Wiggum invocation **must** include these flags. No exceptions — omitting any of them is a workflow violation.

| Flag                            | Value                                                               | Rationale                                                                   |
| ------------------------------- | ------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| `--agent copilot`               | Always `copilot`                                                    | Our agent runtime is GitHub Copilot CLI                                     |
| `--prompt-template <path>`      | Template from `.github/skills/autonomous-executor-skill/templates/` | Agent identity + conventions (selected in Step 4a)                          |
| `--prompt-file <path>`          | `.tmp/ralph/prompts/TASK-{ID}-prompt.md`                            | Task-specific execution specification                                       |
| `--max-iterations <N>`          | Based on complexity (see table below)                               | Safety net for runaway loops                                                |
| `--completion-promise COMPLETE` | Always `COMPLETE`                                                   | Signals successful task completion                                          |
| `--abort-promise ABORT`         | Always `ABORT`                                                      | Signals early exit (BLOCKED or FAILURE — type determined by JSON file)      |
| `--model <model>`               | From `state.json` (PM decides at registration)                      | PM selects model based on task complexity                                   |
| `--verbose-tools`               | Always present                                                      | Orchestrator needs visibility into every tool call                          |
| `--allow-all`                   | Always present                                                      | Agent operates with maximum empowerment — no interactive permission prompts |
| `--no-commit`                   | Always present                                                      | Only the git-committer agent commits changes                                |
| `--no-stream`                   | Default (omit for streaming)                                        | Buffer output; omit when live monitoring is desired                         |

#### Full Command

```bash
cd .worktrees/{TASK-ID} && \
  bun ../../.tmp/ralph/open-ralph-wiggum/bin/ralph.js \
    --agent copilot \
   --prompt-template ../../.github/skills/autonomous-executor-skill/templates/<selected-template>.md \
    --prompt-file .tmp/ralph/prompts/TASK-{ID}-prompt.md \
    --max-iterations <N> \
    --completion-promise COMPLETE \
    --abort-promise ABORT \
    --model <model-from-state> \
    --verbose-tools \
    --allow-all \
    --no-commit \
    --no-stream
```

Wiggum runs **from the task's worktree directory** (`.worktrees/{TASK-ID}`), ensuring all file reads/writes are isolated to the task's worktree. The `--prompt-template` flag selects the agent identity (determined in Step 4a). The `--prompt-file` provides the task-specific execution spec. Wiggum substitutes the prompt file content into the template's `{{prompt}}` variable, creating a complete iteration prompt with agent persona + task context.

#### Model Selection

The PM assigns a model at task registration time and writes it to `state.json`. The orchestrator reads it when constructing the Wiggum command. Two tiers:

| Tier         | Model               | When                                                         |
| ------------ | ------------------- | ------------------------------------------------------------ |
| **Standard** | `claude-sonnet-4.6` | `complexity: small` or `complexity: medium` (est-hours ≤ 12) |
| **Premium**  | `claude-opus-4.6`   | `complexity: high` or `complexity: xl` (est-hours > 12)      |

The human can override the PM's selection by specifying `model: <model-name>` in task frontmatter or via an explicit command to the orchestrator.

#### Abort Signal Handling

The `--abort-promise ABORT` flag tells Wiggum to exit immediately when the agent outputs `<promise>ABORT</promise>`. Two abort types are defined in the prompt templates:

| Type        | JSON File                               | When to Use                                                                                                           | Orchestrator Action                                                  |
| ----------- | --------------------------------------- | --------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| **BLOCKED** | `../../.tmp/ralph/blocked-TASK-{ID}.json` | Unresolvable external issue — missing dependency, missing env var, infrastructure failure, precondition not met       | Mark task as `status: blocked`, escalate to human. **Do not retry.**   |
| **FAILURE** | `../../.tmp/ralph/failed-TASK-{ID}.json`  | Exhausted all approaches — persistent test failures, unresolvable build errors, unable to satisfy acceptance criteria | Retry with `--add-context` if budget allows, else escalate to human. |

The agent writes the appropriate JSON file (with a `reason` field) **before** outputting `<promise>ABORT</promise>`. The orchestrator reads the file after Wiggum exits to determine the abort type.

If Wiggum exits via **max-iterations** without any promise, the orchestrator treats it as an implicit FAILURE (retry-eligible).

> **Streaming (opt-in):** When you want to monitor Wiggum's stdout/stderr live, omit `--no-stream` and enable streaming in the orchestrator. The orchestrator will pipe stdout/stderr into `.tmp/ralph/logs/TASK-{ID}.log`. Use the `watch-ralph.sh` helper to follow the log in another terminal.

Example (local):

```bash
# run the task with streaming
.github/skills/autonomous-executor-skill/scripts/run-ralph.sh TASK-123 --stream

# in another terminal, follow progress
.github/skills/autonomous-executor-skill/scripts/watch-ralph.sh TASK-123
```

**Additional flags (situational):**

| Flag                     | Purpose                                                                                                                                                                    |
| ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `--add-context "<text>"` | Inject additional context into the prompt at runtime. Used primarily for retries — include specific failure diagnostics (test output, build errors, selectors that broke). |
| (omit `--no-stream`)     | Enable streaming of Wiggum stdout/stderr into `.tmp/ralph/logs/TASK-{ID}.log` so it can be tailed from another terminal.                                                     |

**Choosing `--max-iterations`:**

| Task Complexity | Estimated Hours                        | Max Iterations |
| -------------------- | -------------------------------------- | -------------- |
| Small (≤4 hrs)       | Config, token changes, simple entities | 10             |
| Medium (5–12 hrs)    | Endpoint + tests, component migration  | 20             |
| High (13–24 hrs)     | Full feature + E2E + DB verification   | 30             |
| XL (25+ hrs)         | Multi-entity features, form sections   | 40             |

Use the "Est. Hours" column from `workitems/README.md` to determine complexity.

**For multiple work items:** Execute them sequentially in dependency order. After each completes, verify before starting the next.

### Step 6: Verify Completion

After Wiggum finishes (via `<promise>COMPLETE</promise>`, `<promise>ABORT</promise>`, or max-iterations exhaustion):

> **Check abort files first:** If Wiggum exited via ABORT or max-iterations, check for `../../.tmp/ralph/blocked-WI-{ID}.json` (→ escalate, don't verify) or `../../.tmp/ralph/failed-WI-{ID}.json` (→ retry with `--add-context` if budget allows). Only proceed to verification below if Wiggum exited via COMPLETE.

Then verify:

1. **Build check:** Run `cd src/web && npm run build` and `cd src/api && dotnet build` — both must succeed with zero errors
2. **Test results:** Run the test commands specified in the work item's test plan and confirm all pass
3. **Artifact presence:** Check that `workitems/artifacts/WI-{ID}/` exists and contains the expected files:
   - `unit/test-results.txt` (if unit tests required)
   - `playwright/screenshots/` (if E2E required)
   - `playwright/test-results.txt` (if E2E required)
   - `db/verification-output.txt` (if DB verification required)
   - `telemetry/` (if telemetry verification required)
4. **E2E screenshot verification (MANDATORY for E2E work items):** If the work item requires E2E tests with screenshots, the orchestrator MUST verify that actual PNG files were captured — not just that the directory exists. Run:
   ```bash
   SCREEN_COUNT=$(find workitems/artifacts/WI-{ID}/playwright/screenshots -name '*.png' 2>/dev/null | wc -l)
   echo "Screenshots captured: $SCREEN_COUNT"
   ```
   If `SCREEN_COUNT` is 0, the E2E tests were NOT actually executed. This counts as a **verification failure** and must trigger a retry (re-run Wiggum with `--add-context` explaining that E2E tests must actually execute against running servers, not just compile). The orchestrator must NEVER accept an E2E work item as complete without verified screenshot artifacts.
5. **Acceptance criteria:** Manually verify each criterion is met by reading the changed code
6. **Code quality audit:** Run targeted grep/search checks relevant to the work item's domain. Examples:
   - Migration work: verify no remnants of the old pattern (e.g., `attachShadow`, `shadowRoot`, inline `<style>` tags)
   - API work: verify ProblemDetails error format, `x-support-id` header presence
   - Component work: verify no hardcoded colors (should use tokens), proper aria attributes
   - General: verify no `console.log` left behind, no `// TODO` from Wiggum
7. **Work item status:** Update the work item's `**Status:**` to `Done` if everything passes

   **Pipeline mode:** When Wiggum is invoked by the orchestrator (not directly by the human), items 7 and 8 are handled by the orchestrator's PM micro-invocation and git-committer dispatch respectively. Wiggum should return structured results and exit without performing these steps.

8. **Commit immediately:** After verification passes, stage all changes and commit before proceeding to the next work item. This ensures each WI is an atomic, revertable unit and prevents loss of work if a later WI fails. Use:

   ```bash
   git add -A
   # Write commit message to temp file to avoid shell escaping issues
   cat > .tmp/commit-msg.txt << 'EOF'
   WI-{ID}: {Title} (Done)

   - {Summary of changes}
   - All tests pass
   - Artifacts: workitems/artifacts/WI-{ID}/
   EOF
   git commit -F .tmp/commit-msg.txt
   ```

   **This is mandatory for multi-WI orchestration** — do not defer commits to a post-batch step.

If verification fails, the orchestrator checks the retry budget:

1. **If `retriesUsed < maxRetriesPerWI`:** Increment `retriesUsed` for this WI. Re-run Wiggum with `--add-context` providing the specific failure details. Use the same `--max-iterations` as the original attempt.
2. **If `retriesUsed >= maxRetriesPerWI`:** Stop retrying this work item. Report the failure to the user with:
   - Which acceptance criteria passed vs. failed
   - Build/test error output
   - Number of attempts exhausted (e.g., "3/3 attempts used")
   - Suggestion: increase retry budget, intervene manually, or skip this WI
3. **Never silently exceed the budget.** The retry limit is a hard ceiling set by the human operator.

## Error Handling

| Situation                                   | Action                                                                                                                                                                                                                                      |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `bun` not installed                         | Tell user: "Install Bun runtime via `curl -fsSL https://bun.sh/install \| bash`"                                                                                                                                                            |
| `copilot` CLI not available                 | Tell user: "Install GitHub Copilot CLI: ensure `gh copilot` extension or standalone `copilot` binary is on PATH"                                                                                                                            |
| Wiggum exits via ABORT promise              | Check for `../../.tmp/ralph/blocked-WI-{ID}.json` (BLOCKED → escalate, don't retry) or `../../.tmp/ralph/failed-WI-{ID}.json` (FAILURE → retry if budget allows). If neither file exists, check `ralph --status` and `.ralph/` state files. |
| Max iterations reached (no promise)         | Implicit FAILURE. Increment `retriesUsed`, retry with `--add-context` if budget allows, otherwise report to user.                                                                                                                           |
| Dependency WI not Done                      | Warn user, offer to execute dependency first or proceed with acknowledged risk                                                                                                                                                              |
| Build/test failures after Wiggum completion | Re-run ralph with `--add-context` describing the specific failures (include exact error messages, failing test names, and the selector/DOM mismatches if applicable)                                                                        |
| Commit message garbled by shell             | Use `git commit -F <file>` instead of `git commit -m "..."` for multi-line messages                                                                                                                                                         |
| Wiggum writes BLOCKED file                  | `../../.tmp/ralph/blocked-WI-{ID}.json` exists → unresolvable blocker. Read the file for details, set WI to `status: blocked`, and escalate to the human. Do not retry — BLOCKED is not a transient failure.                                |
| Wiggum writes FAILED file                   | `../../.tmp/ralph/failed-WI-{ID}.json` exists → agent exhausted approaches. Read the file for failure details, retry with `--add-context` including the failure reason if budget allows, otherwise escalate.                                |

## Multi-Work-Item Orchestration

When assigned multiple work items:

1. **Sort by dependency order** — topological sort based on `Dependencies:` field
2. **Ask retry budget once** — at the start, ask the user for `maxRetriesPerWI` (applies uniformly to every work item in the batch)
3. **Execute sequentially** — one Wiggum loop per work item, tracking `retriesUsed` per WI (starts at 0)
4. **Gate on verification** — do not start WI-N+1 until WI-N verification passes or retry budget is exhausted
5. **Cross-WI assumption validation** — after WI-N completes, dispatch a lightweight subagent (research-only) to read WI-N+1's scope and acceptance criteria, then verify the current codebase still satisfies N+1's assumptions. If WI-N introduced naming changes, file moves, or API signature changes that N+1's prompt references, update the N+1 prompt before execution. This prevents cascading failures across sequential work items.
6. **On WI failure after budget exhaustion** — ask the user: continue to next WI (skip), abort remaining, or increase budget for this WI
7. **Accumulate state** — each successive Wiggum instance sees code changes from prior work items
8. **Report progress** — after each work item, summarize: completed, failed (with attempt counts), remaining
9. **Per-WI commit** — commit immediately after each WI passes verification (Step 6, item 7). Each work item gets its own atomic commit. This ensures:
   - Clean `git log` with one commit per work item
   - Easy revert if a later WI introduces issues
   - No loss of validated work if the orchestration is interrupted
   - Each successive Wiggum instance starts from a clean git state

   For multi-line commit messages, write the message to a temp file (e.g., `.tmp/commit-msg.txt`) and use `git commit -F .tmp/commit-msg.txt` to avoid shell escaping issues with `git commit -m`.

## Resources

### scripts/setup-ralph.sh

Executable script that downloads and installs Open Ralph Wiggum from the github repository into `.tmp/ralph/` in the workspace.

### references/prompt-engineering.md

Detailed guide for generating effective Wiggum prompts from work item markdown, including template structure, examples, and anti-patterns specific to this project.
