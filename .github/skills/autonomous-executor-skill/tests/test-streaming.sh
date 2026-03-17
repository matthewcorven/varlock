#!/usr/bin/env bash
set -euo pipefail

SKILL_DIR="$(cd "$(dirname "$0")/.." && pwd)"
WORKSPACE_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"

echo "Running setup to ensure environment is ready..."
bash "${SKILL_DIR}/scripts/setup-ralph.sh" "${WORKSPACE_ROOT}"

WI_ID="TEST-STREAM"
PROMPT_FILE="${WORKSPACE_ROOT}/.tmp/ralph/prompts/${WI_ID}-prompt.md"
LOG_FILE="${WORKSPACE_ROOT}/.tmp/ralph/logs/${WI_ID}.log"

mkdir -p "$(dirname "${PROMPT_FILE}")"

cat > "${PROMPT_FILE}" <<'PROMPT'
# Ralph test prompt — streaming smoke test
Goal: Exit quickly and output a single line with <promise>COMPLETE</promise>
Scope: None
Acceptance criteria:
- Output the literal token <promise>COMPLETE</promise> and exit.
Implementation note: This is a test prompt meant to finish quickly. Do not attempt long-running loops.
PROMPT

# Run ralph in streaming mode in background
# The wrapper still requires a prompt template input, even when it has to pre-render it for older Ralph builds.
# Use the backend-builder template since it's lighter and this is just a smoke test.
"${SKILL_DIR}/scripts/run-ralph.sh" "${WI_ID}" --model claude-sonnet-4.6 --prompt-template "${WORKSPACE_ROOT}/.github/skills/autonomous-executor-skill/templates/backend-builder.md" --stream &
RALPH_PID=$!

# Wait up to 60 seconds for a log file with content
SECONDS=0
TIMEOUT=60
while [[ $SECONDS -lt $TIMEOUT ]]; do
  if [[ -f "${LOG_FILE}" && -s "${LOG_FILE}" ]]; then
    echo "✅ Log file created and non-empty: ${LOG_FILE} (size: $(stat -f%z "${LOG_FILE}" 2>/dev/null || stat -c%s "${LOG_FILE}"))"
    # For streaming smoke test we only assert the log receives output (don't require full completion)
    kill $RALPH_PID 2>/dev/null || true
    exit 0
  fi
  sleep 1
done

# If we reach here, test failed
echo "❌ Streaming test failed: log not created or completion token missing after ${TIMEOUT}s"
[[ -f "${LOG_FILE}" ]] && echo "--- Log head ---" && head -n 40 "${LOG_FILE}" || true
kill $RALPH_PID 2>/dev/null || true
exit 2
