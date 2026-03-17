---
name: "cli-surface-compat-wrapper"
description: "Probe a CLI's advertised flags and emulate wrapper-owned behavior when the installed binary is older than the docs"
domain: "tooling-compatibility"
confidence: "high"
source: "2026-03-17 Ralph wrapper compatibility fix"
---

# SKILL: Wrapper-owned CLI surface compatibility

Use this when a repo helper script promises a richer CLI surface than the currently installed tool actually supports.

## Pattern

1. Detect capability from the real binary, not from docs.
2. Treat wrapper inputs and binary flags as different contracts.
3. Pre-render or translate wrapper-owned inputs when the binary lacks the richer flag.
4. Omit unsupported flags instead of forcing hard failure.
5. Add a dry-run mode that validates detection, rendering, and final command shape without launching the full workflow.
6. Document residual limitations honestly when a fallback cannot preserve full behavior.

## Why

This keeps the repo workflow stable across mixed tool versions while making the downgrade path explicit and reviewable.