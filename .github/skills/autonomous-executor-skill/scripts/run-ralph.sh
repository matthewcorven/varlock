#!/usr/bin/env bash
set -euo pipefail

# Usage: run-ralph.sh <WI-ID> --prompt-template <path> [--model <model>] [--max-iterations <N>] [--stream]
WI_ID="${1:?Usage: run-ralph.sh <WI-ID> --prompt-template <path> [--model <model>] [--max-iterations <N>] [--stream]}"
shift

MODEL=""
PROMPT_TEMPLATE=""
MAX_ITER="20"
STREAM=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --model) MODEL="$2"; shift 2 ;;
    --prompt-template) PROMPT_TEMPLATE="$2"; shift 2 ;;
    --max-iterations) MAX_ITER="$2"; shift 2 ;;
    --stream) STREAM=true; shift ;;
    *) echo "Unknown flag: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "${PROMPT_TEMPLATE}" ]]; then
  echo "❌ --prompt-template is required (mandatory per SKILL.md § Mandatory CLI Arguments)"
  exit 2
fi

# Resolve workspace root (git-aware)
WORKSPACE_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
RALPH_DIR="${WORKSPACE_ROOT}/.tmp/ralph"
REPO_DIR="${RALPH_DIR}/open-ralph-wiggum"
RALPH_BIN="${REPO_DIR}/bin/ralph.js"
PROMPT_FILE="${WORKSPACE_ROOT}/.tmp/ralph/prompts/${WI_ID}-prompt.md"
LOG_FILE="${RALPH_DIR}/logs/${WI_ID}.log"

if [[ ! -f "${PROMPT_FILE}" ]]; then
  echo "❌ Prompt file not found: ${PROMPT_FILE}"
  echo "   Create it or run the prompt generator first."
  exit 2
fi

if [[ ! -f "${RALPH_BIN}" ]]; then
  echo "❌ Ralph binary not found: ${RALPH_BIN}"
  echo "   Run setup-ralph.sh first."
  exit 2
fi

# Read model from individual WI state.json if not provided via --model
if [[ -z "${MODEL}" ]]; then
  # Convert WI-ID to folder prefix: WI-H01 → WI_H01
  WI_PREFIX=$(echo "${WI_ID}" | tr '-' '_')
  # Find the WI state.json in items/ or archive/
  WI_STATE=$(find "${WORKSPACE_ROOT}/workitems" -path "*/${WI_PREFIX}_*/${WI_PREFIX}_*.state.json" -print -quit 2>/dev/null)
  if [[ -n "${WI_STATE}" && -f "${WI_STATE}" ]]; then
    MODEL=$(python3 -c "
import json, sys
with open('${WI_STATE}') as f:
    state = json.load(f)
print(state.get('model', ''))
" 2>/dev/null || true)
  fi
fi

if [[ -z "${MODEL}" ]]; then
  echo "❌ No --model specified and could not read model from state.json for ${WI_ID}"
  exit 2
fi

mkdir -p "${RALPH_DIR}/logs"

# All flags are mandatory per SKILL.md § Mandatory CLI Arguments
CMD=(bun "${RALPH_BIN}"
  --agent copilot
  --prompt-file "${PROMPT_FILE}"
  --completion-promise COMPLETE
  --abort-promise ABORT
  --model "${MODEL}"
  --max-iterations "${MAX_ITER}"
  --verbose-tools
  --allow-all
  --no-commit
)

# Add --prompt-template (mandatory)
CMD+=(--prompt-template "${PROMPT_TEMPLATE}")

if [[ "${STREAM}" == true ]]; then
  echo "🔴 Running ralph (streaming enabled) → ${LOG_FILE}"
  "${CMD[@]}" 2>&1 | tee -a "${LOG_FILE}"
else
  echo "⚪ Running ralph (no live stream; --no-stream) → ${LOG_FILE}"
  "${CMD[@]}" --no-stream 2>&1 | tee -a "${LOG_FILE}"
fi

EXIT=$?
if [[ $EXIT -ne 0 ]]; then
  echo "❌ ralph exited with code: $EXIT" >&2
fi

exit $EXIT
