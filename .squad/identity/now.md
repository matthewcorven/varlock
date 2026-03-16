---
updated_at: 2026-03-16T10:12:30.000Z
focus_area: P4-B1 approved-close; product artifacts pending commit
active_issues: []
---

# What We're Focused On

**P4-B1 is approved-close.** Wave 1 and Wave 2 deliverables are complete and reviewer-cleared. The immediate next step is to commit the product artifacts as a single batch.

## Accepted in this batch

- Wave 1 docs: getting-started, configuration, typed-options, watch-and-ide, security-and-logging, migration, troubleshooting, plugin-scope
- Product item: thin `Varlock.SourceGeneration` wrapper
- Wave 2 docs: `type-generation.mdx` and the distribution / release guide
- Package README expansion: `Varlock.DotNet`, `Varlock.Extensions.Configuration`, `Varlock.Extensions.Hosting`, `Varlock.MSBuild`, `Varlock.Serilog`; existing `Varlock.SourceGeneration` README remains acceptable in scope
- O'Brien's Wave 2 slice was reviewer-cleared after a small Data-led editorial correction pass

## Remaining immediate step

- Commit all P4-B1 product artifacts as a single batch
- Optional follow-on editorial cleanup:
  - `packages/varlock-website/src/content/docs/integrations/dotnet/migration.mdx` uses `output=` instead of canonical `path=`
  - `packages/varlock-website/src/content/docs/integrations/dotnet/migration.mdx` shows `src/Generated/` instead of recommended `obj/Varlock/`
  - `packages/varlock-website/src/content/docs/integrations/dotnet/getting-started.mdx` links to `/integrations/dotnet/offline/`

## Still deferred

- Native `.NET` runtime implementation — **NO-GO**
- Full Roslyn source-generator implementation — **NO-GO**
- `.NET`-native plugin expansion — **NO-GO**
- `VarlockValidateOnBuild`, `varlock run` parity, and new framework examples remain out of this batch
