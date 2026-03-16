---
name: "repo-temp-rooting"
description: "Route repo-owned temporary tests and proof artifacts into .tmp without changing runtime semantics."
domain: "build-hygiene"
confidence: "high"
source: "earned"
---

## Context

Use this when a repository is accumulating temporary build, test, or proof artifacts in the working tree or in the wrong location. The goal is to make temp behavior deterministic and local to the repo without smuggling behavior changes into product/runtime code.

## Patterns

- Create shared helpers per runtime to resolve the repo root and create unique directories under `.tmp/`.
- Limit the change to tests, proof harnesses, and repo-owned tooling; do not change product runtime semantics.
- Keep negative-control tests honest. If a test only works when a path is **outside** any git repository, leave that one on OS temp instead of forcing it into `.tmp/`.
- Anchor ignore rules intended for top-level temp folders with a leading `/` so nested directories like `packages/dotnet/` are not accidentally ignored.
- Re-run the exact proof/test surfaces that exercise the moved temp paths, not just unit tests adjacent to the helper.

## Examples

- `packages/utils/src/repo-temp.ts`
- `packages/dotnet/Varlock.DotNet.Tests/TestPaths.cs`
- `scripts/test-dotnet-proof.ts`
- `.gitignore`

## Anti-Patterns

- Reusing `tmpdir()` / `Path.GetTempPath()` for repo-owned artifacts when the repo has a declared `.tmp/` convention.
- Using an unanchored six-character ignore rule that silently hides nested source directories.
- Claiming external Copilot/context-mode workspace behavior is fixed without a documented, validated repo-config knob.
- Forcing every test into `.tmp/` when one of them explicitly depends on *not* being in a git repository.
