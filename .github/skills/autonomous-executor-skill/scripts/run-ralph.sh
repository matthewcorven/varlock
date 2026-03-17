#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: run-ralph.sh <WI-ID> --prompt-template <path> [--model <model>] [--max-iterations <N>] [--stream] [--dry-run]

Compatibility behavior:
  - Detects supported Ralph CLI flags from `ralph --help`
  - Pre-renders the template into a generated prompt file when `--prompt-template` is unsupported
  - Omits `--abort-promise` when the installed Ralph build does not advertise it
EOF
}

WI_ID="${1:-}"
if [[ -z "${WI_ID}" ]]; then
  usage >&2
  exit 2
fi
shift

MODEL=""
PROMPT_TEMPLATE=""
MAX_ITER="20"
STREAM=false
DRY_RUN=false
COMPLETION_PROMISE="COMPLETE"
ABORT_PROMISE="ABORT"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --model) MODEL="$2"; shift 2 ;;
    --prompt-template) PROMPT_TEMPLATE="$2"; shift 2 ;;
    --max-iterations) MAX_ITER="$2"; shift 2 ;;
    --completion-promise) COMPLETION_PROMISE="$2"; shift 2 ;;
    --abort-promise) ABORT_PROMISE="$2"; shift 2 ;;
    --stream) STREAM=true; shift ;;
    --dry-run) DRY_RUN=true; shift ;;
    --help|-h) usage; exit 0 ;;
    *) echo "Unknown flag: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "${PROMPT_TEMPLATE}" ]]; then
  echo "❌ --prompt-template is required by the wrapper"
  exit 2
fi

WORKSPACE_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
RALPH_DIR="${WORKSPACE_ROOT}/.tmp/ralph"
REPO_DIR="${RALPH_DIR}/open-ralph-wiggum"
RALPH_BIN="${REPO_DIR}/bin/ralph.js"
PROMPT_FILE="${WORKSPACE_ROOT}/.tmp/ralph/prompts/${WI_ID}-prompt.md"
RENDERED_PROMPTS_DIR="${RALPH_DIR}/rendered-prompts"
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

if [[ ! -f "${PROMPT_TEMPLATE}" ]]; then
  echo "❌ Prompt template not found: ${PROMPT_TEMPLATE}"
  exit 2
fi

if [[ -z "${MODEL}" ]]; then
  WI_PREFIX=$(echo "${WI_ID}" | tr '-' '_')
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
mkdir -p "${RENDERED_PROMPTS_DIR}"

RALPH_HELP="$(bun "${RALPH_BIN}" --help 2>&1 || true)"

flag_supported() {
  local flag="$1"
  printf '%s\n' "${RALPH_HELP}" | grep -Fq -- "${flag}"
}

render_prompt_template() {
  local template_content prompt_content rendered_prompt rendered_prompt_file

  template_content="$(<"${PROMPT_TEMPLATE}")"
  prompt_content="$(<"${PROMPT_FILE}")"

  rendered_prompt="${template_content}"
  rendered_prompt="${rendered_prompt//'{{prompt}}'/${prompt_content}}"
  rendered_prompt="${rendered_prompt//'{{completion_promise}}'/${COMPLETION_PROMISE}}"
  rendered_prompt="${rendered_prompt//'{{abort_promise}}'/${ABORT_PROMISE}}"
  rendered_prompt="${rendered_prompt//'{{iteration}}'/1}"
  rendered_prompt="${rendered_prompt//'{{max_iterations}}'/${MAX_ITER}}"
  rendered_prompt="${rendered_prompt//'{{min_iterations}}'/1}"
  rendered_prompt="${rendered_prompt//'{{context}}'/}"
  rendered_prompt="${rendered_prompt//'{ID}'/${WI_ID}}"

  rendered_prompt_file="${RENDERED_PROMPTS_DIR}/${WI_ID}-prompt.rendered.md"
  printf '%s\n' "${rendered_prompt}" > "${rendered_prompt_file}"
  printf '%s\n' "${rendered_prompt_file}"
}

format_cmd() {
  local arg
  for arg in "$@"; do
    printf '%q ' "${arg}"
  done
  printf '\n'
}

SUPPORTS_PROMPT_TEMPLATE=false
SUPPORTS_COMPLETION_PROMISE=false
SUPPORTS_ABORT_PROMISE=false

flag_supported "--prompt-template" && SUPPORTS_PROMPT_TEMPLATE=true
flag_supported "--completion-promise" && SUPPORTS_COMPLETION_PROMISE=true
flag_supported "--abort-promise" && SUPPORTS_ABORT_PROMISE=true

PROMPT_SOURCE_FILE="${PROMPT_FILE}"
USED_TEMPLATE_FALLBACK=false

if [[ "${SUPPORTS_PROMPT_TEMPLATE}" != true ]]; then
  PROMPT_SOURCE_FILE="$(render_prompt_template)"
  USED_TEMPLATE_FALLBACK=true
fi

CMD=(bun "${RALPH_BIN}"
  --agent copilot
  --prompt-file "${PROMPT_SOURCE_FILE}"
  --model "${MODEL}"
  --max-iterations "${MAX_ITER}"
  --verbose-tools
  --allow-all
  --no-commit
)

if [[ "${SUPPORTS_PROMPT_TEMPLATE}" == true ]]; then
  CMD+=(--prompt-template "${PROMPT_TEMPLATE}")
fi

if [[ "${SUPPORTS_COMPLETION_PROMISE}" == true ]]; then
  CMD+=(--completion-promise "${COMPLETION_PROMISE}")
fi

if [[ "${SUPPORTS_ABORT_PROMISE}" == true ]]; then
  CMD+=(--abort-promise "${ABORT_PROMISE}")
fi

FINAL_CMD=("${CMD[@]}")
if [[ "${STREAM}" != true ]]; then
  FINAL_CMD+=(--no-stream)
fi

if [[ "${DRY_RUN}" == true ]]; then
  echo "🧪 Dry run only; Ralph will not be launched."
  echo "   prompt-template support: ${SUPPORTS_PROMPT_TEMPLATE}"
  echo "   completion-promise support: ${SUPPORTS_COMPLETION_PROMISE}"
  echo "   abort-promise support: ${SUPPORTS_ABORT_PROMISE}"
  if [[ "${USED_TEMPLATE_FALLBACK}" == true ]]; then
    echo "   rendered prompt file: ${PROMPT_SOURCE_FILE}"
  fi
  if [[ "${SUPPORTS_ABORT_PROMISE}" != true ]]; then
    echo "   limitation: this Ralph build cannot terminate early on ABORT; blocked/failure flows fall back to normal loop behavior."
  fi
  if [[ "${SUPPORTS_COMPLETION_PROMISE}" != true ]]; then
    echo "   limitation: this Ralph build does not advertise --completion-promise; the wrapper relies on Ralph's default COMPLETE token."
  fi
  if [[ "${STREAM}" == true ]]; then
    echo "   stream mode: enabled"
  else
    echo "   stream mode: buffered (--no-stream)"
  fi
  echo "   command: $(format_cmd "${FINAL_CMD[@]}")"
  exit 0
fi

if [[ "${USED_TEMPLATE_FALLBACK}" == true ]]; then
  echo "⚠️ Ralph does not support --prompt-template; using rendered prompt file ${PROMPT_SOURCE_FILE}"
fi

if [[ "${SUPPORTS_ABORT_PROMISE}" != true ]]; then
  echo "⚠️ Ralph does not support --abort-promise; BLOCKED/FAILURE templates will not terminate early on this build."
fi

if [[ "${SUPPORTS_COMPLETION_PROMISE}" != true ]]; then
  echo "⚠️ Ralph does not support --completion-promise; relying on Ralph's default COMPLETE token."
fi

set +e
if [[ "${STREAM}" == true ]]; then
  echo "🔴 Running ralph (streaming enabled) → ${LOG_FILE}"
  "${FINAL_CMD[@]}" 2>&1 | tee -a "${LOG_FILE}"
else
  echo "⚪ Running ralph (no live stream; --no-stream) → ${LOG_FILE}"
  "${FINAL_CMD[@]}" 2>&1 | tee -a "${LOG_FILE}"
fi
EXIT=$?
set -e

if [[ ${EXIT} -ne 0 ]]; then
  echo "❌ ralph exited with code: ${EXIT}" >&2
fi

exit ${EXIT}
