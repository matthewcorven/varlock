#!/usr/bin/env bash
#
# setup-ralph.sh — Download and prepare Open Ralph Wiggum for use
#
# Usage:
#   bash setup-ralph.sh <workspace-root>
#
# This script:
#   1. Creates .tmp/ralph/ in the workspace if it doesn't exist
#   2. Clones or updates the open-ralph-wiggum repository into .tmp/ralph/open-ralph-wiggum/
#   3. Runs bun install
#   4. Verifies the ralph CLI entrypoint works
#
set -euo pipefail

WORKSPACE_ROOT="${1:?Usage: setup-ralph.sh <workspace-root>}"
RALPH_DIR="${WORKSPACE_ROOT}/.tmp/ralph"
REPO_DIR="${RALPH_DIR}/open-ralph-wiggum"
ORW_URL="https://github.com/matthewcorven/open-ralph-wiggum.git"
ORW_BRANCH="feat/copilot-cli-agent"
PROMPTS_DIR="${RALPH_DIR}/prompts"

# ─── Pre-flight checks ──────────────────────────────────────────────────────

echo "🔍 Checking prerequisites..."

# Check bun
if ! command -v bun &>/dev/null; then
  echo "❌ bun is not installed."
  echo "   Install it: curl -fsSL https://bun.sh/install | bash"
  echo "   Then re-run this script."
  exit 1
fi
echo "  ✅ bun $(bun --version)"

# Check git
if ! command -v git &>/dev/null; then
  echo "❌ git is not installed."
  exit 1
fi
echo "  ✅ git available"

# Check copilot CLI (warn only — user may want to use a different agent)
if command -v copilot &>/dev/null; then
  echo "  ✅ copilot CLI available"
elif gh copilot --version &>/dev/null 2>&1; then
  echo "  ✅ gh copilot extension available"
else
  echo "  ⚠️  copilot CLI not found on PATH (needed for --agent copilot)"
  echo "     Install: gh extension install github/gh-copilot"
  echo "     Or ensure the standalone 'copilot' binary is on PATH."
  echo "     Continuing setup anyway..."
fi

# ─── Create directories ─────────────────────────────────────────────────────

echo ""
echo "📁 Preparing directories..."

mkdir -p "${RALPH_DIR}"
mkdir -p "${PROMPTS_DIR}"
mkdir -p "${RALPH_DIR}/logs"

# Add .tmp to .gitignore if not already there
GITIGNORE="${WORKSPACE_ROOT}/.gitignore"
if [[ -f "$GITIGNORE" ]]; then
  if ! grep -qxF '.tmp/' "$GITIGNORE" 2>/dev/null; then
    echo '.tmp/' >> "$GITIGNORE"
    echo "  ✅ Added .tmp/ to .gitignore"
  fi
else
  echo '.tmp/' > "$GITIGNORE"
  echo "  ✅ Created .gitignore with .tmp/"
fi

# ─── Clone or update repo ───────────────────────────────────────────────────

echo ""
if [[ -d "${REPO_DIR}/.git" ]]; then
  echo "🔄 Updating existing Ralph clone..."
  cd "${REPO_DIR}"

  # Fetch latest changes for the target branch
  git fetch origin "${ORW_BRANCH}" --quiet 2>/dev/null || {
    echo "  ⚠️  Fetch failed, attempting fresh clone..."
    cd "${RALPH_DIR}"
    rm -rf "${REPO_DIR}"
    git clone --branch "${ORW_BRANCH}" --single-branch --depth 1 "${ORW_URL}" "${REPO_DIR}"
    cd "${REPO_DIR}"
  }

  # Reset to latest
  git checkout "${ORW_BRANCH}" --quiet 2>/dev/null || git checkout -b "${ORW_BRANCH}" "origin/${ORW_BRANCH}" --quiet
  git reset --hard "origin/${ORW_BRANCH}" --quiet 2>/dev/null || true

  echo "  ✅ Updated to latest ${ORW_BRANCH}"
else
  echo "📥 Cloning Ralph from ${ORW_URL} (branch: ${ORW_BRANCH})..."
  git clone --branch "${ORW_BRANCH}" --single-branch --depth 1 "${ORW_URL}" "${REPO_DIR}"
  echo "  ✅ Cloned successfully"
fi

# ─── Install dependencies ───────────────────────────────────────────────────

echo ""
echo "📦 Installing dependencies..."
cd "${REPO_DIR}"
bun install --frozen-lockfile 2>/dev/null || bun install
echo "  ✅ Dependencies installed"

# ─── Verify ralph entrypoint ────────────────────────────────────────────────

echo ""
echo "🧪 Verifying Ralph CLI..."

RALPH_BIN="${REPO_DIR}/bin/ralph.js"
if [[ -f "${RALPH_BIN}" ]]; then
  # Quick sanity check — just run --help
  if bun "${RALPH_BIN}" --help &>/dev/null; then
    echo "  \u2705 Ralph CLI verified (bin/ralph.js --help succeeded)"
  else
    echo "  \u26a0\ufe0f  bin/ralph.js --help exited non-zero (may still work for actual prompts)"
  fi
else
  echo "  ❌ bin/ralph.js not found — repo structure may have changed"
  echo "     Expected entrypoint: ${RALPH_BIN}"
  exit 1
fi

# ─── Summary ────────────────────────────────────────────────────────────────

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  ✅ Ralph is ready!"
echo ""
echo "  Location:     ${REPO_DIR}"
echo "  Entrypoint:   ${RALPH_BIN}"
echo "  Prompts dir:  ${PROMPTS_DIR}"
echo "  Logs dir:     ${RALPH_DIR}/logs"
echo ""
echo "  Run a work item using the helper script:"
echo "    .github/skills/autonomous-executor-skill/scripts/run-ralph.sh <WI-ID> [--model <model>] [--prompt-template <path>] [--max-iterations <N>] [--stream]"
