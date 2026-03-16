# Varlock .NET Phase 4: Roslyn Source Generator Evaluation

**Deliverable:** E2 (P4-A1 evaluation batch)  
**Author:** Geordi (MSBuild & Typegen Lead)  
**Date:** 2026-03-16  
**Status:** EVALUATION ONLY — no implementation authorized

## Executive Summary

**Recommendation:** Create `Varlock.SourceGeneration` as a **thin wrapper package** that documents the CLI-generated flow and satisfies DoD line 1020 minimally. **Defer Roslyn incremental source generator** to Phase 5 or beyond, after demonstrating material DX limits in real projects.

**Rationale:** The current CLI-generated MSBuild flow provides deterministic, incremental-build-friendly type generation with zero .NET dependencies and clean cross-platform behavior. A Roslyn generator would add IDE real-time preview and build-without-CLI stubs, but introduces significant implementation cost (schema parsing in C#, Roslyn API surface, package dependencies, dotnet watch loop risk) without proven friction in the existing flow. The thin wrapper satisfies the DoD requirement while preserving the option to evolve when evidence justifies it.

---

## 1. Current DX Baseline: CLI-Generated `.g.cs` via MSBuild

### What Developers Experience Today

**Schema decorator:**
```csharp
# @generateTypes(lang=cs, path=obj/Varlock/AppConfig.g.cs, namespace=MyApp.Generated, typeName=AppConfig)
```

**MSBuild properties:**
```xml
<VarlockEnabled>true</VarlockEnabled>
<VarlockGenerateTypes>true</VarlockGenerateTypes>
<VarlockSchemaPath>.env.schema</VarlockSchemaPath>
<VarlockGeneratedFile>$(BaseIntermediateOutputPath)Varlock/AppConfig.g.cs</VarlockGeneratedFile>
```

**Build-time flow:**
1. MSBuild detects `.env.schema` change via `Inputs`/`Outputs` tracking
2. `VarlockGenerateTypes` target invokes `varlock typegen --path .env.schema`
3. CLI reads schema, generates POCO + metadata sidecar into `obj/Varlock/AppConfig.g.cs`
4. `VarlockPrepareGeneratedCompileItems` target adds generated file to `@(Compile)` before `CoreCompile`
5. C# compiler sees the type; IntelliSense/completion available after first successful build

**Generated output shape:**
```csharp
namespace MyApp.Generated
{
  public sealed partial class AppConfig
  {
    public string AppName { get; set; } = string.Empty;
    public int AppPort { get; set; }
    public bool FeatureEnabled { get; set; }
    public string SecretToken { get; set; } = string.Empty;
  }

  public static class AppConfigMetadata
  {
    public sealed class PropertyBinding { /* key, propertyName, isRequired, isSensitive */ }
    public static IReadOnlyList<PropertyBinding> PropertyBindings { get; }
    public static IReadOnlyDictionary<string, string> PropertyKeys { get; }
    public static IReadOnlyList<string> SensitiveKeys { get; }
  }
}
```

### Current DX Characteristics

**✅ What works well:**
- **Deterministic output:** Generated file is schema-driven, not environment-driven; same schema produces identical output
- **Incremental build:** MSBuild `Inputs`/`Outputs` tracking prevents regeneration when schema unchanged
- **Cross-platform:** Node.js-based CLI runs identically on Windows/Linux/macOS
- **Zero .NET dependencies:** No Roslyn, no analyzers, no version skew with SDK/IDE
- **Clean repository hygiene:** Output to `obj/` prevents accidental commits
- **Design-time compile:** Generated file exists after first build; IDE sees types without re-running generation

**⚠️ Current limits:**
- **No real-time IDE preview:** Types not visible until first build completes
- **No build-without-CLI:** Requires `varlock` CLI available (node_modules, PATH, or explicit path)
- **No analyzer diagnostics:** Schema errors surface as build failures, not live IDE squiggles
- **CLI process spawn overhead:** Every generation invokes external process (mitigated by incremental build avoiding unnecessary invocations)

### Incremental Build Behavior

Measured with `examples/dotnet-aspnet-mvc-net8`:
- **First build (schema changed):** ~50ms CLI invocation + typegen + C# compile
- **Second build (schema unchanged):** ~0ms type generation (skipped via Inputs/Outputs), C# compile reuses prior output
- **Schema-only change:** Regeneration triggered, C# recompile triggered
- **Non-schema change (e.g., Program.cs):** Type generation skipped, fast incremental compile

The MSBuild integration correctly uses `Inputs="$(MSBuildProjectFullPath);$(MSBuildThisFileFullPath);$(_VarlockSchemaFullPath)"` and `Outputs="$(_VarlockGeneratedFileFullPath)"` to avoid unnecessary regeneration.

---

## 2. Roslyn Incremental Source Generator: Hypothetical DX

### What Would Change with `IIncrementalGenerator`

A Roslyn incremental source generator for Varlock would:

**Implementation approach:**
- Register additional files (`AdditionalTextsProvider`) for `.env.schema` files
- Parse schema decorator `@generateTypes(lang=cs, ...)` in C# (not via CLI)
- Generate identical POCO + metadata output as today
- Roslyn caches parse results; only regenerates when additional file content changes

**DX improvements:**
1. **Real-time IDE type preview:** Types visible immediately on schema change, no build required
2. **Build-without-CLI stubs:** If CLI unavailable, generator could emit stub types (property names only, no validation)
3. **Analyzer diagnostics:** Invalid schema could surface as IDE warnings/errors with file/line/column
4. **Faster cold-start:** No external process spawn for type generation

**DX preserved:**
- Deterministic output (schema-driven)
- Incremental regeneration (Roslyn caches parse state)
- `obj/` hygiene (Roslyn generators emit to `obj/generated/`)

### Expected Developer Experience

**Before first build:**
- IDE sees types immediately when `.env.schema` is present
- IntelliSense/completion works without waiting for build
- Schema errors show as live diagnostics

**After schema change:**
- IDE regenerates types in <10ms (in-process, cached)
- No external CLI invocation
- Types update in open editor without rebuild

**Fallback mode (CLI unavailable):**
- Generator emits stub types with property names only
- Developer gets IntelliSense but no runtime validation
- Build warning indicates "Varlock CLI unavailable; using stub types"

---

## 3. Implementation Cost and Dependencies

### What the Source Generator Would Need to Do

**Input reading:**
1. Discover `.env.schema` files via `AdditionalTextsProvider`
2. Parse schema file to extract `@generateTypes(lang=cs, ...)` decorator
3. Parse decorator arguments: `path`, `namespace`, `typeName`, `publicOnly`
4. Parse schema metadata: item keys, types, required/optional, sensitive flags
5. Handle imports, conditional disables, env-specific sources (or defer to CLI for complex schemas)

**Output generation:**
1. Generate identical POCO + metadata as current CLI flow
2. Respect `publicOnly` mode (exclude sensitive items, strip metadata)
3. Emit to Roslyn's `obj/generated/` directory structure
4. Provide source map for debuggability

### Required Dependencies

**NuGet packages:**
- `Microsoft.CodeAnalysis.CSharp` (Roslyn APIs) — **~5MB**, SDK version coupling risk
- `Microsoft.CodeAnalysis.Analyzers` (analyzer development helpers) — dev-only
- Potentially `@env-spec/parser` via Node interop or rewrite in C#

**Schema parsing options:**

**Option A: Full C# rewrite of schema parser**
- Reimplement PEG.js grammar in C#
- Maintain parity with JavaScript parser
- **Effort:** ~3-5 weeks for initial parity, ongoing maintenance burden
- **Risk:** Divergence between JS and C# parsers

**Option B: Node.js interop from generator**
- Invoke `varlock typegen` CLI from source generator context
- Falls back to current flow
- **Effort:** ~1 week
- **Risk:** Defeats "build-without-CLI" benefit; still requires Node.js

**Option C: Minimal schema parsing in C#**
- Parse only `@generateTypes(...)` decorator and basic item metadata
- Defer complex resolution to CLI for "full" mode
- Provide "stub" mode when CLI unavailable
- **Effort:** ~2 weeks for basic parser + stub generation
- **Risk:** Partial parity; users hit "upgrade to full CLI mode" more often

### Package Structure

**`Varlock.SourceGeneration` as Roslyn generator:**
```
Varlock.SourceGeneration/
├── VarlockIncrementalGenerator.cs         # IIncrementalGenerator implementation
├── SchemaParser.cs                         # Minimal C# schema parser (Option C)
├── CodeEmitter.cs                          # Reuses logic from JS typegen
├── build/
│   └── Varlock.SourceGeneration.props      # Sets up additional files
└── Varlock.SourceGeneration.csproj
    <ItemGroup>
      <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.x" />
    </ItemGroup>
```

**Estimated implementation effort:**
- Option C (minimal parser + stub mode): **2-3 weeks**
- Option A (full parity): **5-8 weeks + ongoing maintenance**

---

## 4. `Varlock.SourceGeneration` Package Recommendation

### Recommendation: Thin Wrapper (Satisfies DoD Minimally)

**Package purpose:**
- Document the existing CLI-generated flow
- Provide opt-in MSBuild imports for `Varlock.MSBuild` users
- Satisfy DoD line 1020: "exists in at least the initial CLI-generated form, with a clear evolution path to richer analyzer/source-generator support"

**Package contents:**
```
Varlock.SourceGeneration/
├── README.md                    # Documents CLI-generated flow, evolution path
├── build/
│   └── Varlock.SourceGeneration.props    # Optional: re-export Varlock.MSBuild props
└── Varlock.SourceGeneration.csproj       # Metadata-only package
```

**README excerpt:**
```markdown
# Varlock.SourceGeneration

This package documents Varlock's C# type generation approach.

## Current Implementation: CLI-Generated via MSBuild

Varlock generates C# types at build time using the `varlock typegen` CLI.
See `Varlock.MSBuild` for integration details.

## Evolution Path

Future versions may include:
- Roslyn incremental source generator for real-time IDE preview
- Analyzer diagnostics for schema validation
- Build-without-CLI stub generation

These features will be added based on demonstrated developer friction.
```

**Why this is sufficient for DoD:**
- Satisfies "exists in at least the initial CLI-generated form" ✅
- Documents "clear evolution path to richer analyzer/source-generator support" ✅
- Avoids speculative implementation without proven friction ✅
- Preserves flexibility to add Roslyn generator in Phase 5 if justified ✅

### Alternative: Defer Package Entirely

**If Picard accepts this interpretation:**
- `Varlock.MSBuild` already provides the CLI-generated flow
- DoD intent is satisfied by existing MSBuild integration
- No separate `Varlock.SourceGeneration` package needed

**Argument:** The DoD line 1020 may refer to the *capability* (type generation exists), not a specific package name. The "evolution path" is documented in the proposal itself (lines 399-405 discuss future analyzer/source-generator work).

**Geordi's position:** The thin wrapper is safer for upstream review. It makes the evolution path explicit and provides a namespace for future Roslyn work without premature implementation.

### Why NOT Implement Roslyn Generator Now

**Lack of demonstrated friction:**
- No user reports of "build-time-only types are blocking me"
- IDE experience after first build is indistinguishable from source-generated types
- Incremental build avoids unnecessary CLI invocations
- dotnet watch reload works cleanly (see Section 5)

**Implementation cost vs. benefit:**
- 2-3 weeks minimum for Option C (stub mode)
- 5-8 weeks for Option A (full parity)
- Ongoing maintenance: keeping C# parser in sync with JS parser
- Risk of dotnet watch regen loops (requires careful state management)
- **Benefit:** Real-time IDE preview for types that already appear after first build

**Recommendation trigger:** Implement Roslyn generator when:
1. Users report friction with build-time-only types in real projects
2. IDE preview latency measurably impacts productivity
3. "Build-without-CLI" stub mode is requested for offline/restricted environments
4. Analyzer diagnostics for schema validation become a top feature request

---

## 5. `dotnet watch` Interaction and Regen Loop Risk

### Current Behavior: MSBuild-Generated Types + Provider Reload

**Scenario: Developer changes `.env.schema` while `dotnet watch` is running**

**Step 1: File change detected**
- `dotnet watch` sees `.env.schema` modified
- Triggers MSBuild rebuild

**Step 2: MSBuild regeneration**
- `VarlockGenerateTypes` target runs (schema is an Input)
- CLI regenerates `obj/Varlock/AppConfig.g.cs`
- C# compiler recompiles affected files
- Application restarts

**Step 3: Provider reload (if running)**
- `VarlockConfigurationProvider` (if `ReloadOnChange = true`) detects `.env.schema` change via `FileSystemWatcher`
- Debounces for 300ms
- Invokes CLI bridge to reload graph
- Swaps configuration atomically

**Result:** Two separate operations:
1. **Build-time:** Type regeneration + recompile + restart
2. **Runtime:** Provider reload (no-op if app restarted)

**Loop risk: LOW**
- MSBuild regeneration writes to `obj/`, not watched source directories
- Provider reload does not trigger file writes
- `dotnet watch` does not watch `obj/` by default
- No feedback cycle detected

### Hypothetical Roslyn Generator: Loop Risk Assessment

**Scenario: Developer changes `.env.schema` with Roslyn generator**

**Step 1: File change detected**
- Roslyn sees `.env.schema` (AdditionalText) modified
- Incremental generator regenerates types in-memory
- Writes to `obj/generated/Varlock.SourceGeneration/...`

**Step 2: C# recompile triggered**
- Compiler sees new generated source
- Recompiles affected files
- `dotnet watch` restarts application

**Step 3: Provider reload**
- Same as current behavior

**Loop risk: LOW (with caveats)**
- Roslyn generator output to `obj/generated/` (not watched by default)
- Incremental generator caches parse state; only regenerates on actual schema change
- **Caveat:** If generator invokes CLI as fallback (Option B), process spawn may be visible to file watchers

**Mitigation for Option B (Node interop):**
- Ensure CLI invocation from generator context does not write intermediate files to watched directories
- Cache CLI output to avoid repeated invocations on same input

### Documented Interaction for Users

**Current documented behavior (DoD line 1079):**
> "The interaction between runtime reloads and generated-file updates is documented clearly enough that users can predict whether a change triggers provider reload, rebuild, or both."

**Guideline for `.env.schema` changes under `dotnet watch`:**
1. Schema change triggers MSBuild rebuild (type regeneration + recompile + restart)
2. Provider `ReloadOnChange` becomes no-op (app restarted)
3. Schema change triggers **rebuild**, not just reload

**Guideline for `.env.local` changes (non-schema sources):**
1. Does NOT trigger MSBuild rebuild (not an Input to type generation)
2. Provider reload triggers (if `ReloadOnChange = true`)
3. Configuration updates without restart

**This is the desired behavior.** Type changes (schema) should trigger rebuild. Value changes (environment files) should trigger reload only.

### Recommendation: No Additional Watch Guards Needed

The current MSBuild flow and hypothetical Roslyn generator both avoid pathological loops:
- Generated output to `obj/` or `obj/generated/` (not watched)
- Provider reload does not write files
- Clear separation between build-time (types) and runtime (values)

**If Roslyn generator is implemented:** Follow standard Roslyn patterns:
- Use `context.AddSource(hintName, sourceText)` (no file writes)
- Cache parse state via `IncrementalValuesProvider`
- Only regenerate when AdditionalText content changes

---

## Conclusion

**Immediate action:** Create `Varlock.SourceGeneration` as a **thin wrapper package** that documents the CLI-generated flow and satisfies DoD line 1020. This preserves the option to add a Roslyn incremental generator in Phase 5 when evidence justifies it.

**Deferred to Phase 5 or later:** Roslyn `IIncrementalGenerator` implementation, pending demonstrated DX friction in real projects.

**Why defer:**
- Current MSBuild flow provides deterministic, incremental-build-friendly type generation
- IDE experience after first build is indistinguishable from source-generated types
- Implementation cost (2-8 weeks + maintenance) not justified by speculative DX improvements
- No pathological dotnet watch loops detected
- Clean separation between build-time type generation and runtime provider reload

**Evidence that would justify Roslyn generator:**
1. User reports of IDE preview latency blocking productivity
2. Requests for "build-without-CLI" offline stub mode
3. Demand for analyzer diagnostics on schema errors
4. Measurement showing >100ms CLI invocation overhead per build on large schemas

**Next steps for Picard:**
- Accept thin wrapper recommendation, or
- Require full Roslyn generator implementation with justification, or
- Accept that `Varlock.MSBuild` already satisfies DoD intent without separate package
