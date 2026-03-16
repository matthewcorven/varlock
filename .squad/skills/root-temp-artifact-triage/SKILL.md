---
name: "root-temp-artifact-triage"
description: "Classify suspicious repo-root folders as disposable temp output, active repro state, or tracked cleanup work"
domain: "repo-hygiene"
confidence: "high"
source: "observed"
---

# SKILL: Repo-root temp artifact triage

## Context

Use this when a repository root contains suspicious folders that look like temporary output, repro harnesses, caches, or leaked tool state and you need to decide whether deletion is safe.

This applies especially well when folder names are opaque (`pDcIK9/`, `0SGZKr/`), when cache-like names appear (`NuGetScratch/`, `node-compile-cache/`, `MSBuildTemp/`), or when a user wants a deletion verdict without breaking build or runtime behavior.

## Patterns

1. **Classify by Git first**
   - Run `git ls-files -- <dir>` and `git status --ignored --untracked-files=all -- <dir>`.
   - If Git tracks it, deletion is a repository change even if the directory is “just cache.”
   - If it is empty and untracked, it is usually safe local trash unless a live process still owns it.

2. **Fingerprint the folder contents**
   - Empty mixed-case six-character folders usually indicate temp-directory leakage.
   - Zero-byte lock files suggest scratch/restore state.
   - Hashed blobs under a versioned subfolder usually indicate a compile cache.
   - Tiny self-contained `app/.env.schema` + `packages/.../cli.js` trees usually indicate an ad-hoc repro harness.

3. **Search code for supported ownership**
   - Search for the folder name and for temp-directory APIs (`Path.GetTempPath()`, `tmpdir()`, `mkdtempSync`, `Directory.Delete(..., recursive: true)`, cleanup in `finally`).
   - If the repo’s proof/test code already promises system-temp usage and cleanup, root-level leftovers are probably regressions, not dependencies.

4. **Check live-use risk separately from dependency risk**
   - Use `lsof +D <dir>` to see whether any process has open files there.
   - Active IDE or language-service processes do not matter unless they hold those exact paths.
   - No open handles means deletion risk is about repo hygiene, not runtime liveness.

5. **Write a three-part verdict**
   - **Safe local trash:** empty untracked temp folders, empty `MSBuildTemp/`, dead repro folders.
   - **Cleanup-commit material:** tracked caches or scratch directories with no code references.
   - **Do not touch blindly:** real source trees and broad glob targets (`packages/`, `scripts/`, `docs/`, `examples/`, `smoke-tests/`, `node_modules/`).

## Examples

- `pDcIK9/` and similar six-character root folders: empty + untracked + no references ⇒ safe to remove.
- `varlock dotnet tests repro .../`: untracked repro harness matching a bridge-alignment test payload ⇒ safe once the repro is no longer in use.
- `NuGetScratch/` and `node-compile-cache/`: no runtime dependency, but tracked in Git ⇒ remove only through an intentional cleanup commit.

## Anti-Patterns

- **Do not assume “cache” means safe** — if Git tracks it, it is not ordinary local trash.
- **Do not bulk-delete by broad glob from repo root** — explicit allowlists are safer than pattern guesses.
- **Do not claim a precise creator without evidence** — “looks like temp leakage” is honest; “created by X” requires proof.
