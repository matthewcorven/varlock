---
updated_at: 2026-03-16T11:19:33.000Z
focus_area: P4-B1 COMPLETE; Phase 4 documentation/wrapper scope complete
active_issues: []
---

# What We're Focused On

**P4-B1 is COMMITTED.** All Wave 1 and Wave 2 deliverables have been landed in product commit `101ebde` with editorial cleanup applied. Phase 4 documentation and thin-wrapper scope is complete.

## Completed P4-B1 Batch

- Wave 1 docs (8 items): getting-started, configuration, typed-options, watch-and-ide, security-and-logging, migration, troubleshooting, plugin-scope
- Product item: thin `Varlock.SourceGeneration` wrapper (MSBuild delegation, no Roslyn)
- Wave 2 docs (2 items): `type-generation.mdx` and `distribution.mdx` 
- Package README expansion (5 items): `Varlock.DotNet`, `Varlock.Extensions.Configuration`, `Varlock.Extensions.Hosting`, `Varlock.MSBuild`, `Varlock.Serilog`
- Editorial cleanup: migration.mdx path alignment (output= → path=, src/Generated/ → obj/Varlock/), getting-started.mdx dead-link fix

## Product Coherence Achieved

- **Canonical output location:** All docs now specify `obj/Varlock/` as the recommended intermediate output path
- **Canonical parameter form:** All examples use `path=` parameter in `@generateTypes` decorator
- **Unified message:** CLI @generateTypes and MSBuild VarlockGeneratedFile reference the same physical output file
- **Honest scope:** Thin wrapper, MSBuild delegation, no Roslyn implementation, explicit deferred-work callouts

## Scope Boundaries Maintained

- ✓ Thin source-generation wrapper (no Roslyn, no analyzers)
- ✓ Build-backed type generation with external CLI
- ✓ Documentation coherence (Wave 1 + Wave 2)
- ✓ Package README clarity
- ✗ Native .NET runtime — **NO-GO**
- ✗ Full Roslyn source-generator — **NO-GO**
- ✗ .NET-native plugin expansion — **NO-GO**
- ✗ Watch-mode IDE integration, VarlockValidateOnBuild, varlock run parity — deferred
