# Varlock .NET Support Proposal

## Status

- Proposed
- Intended for upstream discussion and incremental implementation
- Initial execution model: CLI bridge to the existing `varlock` engine

## Summary

Varlock should provide first-class .NET support without forcing .NET teams to abandon existing configuration patterns. The primary product stance is coexistence with `appsettings.json` and the standard .NET configuration pipeline, while allowing Varlock to become the authoritative source for env-backed, validated, and secret-aware configuration.

The first shipping implementation should use the existing `varlock` CLI as the source of truth, then layer a native-feeling .NET package surface on top:

- configuration provider integration
- typed access via generated C# and standard options binding
- MSBuild integration
- Serilog-specific redaction support
- example applications proving legacy and modern compatibility

This proposal assumes the long-term native `.NET` parser/runtime question remains open and should be re-evaluated as CLI-bridged support matures.

Initial first-class `.NET` support should mean first-class parity for schema loading, validation, coercion, sensitive metadata, supported logging/redaction workflows, and mainstream `.NET` configuration/hosting ergonomics. It should not be interpreted as immediate parity with every JavaScript-specific runtime injection or framework-specific build integration surface already present elsewhere in the repository.

## Success Model

To keep the proposal reviewable and honest, “first-class” should be evaluated across three separate parity buckets rather than as a single vague promise.

### 1. Engine parity

This is the minimum parity bar for a credible CLI-bridged `.NET` implementation.

It means parity with the existing Varlock engine for:

- schema discovery and loading
- imports and environment-specific source handling
- validation and coercion
- resolver execution
- plugin-backed loading through the CLI bridge
- sensitive/public metadata
- deterministic type-generation inputs derived from schema metadata rather than resolved environment state
- machine-readable source and item metadata exposed through the serialized bridge contract

### 2. `.NET`-native parity

This is the idiomatic `.NET` surface that should feel first-class to application developers.

It means parity for:

- `IConfiguration` integration
- `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>`
- build-time C# generation
- MSBuild integration
- mainstream hosted and non-hosted `.NET` workflows
- supported `.NET` logging and redaction ergonomics

### 3. Explicitly deferred JavaScript-specific parity

The proposal should explicitly not claim parity in v1 for runtime behaviors that are currently tied to JavaScript runtime patching or framework-specific code injection unless they are actually implemented for `.NET`.

By default this deferred bucket includes:

- JavaScript runtime bootstrap injection behavior
- framework-specific build-time injection surfaces comparable to Vite or Next.js
- non-Serilog global process output redaction
- HTTP response interception and leak prevention equivalent to the current JS runtime patches
- any `.NET` equivalent of `varlock run` unless intentionally designed and shipped
- any `.NET` equivalent of `varlock scan` unless intentionally designed and shipped

The proposal should treat these as optional future work, not as implicit outcomes of the bridge.

## Product Positioning

### Default stance

Varlock augments the .NET configuration system.

- `appsettings.json` remains supported
- `appsettings.{Environment}.json` remains supported
- Varlock integrates through `IConfiguration`
- Varlock can participate in `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>`
- Varlock should override appsettings values by default when the same key exists

### Migration stance

Varlock should support two legitimate modes:

1. Coexistence mode
   - keep appsettings for non-secret application defaults and structured config
   - use Varlock for env-backed, secret-aware, validated configuration

2. Varlock-primary mode
   - make Varlock the main source of truth for environment configuration
   - keep appsettings minimal or optional

### Documentation recommendation

Official docs should recommend this split by default:

- AppSettings: non-secret app defaults, structured application config, feature flags, UI defaults
- Varlock: secrets, environment-specific settings, external secret resolution, validated/coerced values, AI-safe schema exposure

## Target Platforms

### Compatibility goals

- Core shared packages should target `netstandard2.0` unless a concrete dependency requires `netstandard2.1`
- Host-specific convenience layers may multi-target newer frameworks where useful

### Likely developer-experience intersections

The support bar should include the mainstream intersections where `.NET` developers actually encounter configuration behavior, not just the top-level app types.

The proposal should explicitly account for:

- `HostApplicationBuilder` and `WebApplicationBuilder`
- ASP.NET Core MVC and minimal API startup flows
- `dotnet run`, `dotnet watch`, and IDE-driven debug launches
- Visual Studio and Rider design-time builds where generated code and build diagnostics must behave predictably
- coexistence with User Secrets during local development
- coexistence guidance for legacy `ConfigurationManager` / `App.config` or `Web.config` migration paths where applicable
- Azure Functions isolated worker local behavior, including how Varlock relates to `local.settings.json`
- Blazor Server and Blazor WebAssembly public-config-only flows

If any of these are intentionally unsupported in v1, that should be stated explicitly rather than left implicit.

### Proven-by-example app types

The first-class support bar is not met until example apps prove the following:

1. Console app
2. ASP.NET Core MVC
3. Worker Service / Generic Host
4. Azure Functions isolated worker
5. Windows Forms on .NET Framework or equivalent legacy Windows target
6. Blazor Server
7. Blazor WebAssembly with public-config-only workflow

### Runtime caveat

Blazor WebAssembly cannot support the same runtime model as server-side apps when using a CLI bridge. First-class support there should mean:

- build-time generation
- non-sensitive public config only
- typed public config surface

## Architecture

### Execution model

The initial `.NET` implementation should bridge to the existing `varlock` CLI.

Reasons:

- preserves one semantic source of truth
- avoids reimplementing parser, validation, resolver, and plugin behavior prematurely
- accelerates delivery of a mergeable, testable package set

The CLI bridge should be an internal implementation detail behind stable `.NET` abstractions.

### Core rule

Keep Varlock semantics internally, expose .NET idioms externally.

Preserve exactly:

- file discovery and precedence
- environment-specific source handling
- validation and coercion semantics
- sensitive/public metadata
- serialized graph contract and machine-readable output semantics used by the bridge
- type-generation determinism from schema metadata rather than resolved environment state
- redaction and leak-prevention settings semantics
- resolver behavior
- plugin behavior exposed through CLI execution
- supported plugin discovery and loading semantics for the chosen executable distribution model

Expose via .NET idioms:

- `IConfiguration`
- `IConfigurationProvider`
- `IOptions<T>`
- `IOptionsSnapshot<T>`
- `IOptionsMonitor<T>`
- source generation
- MSBuild
- Serilog hooks

### CLI bridge contract

The CLI bridge is not just an implementation detail. It is a compatibility contract between the existing Varlock engine and the `.NET` package surface.

Requirements:

- the `.NET` bridge should consume only machine-readable CLI output
- `varlock load --format json-full` should be treated as a stable machine-oriented contract once this support ships
- the serialized payload shape used by `.NET` should be versioned or otherwise evolved intentionally
- human-readable CLI output should not be parsed by `.NET` integrations
- CLI error handling should preserve actionable schema and resolution diagnostics rather than flattening them into generic process failures

Additional v1 requirements:

- the bridge should define stable machine-readable error categories and exit semantics for missing schema, schema errors, resolution errors, plugin loading errors, and executable discovery failures
- machine-readable errors should preserve file, line, and column information where Varlock already has that information available
- the bridge should define how version skew is handled between `.NET` packages and the `varlock` executable
- the bridge should define whether machine-readable errors are emitted by a new flag, a dedicated command, stderr payloads, or another explicit mechanism

Recommended v1 bridge contract details:

- success and failure payloads should each include a bridge contract version field
- machine-readable failures should use explicit categories such as `executable-not-found`, `executable-version-mismatch`, `schema-missing`, `schema-invalid`, `resolution-failed`, `plugin-load-failed`, and `bridge-internal-error`
- machine-readable failures should preserve the original Varlock diagnostic message and include structured location metadata when available
- the `.NET` bridge should treat unknown contract versions as unsupported rather than attempting best-effort parsing
- package-to-executable compatibility should be checked before normal load execution so version mismatches fail fast and predictably

Recommended machine-readable error shape:

```json
{
    "contractVersion": 1,
    "ok": false,
    "category": "schema-invalid",
    "message": "@defaultSensitive must resolve to a boolean value",
    "location": {
        "file": ".env.schema",
        "line": 12,
        "column": 5
    }
}
```

### Executable acquisition and versioning

The proposal should treat executable acquisition as part of the support contract rather than an implementation footnote.

Recommended v1 stance:

1. the `.NET` packages should prefer a version-pinned Varlock executable distributed in a way controlled by the `.NET` package set
2. explicit configuration should allow advanced users to override the executable path
3. opportunistic PATH-based discovery should be treated as an advanced or development-only fallback rather than the primary supported path

Recommended lookup order:

1. explicit application configuration pointing to a specific executable path
2. package-managed executable path resolved from the installed `.NET` package/tooling assets
3. development-only PATH discovery when explicitly enabled or when running in a documented opt-in mode

Recommended versioning behavior:

- the `.NET` package should declare the compatible CLI contract version it expects
- the executable should expose a machine-readable version/contract handshake that can be checked before load operations
- incompatible versions should fail with a dedicated machine-readable category rather than surfacing as generic process errors
- CI guidance should recommend pinning both the `.NET` package version and the executable source in the same repository-controlled workflow

At minimum, the support contract should document:

- how the executable is located
- how package and executable versions are kept compatible
- what happens when the executable is missing
- what happens when the executable is incompatible with the consuming package version
- what deterministic setup looks like in CI and offline/restricted environments

## Package Layout

### 1. `Varlock.DotNet`

Purpose:

- base runtime bridge to the CLI
- shared DTOs and metadata model
- load/resolve orchestration
- public low-level APIs for non-hosted and legacy apps

Target:

- `netstandard2.0`

Candidate surface:

```csharp
public interface IVarlockRuntime
{
    VarlockResolvedGraph Load(VarlockLoadOptions options);
    Task<VarlockResolvedGraph> LoadAsync(VarlockLoadOptions options, CancellationToken cancellationToken = default);
}

public sealed class VarlockResolvedGraph
{
    public IReadOnlyDictionary<string, VarlockResolvedItem> Items { get; }
    public IReadOnlyList<VarlockSourceInfo> Sources { get; }
    public bool RedactLogs { get; }
    public bool PreventLeaks { get; }
}

public sealed class VarlockResolvedItem
{
    public string Key { get; }
    public object? Value { get; }
    public bool IsSensitive { get; }
}
```

### 2. `Varlock.Extensions.Configuration`

Purpose:

- `IConfigurationSource`
- `IConfigurationProvider`
- change token integration
- optional startup and reload behavior

Target:

- `netstandard2.0`

Candidate surface:

```csharp
public sealed class VarlockConfigurationSource : IConfigurationSource
{
    public string SchemaPath { get; set; } = ".env.schema";
    public bool Optional { get; set; }
    public bool ReloadOnChange { get; set; }
    public string? EnvironmentName { get; set; }
    public VarlockReloadFailureBehavior ReloadFailureBehavior { get; set; }
    public VarlockPrecedence Precedence { get; set; } = VarlockPrecedence.OverrideExisting;
}

public static class VarlockConfigurationExtensions
{
    public static IConfigurationBuilder AddVarlock(this IConfigurationBuilder builder);
    public static IConfigurationBuilder AddVarlock(this IConfigurationBuilder builder, Action<VarlockConfigurationSource> configure);
}
```

### 3. `Varlock.Extensions.Hosting`

Purpose:

- convenience helpers for ASP.NET Core and Generic Host
- service registration
- optional status/diagnostics services

Target:

- `netstandard2.0`
- optionally multi-target newer frameworks for convenience overloads

Candidate surface:

```csharp
public static class VarlockHostingExtensions
{
    public static HostApplicationBuilder AddVarlock(this HostApplicationBuilder builder);
    public static HostApplicationBuilder AddVarlock(this HostApplicationBuilder builder, Action<VarlockConfigurationSource> configure);
    public static IServiceCollection AddVarlock(this IServiceCollection services);
}
```

### 4. `Varlock.SourceGeneration`

Purpose:

- generate strongly typed C# from Varlock schema
- optionally add analyzers later

Initial strategy:

- CLI-generated `.g.cs` should ship first
- Roslyn analyzer/source generator can be added after the basic flow is stable

Target:

- `netstandard2.0`

### 5. `Varlock.MSBuild`

Purpose:

- build-time type generation
- optional build-time validation
- incremental build wiring

Approach:

- package-provided `.props` and `.targets`
- invoke `varlock` via packaged tool or managed runtime bridge
- generate into `obj/Varlock/`

### 6. `Varlock.Serilog`

Purpose:

- Serilog-specific redaction helpers
- redaction-aware destructuring and enrichment

Target:

- `netstandard2.0`

### 7. `Varlock.DotNet.Plugins`

Purpose:

- future .NET-native plugin authoring model

Initial scope:

- optional and experimental
- CLI plugin behavior remains the primary supported plugin path in v1

## Configuration Provider Behavior

### Precedence

Recommended default provider ordering:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Varlock
4. command line or explicit application overrides added later

Default behavior:

- Varlock wins when the same key exists in both appsettings and Varlock

### `optional: true`

Recommended semantics:

- if the schema entry point is missing at startup, provider registration succeeds
- if `ReloadOnChange` is `false`, the provider remains empty until next startup
- if `ReloadOnChange` is `true`, the provider should watch for the file to appear later and activate when it does

Behavior matrix:

| optional | reloadOnChange | startup missing schema | later file appears |
| --- | --- | --- | --- |
| false | false | fail startup | n/a |
| false | true | fail startup | n/a |
| true | false | empty provider | no automatic activation |
| true | true | empty provider | activate on successful load |

### `reloadOnChange: true`

Recommended semantics:

- watch the root schema path and all active loaded source files
- debounce file changes briefly
- re-run the CLI bridge load
- update configuration only after a successful reload
- recompute watched files after each successful reload because imports and env-specific sources may change

Recommended v1 operational details:

- debounce should be explicit and documented rather than implied; a default window in the low hundreds of milliseconds is preferred over vague “briefly” language
- only one reload should be active at a time; overlapping file events should be coalesced into a single pending reload
- successful reloads should swap configuration atomically so consumers never observe partially updated state
- change notifications should fire only after the new state is fully committed
- failed reloads should not emit success-style change notifications
- the active watched-file set should be recomputed from the latest successful load, not from failed attempts
- if the root schema disappears after startup, behavior should follow the same last-known-good rules rather than poisoning active configuration

### Reload and watch guarantees

The support contract should define the guarantees, not just the intent.

Minimum v1 guarantees:

1. reads during reload see either the last successful state or the next successful state, never a mixed state
2. `IOptionsMonitor<T>.OnChange(...)` fires at most once per successful committed reload cycle
3. failed reloads preserve the last successful state and surface diagnostics without mutating current values
4. watched files are derived from the last successful active source set and recomputed after each successful reload
5. import graph changes and environment-specific source activation changes are reflected in the next watch-set recomputation

### `dotnet watch` and IDE watch-mode interaction

The proposal should define how Varlock reload behavior and generated-file behavior interact with `.NET` watch workflows.

Recommended v1 stance:

- runtime config reload and MSBuild-driven code generation should be treated as separate mechanisms with separate triggers
- generated files written to `obj/Varlock/` should avoid causing pathological rebuild loops under `dotnet watch`
- supported watch-mode behavior should be proven by example applications rather than assumed from provider semantics alone
- if some watch workflows are slower or less capable in IDE-driven environments, that should be documented explicitly rather than treated as implementation noise

### Reload failure behavior

Recommended default:

- keep last known good configuration
- emit diagnostics and logs
- do not poison active configuration with failed reload results

Suggested enum:

```csharp
public enum VarlockReloadFailureBehavior
{
    KeepLastKnownGood,
}
```

## `IOptions<T>` and Related APIs

### `IOptions<T>`

This should be the primary typed-consumption model for hosted apps.

Supported patterns:

1. User-authored options classes bound from combined configuration
2. Generated Varlock options classes bound from Varlock-backed configuration
3. Mixed usage of both styles in the same application

### `IOptionsSnapshot<T>`

Recommended semantics:

- each new request or scope should receive the latest successfully loaded Varlock-backed configuration
- failed reloads should not invalidate previously working snapshots
- snapshot changes should only become visible after a successful config swap

This makes `IOptionsSnapshot<T>` suitable for ASP.NET Core request-scoped behavior.

### `IOptionsMonitor<T>`

Recommended semantics:

- `CurrentValue` points to the latest successful configuration state
- `OnChange` fires only after a successful reload
- failed reloads do not mutate `CurrentValue`

This makes `IOptionsMonitor<T>` suitable for:

- worker services
- long-lived singleton components
- desktop apps
- logging infrastructure that needs updated metadata

### `ValidateOnStart`

Hosted app guidance should explicitly recommend `ValidateOnStart()` when teams bind user-authored or generated options from Varlock-backed configuration.

## Typed Access

### Primary recommendation

Typed access should not be framed as a replacement for appsettings. The main typed story should be:

- configuration provider integration
- binder-friendly generated C# classes
- standard `IOptions<T>` consumption

### Generated types

The generated C# should prioritize POCO and binder compatibility.

Example generated type:

```csharp
public sealed partial class VarlockConfig
{
    public string AppEnv { get; init; } = "";
    public int ApiPort { get; init; }
    public Uri ApiUrl { get; init; } = null!;

    [VarlockSensitive]
    public string OpenAiApiKey { get; init; } = "";
}
```

### Optional direct access layer

An optional direct access layer is still useful, especially for:

- console apps
- WinForms
- small utilities

But it should be secondary, not the lead product story.

If this layer exists, the supported patterns should be explicit:

- load a resolved graph without `IConfiguration`
- bind generated or user-authored POCOs directly when appropriate
- optionally project values into process environment for utility or legacy scenarios
- support child-process env injection if that becomes part of the supported low-level story

Example convenience API:

```csharp
public static class VarlockEnv
{
    public static VarlockConfig Current { get; }
}
```

## Serilog Support

`Varlock.Serilog` should be part of the first-class plan, not a later nice-to-have.

Recommended responsibilities:

- redaction-aware destructuring helpers
- enrichment with Varlock sensitivity metadata where useful
- helpers for explicit wrapping of sensitive values

Example usage target:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithVarlockMetadata()
    .Destructure.WithVarlockRedaction()
    .WriteTo.Console()
    .CreateLogger();
```

## Security Behavior Scope

The `.NET` plan should define the security boundary explicitly rather than implying that Serilog support alone covers the full current Varlock security story.

At minimum, the proposal should state for v1:

- how `RedactLogs` maps into supported `.NET` logging behavior
- whether any `PreventLeaks` behavior is supported directly in `.NET` runtimes or only surfaced as metadata
- whether there is any supported equivalent to `varlock scan`, or whether repository/file scanning is explicitly deferred
- whether non-Serilog process output redaction is supported, unsupported, or deferred

Unsupported security behaviors should be documented plainly rather than implied by the broader “first-class” label.

## Plugin Support

### v1 guaranteed behavior

Existing Varlock CLI plugin behavior should work through the CLI bridge only for scenarios supported by the underlying `varlock` executable distribution and plugin-loading model used by the `.NET` packages.

The proposal should document:

- supported plugin packaging and discovery modes
- whether single-file and package-based plugins are both supported
- how plugin loading failures surface to users
- that `.NET` v1 does not introduce a second independent plugin runtime

### v1 experimental behavior

Introduce a .NET-native plugin extension point only where it does not imply native feature parity prematurely.

Good candidates:

- custom type mappings
- host integration hooks
- diagnostics extensions
- logging/redaction policy additions

Not a v1 requirement:

- full native resolver or decorator parity separate from the CLI engine

## Type Generation and CLI Surface

The existing `@generateTypes` model should be extended rather than duplicated.

Recommended evolution:

- add `lang=cs`
- support generated output paths that are safe for MSBuild and `obj/`
- keep `ts` and `cs` generation in the same Varlock type-generation flow
- keep generation schema-driven and deterministic rather than environment-driven
- document imported-schema and `auto=false` semantics consistently with the existing type-generation model

Examples:

```bash
varlock typegen --path .env.schema
```

Schema usage:

```bash
# @generateTypes(lang=cs, path=obj/Varlock/VarlockConfig.g.cs)
```

Longer term, a dedicated machine-oriented command may also be useful:

```bash
varlock dotnet emit-config
```

But the first iteration should not require inventing a new CLI surface if `typegen` and `load --format json-full` are sufficient.

## Diagnostics and Inspection

The proposal should treat inspection and troubleshooting as first-class developer-experience requirements, not as incidental consequences of the bridge.

Recommended scope:

- a documented way to inspect resolved config and source metadata for `.NET` scenarios
- preservation of actionable file and location diagnostics from schema and resolution failures
- a clear distinction between human-facing diagnostics and machine-facing bridge output
- examples showing how users debug precedence, imports, missing schema, and reload failures
- a canonical recommendation for what command, API, or MSBuild target a `.NET` user runs first when debugging a failed load
- enough structured information to explain why a value won precedence, which source files were active, and which watched files are currently relevant after reload
- documentation of the machine-readable error categories and sample payloads used by the bridge so maintainers and consumers can debug compatibility issues deterministically

## MSBuild Integration

### Responsibilities

- generate C# during build
- optionally validate schema during build
- use incremental inputs and outputs
- write generated files into `obj/Varlock/`
- surface failures as normal MSBuild diagnostics

### Suggested properties

```xml
<PropertyGroup>
  <VarlockEnabled>true</VarlockEnabled>
  <VarlockSchemaPath>.env.schema</VarlockSchemaPath>
  <VarlockGenerateTypes>true</VarlockGenerateTypes>
  <VarlockValidateOnBuild>true</VarlockValidateOnBuild>
  <VarlockOptional>false</VarlockOptional>
  <VarlockReloadOnChange>true</VarlockReloadOnChange>
  <VarlockGeneratedFile>obj\Varlock\VarlockConfig.g.cs</VarlockGeneratedFile>
</PropertyGroup>
```

### Hygiene requirement

Generated implementation artifacts should go to `obj/Varlock/` or equivalent build output paths. Permanent repo-tracked examples and proposal documents should be the only `.NET` artifacts committed before actual package work begins.

## Example Projects

Example projects are part of the support contract and should be organized predictably.

Recommended layout:

```text
examples/
  dotnet-console-net8/
  dotnet-worker-net8/
  dotnet-aspnet-mvc-net8/
  dotnet-functions-isolated-net8/
  dotnet-winforms-net48/
  dotnet-blazor-server-net8/
  dotnet-blazor-wasm-net8-public/
```

Each example should prove:

- schema discovery
- validation behavior
- typed generated access
- configuration provider integration where applicable
- precedence over appsettings
- `optional` behavior where applicable
- `reloadOnChange` behavior where applicable
- logging/redaction where applicable

## Testing Matrix

The package set should not be considered first-class until it is exercised across:

- Windows
- macOS
- Linux

At minimum:

- `net8.0`
- a legacy Windows desktop target such as `net48`

Recommended validation categories:

1. CLI bridge integration tests
2. configuration provider tests
3. `IOptions<T>` binding tests
4. `IOptionsSnapshot<T>` reload tests
5. `IOptionsMonitor<T>` reload tests
6. Serilog redaction tests
7. example-app smoke tests

## Phased Implementation Plan

### Phase 0: design and repository preparation

- merge this proposal or equivalent upstream design artifact
- define package boundaries and naming
- define stable machine-readable CLI contract for the .NET bridge
- decide how packaged binaries or tools are distributed to .NET consumers

### Phase 1: core bridge and C# generation

- add `lang=cs` support to type generation
- create `Varlock.DotNet`
- create `Varlock.Extensions.Configuration`
- create `Varlock.MSBuild`
- prove with console, ASP.NET MVC, and WinForms examples

### Phase 2: hosted app maturity

- create `Varlock.Extensions.Hosting`
- add worker service support
- add `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` reload tests
- add `optional` and `reloadOnChange` behavior coverage

### Phase 3: logging and wider platform coverage

- create `Varlock.Serilog`
- add Azure Functions isolated example
- add Blazor Server example
- add public-config-only Blazor WebAssembly example

### Phase 4: native evolution and plugin expansion

- re-evaluate native `.NET` parser/runtime cost-benefit
- expand .NET-native plugin capabilities if justified
- consider Roslyn analyzer/source generator enhancements

## Definition of Done

The `.NET` support initiative is only done when the project satisfies all of the following conditions across product design, implementation, documentation, testing, release readiness, and repository hygiene.

### 1. Product and behavior definition is complete

- The official product stance is documented clearly: Varlock coexists with appsettings by default and can become the primary configuration source when a team chooses.
- The default precedence model is documented and implemented: appsettings loads first, Varlock loads after it, and Varlock wins when keys overlap unless users explicitly opt into a different order.
- The supported app-type matrix is documented with explicit notes about full support, partial support, and special constraints.
- The support-boundary model is documented clearly in terms of engine parity, `.NET`-native parity, and explicitly deferred JavaScript-specific parity.
- The likely developer-experience intersections are documented explicitly, including minimal APIs, `dotnet watch`, User Secrets coexistence, IDE-driven builds/debugging, Azure Functions isolated local workflow, and legacy migration guidance where claimed.
- Blazor WebAssembly support is documented honestly as public-config-only while using the CLI bridge.
- The initial CLI-bridge architecture is documented as intentional, not accidental, including the criteria that would justify a future native runtime.
- Unsupported parity gaps versus JavaScript-specific runtime integrations are called out explicitly.
- The machine-readable CLI contract consumed by the `.NET` bridge is documented clearly enough to support long-term maintenance.
- The executable acquisition and version-compatibility story is documented clearly enough for local development, CI, and offline/restricted environments.

### 2. Package surface is complete and coherent

- `Varlock.DotNet` exists and exposes a stable low-level runtime bridge API.
- `Varlock.Extensions.Configuration` exists and integrates with `IConfigurationBuilder` using standard .NET patterns.
- `Varlock.Extensions.Hosting` exists and provides clean host/service registration helpers for ASP.NET Core and Generic Host scenarios.
- `Varlock.SourceGeneration` exists in at least the initial CLI-generated form, with a clear evolution path to richer analyzer/source-generator support.
- `Varlock.MSBuild` exists and provides build integration without requiring users to hand-roll targets.
- `Varlock.Serilog` exists and provides Serilog-specific redaction ergonomics.
- Any `.NET` plugin package introduced is clearly marked supported, preview, or experimental.
- A supported diagnostics or inspection workflow exists for debugging Varlock-backed `.NET` loads.
- Package names, namespaces, versioning, and dependency directions are consistent and defensible for upstream review.

### 3. Configuration provider behavior is fully implemented

- `AddVarlock()` works in standard configuration builder flows.
- `SchemaPath`, `Optional`, `ReloadOnChange`, `EnvironmentName`, precedence settings, and reload failure behavior are implemented and documented.
- The provider performs a successful initial load when the schema is present and valid.
- The provider behaves correctly when the schema is missing and `Optional = true`.
- The provider fails predictably when the schema is missing and `Optional = false`.
- The provider watches the correct active source set when `ReloadOnChange = true`.
- The active watch set is recomputed after successful reloads if imports or environment-specific sources change.
- Reloads are debounced sufficiently to avoid pathological repeated executions during file save bursts.
- Failed reloads do not replace active configuration with invalid or partial data.
- Successful reloads update the provider atomically.
- Configuration consumers see stable, predictable behavior during reload boundaries.
- Provider behavior preserves sensitive metadata and other serialized settings required by the supported `.NET` experience.
- Provider diagnostics include enough source identity to debug precedence and import-related behavior.

### 4. `IOptions<T>` integration is complete

- Standard `IOptions<T>` binding works with user-authored options classes.
- Standard `IOptions<T>` binding works with generated Varlock C# types where applicable.
- `IOptionsSnapshot<T>` reflects the latest successful configuration state per scope/request.
- `IOptionsMonitor<T>` reflects the latest successful configuration state for long-lived consumers.
- `IOptionsMonitor<T>.OnChange(...)` fires only after successful reloads.
- Failed reloads do not corrupt or regress `CurrentValue` for `IOptionsMonitor<T>`.
- Official examples demonstrate recommended usage for `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>`.
- Guidance explicitly covers when to use `ValidateOnStart()`.

### 5. Type generation is complete enough for first-class support

- Varlock supports generating C# types through the existing type-generation flow.
- `@generateTypes(lang=cs, ...)` is implemented and documented.
- Generated C# is valid for supported target frameworks.
- Generated C# favors binder-friendly POCOs and clean interop with standard .NET patterns.
- Sensitive and non-sensitive metadata is preserved where intended by the design.
- Generated type naming rules are documented and consistently applied.
- Generated output paths are safe for MSBuild and repository hygiene.
- Generated code lands in build output directories for normal project flows unless the user explicitly chooses another path.
- C# generation preserves the existing deterministic schema-driven model rather than depending on resolved environment values.
- Imported-schema behavior and `auto=false` behavior are documented and tested consistently with the underlying Varlock type-generation model.

### 6. MSBuild integration is complete

- A supported MSBuild package exists.
- It can generate C# types automatically during build.
- It can validate schema during build when enabled.
- It uses incremental inputs and outputs correctly enough to avoid needless rebuild churn.
- Build failures are surfaced as normal MSBuild diagnostics with actionable messages.
- The integration works in standard command-line builds.
- The integration works in IDE-driven builds to the extent required for mainstream .NET workflows.
- The integration behaves predictably under `dotnet watch` and does not create pathological rebuild or regeneration loops in supported scenarios.
- The interaction between runtime reloads and generated-file updates is documented clearly enough that users can predict whether a change triggers provider reload, rebuild, or both.
- The integration does not require users to manually edit temporary generated files.
- Generated artifacts are not committed accidentally as part of the recommended workflow.

### 7. Logging and redaction support is complete

- Serilog integration exists as a supported package.
- Sensitive Varlock values can be redacted in Serilog output using documented APIs.
- The Serilog package provides an ergonomic story for both automatic and explicit redaction.
- Example applications demonstrate Serilog integration in at least one hosted app and one non-hosted app where appropriate.
- The behavior of redaction around reloads is defined and tested where metadata or active sensitive values change.
- The status of non-Serilog redaction and leak-prevention behavior is documented explicitly as supported, unsupported, or deferred.
- If `PreventLeaks` metadata is exposed through the bridge, its supported `.NET` meaning is documented and tested where applicable.
- If repository/file scanning is not part of v1, that deferral is documented plainly.

### 8. Plugin behavior is clearly supported

- Existing Varlock CLI plugin behavior works through the CLI bridge for supported scenarios.
- Documentation explains what “plugin support” means in the CLI-bridge model.
- If any .NET-native plugin hooks are introduced, they are documented with explicit scope boundaries.
- No experimental `.NET` plugin mechanism is presented as equivalent to full native Varlock engine parity unless it truly is.
- Supported plugin packaging and discovery modes are documented.
- Plugin loading failures are surfaced with actionable diagnostics.

### 9. Example applications prove the support claims

- Minimal example projects exist for the agreed support matrix.
- Each example is runnable without local, undocumented hand steps.
- Each example is intentionally small and focused on proving specific behavior.
- Console, ASP.NET Core MVC, and WinForms examples exist and are working.
- Worker Service, Azure Functions isolated worker, Blazor Server, and Blazor WebAssembly public-config examples exist and are working before the initiative is called complete.
- Example apps demonstrate precedence with appsettings.
- Example apps demonstrate coexistence with other common `.NET` configuration layers where the docs claim coexistence, such as User Secrets or `local.settings.json` in the relevant app types.
- Example apps demonstrate typed access.
- Example apps demonstrate validation behavior.
- Example apps demonstrate `Optional` behavior where relevant.
- Example apps demonstrate `ReloadOnChange` behavior where relevant.
- At least one example demonstrates direct non-hosted loading without `IConfiguration` if that scenario is claimed as supported.
- At least one example demonstrates plugin-backed secret resolution if plugin support is claimed in user-facing docs.
- Example apps do not include unnecessary fork-only scaffolding or unfinished exploratory code.

### 10. Test coverage spans the entire support contract

- Automated tests cover the CLI bridge load path.
- Automated tests cover configuration provider startup behavior.
- Automated tests cover `Optional = true` and `Optional = false` semantics.
- Automated tests cover `ReloadOnChange = true` success and failure flows.
- Automated tests cover atomic reload semantics.
- Automated tests cover `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` behavior.
- Automated tests cover generated C# output validity and representative schema shapes.
- Automated tests cover MSBuild integration sufficiently to catch regressions in generation and validation flows.
- Automated tests cover machine-readable error contract behavior in addition to success payload behavior.
- Automated tests cover executable discovery, override-path handling, and executable version-mismatch failures.
- Automated tests cover watch/reload coalescing and last-known-good guarantees under repeated file changes.
- Automated tests cover Serilog redaction behavior.
- Example-app smoke tests exist and run in CI for the supported matrix that is claimed in docs.
- Automated tests cover the machine-readable CLI contract consumed by the `.NET` bridge.
- Automated tests cover plugin-backed load scenarios if plugin support is claimed.
- Automated tests cover representative schema and resolution diagnostics.
- Automated tests cover imported-schema and `auto=false` type-generation semantics.

### 11. Cross-platform support claims are proven in CI

- CI validates supported `.NET` examples and packages on Windows.
- CI validates supported `.NET` examples and packages on Linux.
- CI validates supported `.NET` examples and packages on macOS where applicable.
- CI covers at least one modern target such as `net8.0`.
- CI covers at least one legacy Windows desktop target such as `net48` if that remains part of the support claim.
- CI failures are actionable and attributable to the `.NET` support work rather than unowned ad hoc scripts.

### 12. Documentation is complete and publishable

- User-facing docs explain the `.NET` story without ambiguity.
- Docs explain the coexistence model with appsettings and when to prefer each layer.
- Docs explain default precedence and how to change it if customization is supported.
- Docs explain `Optional`, `ReloadOnChange`, and reload failure semantics.
- Docs explain `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` usage.
- Docs explain how C# type generation works.
- Docs explain how MSBuild integration works.
- Docs explain Serilog integration.
- Docs explain the machine-readable CLI bridge contract and supported inspection workflows.
- Docs explain what current Varlock security behaviors are included, deferred, or unsupported in `.NET` v1.
- Docs explain what plugin support means operationally for `.NET` consumers.
- Docs explain platform caveats, especially Blazor WebAssembly.
- Docs include migration guidance for teams already using appsettings, DotEnv, or `ConfigurationManager.AppSettings`.
- Docs are organized in stable locations suitable for upstream merge and future maintenance.

### 13. Developer experience is acceptable

- A new `.NET` user can get a working example running with documented steps only.
- A user can add Varlock to an existing appsettings-based app without having to abandon existing configuration classes immediately.
- A user can understand how Varlock should coexist with User Secrets, `local.settings.json`, and legacy configuration layers in the scenarios claimed by docs.
- Errors during load, validation, and MSBuild generation are understandable and actionable.
- IDE and build outputs point users toward the correct schema or configuration problems.
- The required setup for the CLI bridge is explicit and not surprising.
- The support story does not rely on users discovering hidden environment assumptions.
- The distinction between provider-based usage, direct runtime usage, and build-time generation usage is documented clearly.
- The supported debugging path for failed loads is obvious from docs and error messages.

### 14. Distribution and release story is ready

- Each `.NET` package has a clear publishing strategy.
- The project implements and validates a supported story for how the `.NET` packages locate or obtain the `varlock` executable.
- Package metadata, README files, and versioning are ready for public consumption.
- Release steps are documented well enough that upstream maintainers can ship the packages without fork-specific tribal knowledge.
- Any new CI or release workflow changes are minimal, reviewable, and aligned with the existing monorepo release model.

### 15. Repository hygiene and upstream mergeability are preserved

- Design documents live under `docs/proposals/` or another agreed permanent documentation location.
- Temporary notes, scratch code, or fork-only artifacts are not committed.
- Example projects are added only when intentionally named, runnable, and scoped.
- Generated artifacts are excluded from source control where appropriate.
- Directory layout for `.NET` work is consistent with the rest of the monorepo.
- No package, example, or workflow names are fork-specific.
- The resulting diff set can be reviewed and merged upstream without requiring a cleanup PR first.

### 16. Upstream readiness is explicit

- The final set of proposals, package boundaries, and behavior decisions is understandable without requiring chat history.
- Open questions are either resolved or intentionally deferred with clear rationale.
- Unsupported scenarios are called out plainly rather than implied.
- The implementation can be delivered incrementally in reviewable PRs without invalidating the high-level design.
- Maintainers can tell from the repository itself, not just external discussion, what “done” means for `.NET` support.

## Acceptance Criteria for First-Class Support

Varlock should claim first-class `.NET` support only when all of the following are true:

1. the proposal clearly distinguishes engine parity, `.NET`-native supported parity, and explicitly deferred JavaScript-specific parity
2. documented configuration provider integration exists
3. documented `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` behavior exists
4. `optional` and `reloadOnChange` behavior is implemented and tested
5. a stable machine-readable CLI contract for the `.NET` bridge exists and is documented
6. a documented executable acquisition and compatibility story exists for local development and CI
7. C# type generation exists, is documented, and preserves the deterministic schema-driven model
8. MSBuild integration exists and is documented
9. supported logging/redaction behavior exists and is documented
10. plugin-backed loading behavior is documented and tested for claimed supported scenarios
11. diagnostics and inspection workflows exist for supported `.NET` usage modes
12. example apps prove hosted and non-hosted scenarios plus claimed security and plugin behaviors
13. CI validates the supported platform matrix
14. unsupported parity gaps versus JavaScript-specific runtime integrations are explicitly documented
15. the claimed `.NET` developer-experience intersections are documented and proven for the workflows that are labeled supported

## Open Questions To Resolve During Implementation

1. How should the `.NET` packages obtain the `varlock` executable in the user environment?
    - preferred answer for v1: package-managed executable with explicit override path and documented opt-in PATH fallback

2. Should generated C# names always be PascalCase transforms of env keys while preserving original key metadata?

3. What is the minimum supported legacy Windows target to prove first-class support credibly?

4. Should a dedicated `.NET`-focused CLI subcommand be added after the initial bridge is working, or should the existing `load` and `typegen` commands remain the only contract?

5. What is the supported versioning strategy for the machine-readable CLI contract consumed by the `.NET` bridge?
    - preferred answer for v1: explicit contract version handshake checked before normal load operations

6. Which current Varlock security behaviors are part of first-class `.NET` support: logging redaction only, leak prevention, scanning, or some subset?

7. Which plugin packaging and discovery modes are supported for `.NET` consumers?

8. Is child-process environment injection part of the supported low-level `.NET` story, or explicitly out of scope for v1?

9. What inspection or debugging command/API is the canonical `.NET` troubleshooting path?

## Remaining Gaps To 9.5

This proposal is intentionally trying to be strong enough that implementation success can later be evaluated at or above a 9.5/10 confidence level. The remaining credibility blockers should be treated as implementation-priority items rather than editorial footnotes.

1. Finalize executable acquisition and compatibility behavior in a way that works predictably for local development, CI, and offline environments.
2. Finalize a machine-readable failure contract with explicit categories, versioning, and location metadata rules.
3. Finalize reload/watch semantics tightly enough that `dotnet watch`, IDE builds, and long-lived hosted apps have predictable behavior under repeated changes.

If these three areas remain vague, the proposal may still be directionally correct but should not be treated as a 9.5/10 confidence design for delivery.

## Recommended v1 Defaults

Unless implementation constraints force a different decision, the proposal should bias toward the following defaults because they maximize the chance of reaching a credible first-class support bar quickly.

1. Use the CLI bridge as the only supported semantic engine in v1.
2. Treat engine parity as mandatory and JavaScript-runtime parity as explicitly deferred unless implemented.
3. Prefer a version-pinned executable acquisition model controlled by the `.NET` package set.
4. Prefer provider-based and options-based application integration over direct static-access APIs.
5. Treat Serilog as the only logging stack with first-class redaction ergonomics in v1 unless broader support is intentionally implemented.
6. Treat repository/file scanning and process-wrapper behavior as out of scope for first-class `.NET` parity unless explicitly added and documented.
7. Keep generated C# schema-driven and deterministic in the same way existing TypeScript generation works today.
8. Require every user-facing support claim to be proven by an example app or automated test before the initiative is called complete.

## Repository Hygiene Notes

Because this work is being developed in a fork with intent to merge upstream, repository hygiene should stay strict:

- keep design artifacts centralized under `docs/proposals/`
- avoid committing temporary scratch notes or exploratory example fragments outside their final paths
- place generated files in build output directories, not tracked source paths
- only add example projects once they are runnable, minimal, and intentionally named
- avoid fork-specific scaffolding that would need to be deleted before opening a pull request
