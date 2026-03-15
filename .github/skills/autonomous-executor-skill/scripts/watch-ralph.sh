#!/usr/bin/env bash
set -euo pipefail

ARG="${1:?Usage: watch-ralph.sh <WI-ID|log-path>}"
WORKSPACE_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
LOG="${ARG}"

if [[ ! -f "${LOG}" ]]; then
  if [[ -f "${WORKSPACE_ROOT}/.tmp/ralph/logs/${ARG}.log" ]]; then
    LOG="${WORKSPACE_ROOT}/.tmp/ralph/logs/${ARG}.log"
  elif [[ -f "${WORKSPACE_ROOT}/.tmp/ralph/logs/${ARG}" ]]; then
    LOG="${WORKSPACE_ROOT}/.tmp/ralph/logs/${ARG}"
  fi
fi

if [[ ! -f "${LOG}" ]]; then
  echo "❌ Log file not found: ${ARG}"
  echo "   Expected: .tmp/ralph/logs/${ARG}.log or provide a direct path"
  exit 2
fi

echo "▶ Tailing: ${LOG}"
tail -F "${LOG}"
