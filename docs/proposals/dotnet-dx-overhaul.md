# .NET DX Overhaul — Implementation Plan

> **Status:** Approved (pending implementation)
> **Created:** 2026-03-17
> **Context:** Follows DotNetEnv DX comparison analysis and dotnet example refactoring

---

## Goal

Dramatically improve the developer experience of Varlock's .NET integration by:
1. Simplifying the default "happy path" to DotNetEnv-level simplicity
2. Restructuring dotnet examples around single-file apps that each showcase one feature clearly
3. Shipping API improvements (metapackage, static entry point, WebApplicationBuilder extension, etc.)

This plan is split into two tracks that can proceed in parallel:

- **Track A** — Example restructure (the teaching surface)
- **Track B** — API & package improvements (the library surface)

---

## Track A: Example Restructure

### Philosophy

The current examples are full project templates (MVC, Blazor, Worker, etc.) that happen to use Varlock. This buries Varlock's features inside framework boilerplate. The new structure inverts this: **every example exists to showcase a specific Varlock feature**, and uses the simplest possible app shape to do it.

### A.1 — Canonical single-file console app (the "hello world")

**Replace** `examples/dotnet-console/` with a .NET 10 single-file app (no `Main`, no namespace, no class — top-level statements with global usings). This is the simplest possible Varlock integration and serves as the entry point for all documentation.

**Target `Program.cs`** (approximately):

```csharp
using Varlock.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddVarlock();

var app = builder.Build();

var config = app.Services.GetRequiredService<IConfiguration>();
Console.WriteLine($"APP_NAME = {config["APP_NAME"]}");
Console.WriteLine($"HTTP_PORT = {config["HTTP_PORT"]}");
```

**Target `.csproj`** — minimal:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../packages/dotnet/Varlock.Extensions.Configuration/Varlock.Extensions.Configuration.csproj" />
  </ItemGroup>
</Project>
```

**Key points:**
- Uses `AddVarlock()` on IConfigurationBuilder — the recommended default path
- No MSBuild type generation, no Serilog, no custom options — pure defaults
- `.env.schema` uses only basic features (string, coerced number, coerced boolean)
- This is the "convention over configuration" baseline

### A.2 — Sibling single-file apps (one per feature)

Each sibling lives under `examples/dotnet-console-{feature}/` and is a self-contained single-file .NET 10 console app. Every sibling should:

- Be a **single `Program.cs`** (top-level statements, no classes unless the feature requires one)
- Have its own `.env.schema` that demonstrates the specific schema features needed
- Have its own `.csproj` with only the package references that feature requires
- Include a brief `README.md` (2-5 sentences: what this example demonstrates, which packages/features it uses)
- Be buildable and runnable independently (`dotnet run` from its directory)

#### Complete sibling list

Each entry below specifies the feature, the directory name, which packages are referenced, and what the example demonstrates.

| # | Directory | Feature | Packages | Demonstrates |
|---|-----------|---------|----------|--------------|
| 1 | `dotnet-console` | Defaults / happy path | Configuration | `AddVarlock()` with zero config — the baseline |
| 2 | `dotnet-console-direct-load` | Direct CLI bridge (no IConfiguration) | DotNet | `VarlockCliRuntime.Load()` — raw graph access, item iteration, `IsSensitive` checks |
| 3 | `dotnet-console-typed-config` | MSBuild type generation | Configuration, MSBuild | installed/imported MSBuild package or targets act as the opt-in signal, `@generateTypes` in schema, inject `IOptions<VarlockConfig>` |
| 4 | `dotnet-console-sensitive` | Sensitive value handling | Configuration | `@sensitive` decorator, demonstrating that sensitive values are loaded but should be redacted in output |
| 5 | `dotnet-console-serilog` | Serilog redaction | Configuration, Serilog | `WithVarlockRedaction()`, `WithVarlockMetadata()` — log redaction of sensitive values via structured logging |
| 6 | `dotnet-console-reload` | File watching / hot reload | Configuration | `ReloadOnChange = true` on `VarlockConfigurationSource`, demonstrate config changing at runtime |
| 7 | `dotnet-console-custom-schema-path` | Non-default schema location | Configuration | `AddVarlock(source => source.SchemaPath = "config/.env.schema")` — schema in a subdirectory |
| 8 | `dotnet-console-custom-working-dir` | Custom working directory | Configuration | `AddVarlock(source => source.WorkingDirectory = "../shared")` — load schema from a different directory |
| 9 | `dotnet-console-environment-name` | Environment-specific loading | Configuration | `source.EnvironmentName = "production"` — demonstrate multi-environment schema resolution |
| 10 | `dotnet-console-optional` | Optional/graceful degradation | Configuration | `source.Optional = true` — app starts even when schema is missing or varlock CLI unavailable |
| 11 | `dotnet-console-custom-runtime` | Custom IVarlockRuntime | DotNet, Configuration | Implement `IVarlockRuntime` to demonstrate testability / mocking — inject a fake runtime |
| 12 | `dotnet-console-coercion` | Type coercion showcase | Configuration | Schema with `@coerce=number`, `@coerce=boolean`, `@coerce=url`, etc. — show how coerced values flow through IConfiguration |
| 13 | `dotnet-console-validation` | Validation errors | Configuration | Schema with `@required`, `@minLength`, `@pattern`, etc. — show what happens when validation fails (error messages, exit behavior) |
| 14 | `dotnet-console-public-only` | Public-only type generation | Configuration, MSBuild | `@generateTypes(lang="cs", publicOnly=true)` — generate types excluding sensitive values (safe for client-side) |
| 15 | `dotnet-console-exec` | External secret loading | Configuration | `exec()` function in schema (e.g., `@default=exec("echo secret")`) — demonstrate loading secrets from external tools |
| 16 | `dotnet-console-composition` | Value composition / references | Configuration | Schema using `${}` references between variables and `@extends` — demonstrate declarative composition |
| 17 | `dotnet-console-di-options` | IOptions<T> / IOptionsMonitor<T> pattern | Configuration, MSBuild | Full DI pattern: register generated config type, inject `IOptionsMonitor<VarlockConfig>` into a service class |
| 18 | `dotnet-console-explicit-executable` | Explicit CLI path | Configuration | `source.ExecutablePath = "/usr/local/bin/varlock"` — override automatic executable lookup |
| 19 | `dotnet-console-leak-prevention` | Leak prevention | Configuration | Schema with `@preventLeaks=true` — demonstrate the `PreventLeaks` flag on `VarlockResolvedGraph` |

#### Notes on example design

- **Numbering is not ordering.** Examples are peers, not a progression. The baseline (`dotnet-console`) is the starting point; all others branch from it.
- **Each example should be self-documenting.** The `Program.cs` should have brief `// 👈 Varlock:` comments pointing out the Varlock-specific lines (following the existing convention).
- **`.env.schema` files are part of the example.** Each schema should contain only the decorators needed for that feature — don't pile unrelated decorators into one schema.
- **`.env` files** should be committed alongside `.env.schema` with safe example values so `dotnet run` works out of the box (no setup required beyond `npm install` at repo root for the varlock CLI).
- **Error examples** (like `dotnet-console-validation`) should include a README explaining the expected error output, since the app intentionally fails.

### A.3 — Framework-specific examples (keep but simplify)

The existing framework examples remain but are simplified and refocused:

| Directory | Framework | Purpose |
|-----------|-----------|---------|
| `dotnet-aspnet-mvc` | ASP.NET Core MVC | `AddVarlock()` in `WebApplicationBuilder`, plus coexistence with `appsettings.json` and User Secrets |
| `dotnet-blazor-server` | Blazor Server | `AddVarlock()` during server startup, with configuration flowing into components |
| `dotnet-blazor-wasm-public` | Blazor WebAssembly | `publicOnly=true` type generation — sensitive values excluded from client bundle |
| `dotnet-functions-isolated` | Azure Functions (isolated) | `AddVarlock()` on `IConfigurationBuilder` in Functions host |
| `dotnet-worker` | Worker Service | `builder.AddVarlock()` via `HostApplicationBuilder` extension |
| `dotnet-winforms` | Windows Forms | Desktop app scenario — direct `VarlockCliRuntime.Load()` without DI |

**Changes to existing framework examples:**
- Remove any feature demonstrations that are now covered by a console sibling or package test. Framework examples are not the owners for Serilog, typed binding, reload, or other feature lanes unless the framework seam itself is the point.
- Each framework example should focus on **framework-specific integration** only — the "how do I use Varlock with {framework}?" question
- Keep `.env.schema` files minimal — enough to show the integration works, not a kitchen sink

### A.4 — Shared `.env.schema` conventions

Create a shared reference schema at `examples/dotnet-shared/.env.schema.reference` (not a buildable project, just a reference file) that documents only the `@env-spec` decorators and schema patterns exercised by the checked-in `.NET` examples. This isn't an example app and is not part of `bun run proof:dotnet` — it's a bounded cheat sheet kept close to the specimens.

---

## Track B: API & Package Improvements

These are ordered by implementation dependency (earlier items unblock later items).

### B.1 — `WebApplicationBuilder.AddVarlock()` extension

**Package:** `Varlock.Extensions.Hosting`
**What:** Add an extension method on `WebApplicationBuilder` (in addition to the existing `HostApplicationBuilder` extension).

```csharp
public static WebApplicationBuilder AddVarlock(this WebApplicationBuilder builder)
{
    builder.Configuration.AddVarlock();
    return builder;
}

public static WebApplicationBuilder AddVarlock(this WebApplicationBuilder builder, Action<VarlockConfigurationSource> configure)
{
    builder.Configuration.AddVarlock(configure);
    return builder;
}
```

**Why:** ASP.NET Core apps use `WebApplicationBuilder`, not `HostApplicationBuilder`. Currently they must use `builder.Configuration.AddVarlock()` instead of the cleaner `builder.AddVarlock()`. This is a one-file change.

**Dependency:** `Varlock.Extensions.Hosting` will need a reference to `Microsoft.AspNetCore.App` shared framework or a conditional TFM to access `WebApplicationBuilder`. Evaluate whether to multi-target (`netstandard2.0` + `net10.0`) or create a separate package `Varlock.Extensions.AspNetCore`.

### B.2 — Metapackage: `Varlock` (or `Varlock.AspNetCore`)

**What:** A single NuGet meta-package that bundles the common dependencies:
- `Varlock.DotNet`
- `Varlock.Extensions.Configuration`
- `Varlock.Extensions.Hosting`
- `Varlock.MSBuild`

**Excludes:** `Varlock.Serilog` — remains a separate opt-in package for Serilog users. `Varlock.SourceGeneration` — thin wrapper, pulled transitively by MSBuild.

**Why:** A new user should be able to `dotnet add package Varlock` and get everything they need. Individual packages remain available for advanced scenarios (e.g., only wanting the CLI bridge).

**Implementation:** Create a new `.csproj` with only `<PackageReference>` entries to the four dependencies. No source code. Publish as a NuGet package.

### B.3 — Static convenience: `Varlock.Env.Load()`

**Package:** `Varlock.DotNet`
**What:** A static entry point for the simplest possible usage (no DI, no IConfiguration):

```csharp
namespace Varlock.DotNet;

public static class Env
{
    public static VarlockResolvedGraph Load() => Load(new VarlockLoadOptions());

    public static VarlockResolvedGraph Load(VarlockLoadOptions options) =>
        new VarlockCliRuntime().Load(options);

    public static VarlockResolvedGraph Load(Action<VarlockLoadOptions> configure)
    {
        var options = new VarlockLoadOptions();
        configure(options);
        return new VarlockCliRuntime().Load(options);
    }

    public static Task<VarlockResolvedGraph> LoadAsync(CancellationToken cancellationToken = default) =>
        LoadAsync(new VarlockLoadOptions(), cancellationToken);

    public static Task<VarlockResolvedGraph> LoadAsync(VarlockLoadOptions options, CancellationToken cancellationToken = default) =>
        new VarlockCliRuntime().LoadAsync(options, cancellationToken);
}
```

**Why:** `new VarlockCliRuntime().Load(new VarlockLoadOptions())` is two classes the user doesn't care about. `Varlock.DotNet.Env.Load()` is one call. This is the DotNetEnv pattern that works.

### B.4 — DI registration for `IVarlockRuntime`

**Package:** `Varlock.Extensions.Configuration` (or `Varlock.Extensions.Hosting`)
**What:** When `AddVarlock()` is called, also register `IVarlockRuntime` and `VarlockResolvedGraph` in the DI container so services can inject them directly.

```csharp
services.AddSingleton<IVarlockRuntime>(runtime);
services.AddSingleton(resolvedGraph);
```

**Why:** Currently there's no way to access the graph or runtime from DI without wiring it manually. Services that need to check `IsSensitive` or `RedactLogs` have no clean path today.

### B.5 — `[VarlockSensitive]` attribute on generated properties

**Package:** `Varlock.DotNet` (attribute definition), `Varlock.MSBuild` / type generation (emit the attribute)
**What:** The `varlock typegen` command should emit `[VarlockSensitive]` on properties whose schema key is marked `@sensitive`. This enables runtime reflection for redaction, serialization control, etc.

```csharp
// Generated output
public class VarlockConfig
{
    public string AppName { get; set; }

    [VarlockSensitive]
    public string SecretToken { get; set; }
}
```

**Why:** Currently sensitivity metadata lives only in `VarlockConfigMetadata.SensitiveKeys` (a string set). An attribute on the property itself enables ASP.NET model binding sanitization, JSON serialization filtering, and Serilog destructuring without referencing the metadata class.

### B.6 — `AddVarlock<TConfig>()` with automatic `IOptions<T>` binding

**Package:** `Varlock.Extensions.Configuration` or `Varlock.Extensions.Hosting`
**What:** A generic overload that loads varlock config AND binds it to a typed options class:

```csharp
builder.AddVarlock<VarlockConfig>();
// Equivalent to:
// builder.Configuration.AddVarlock();
// builder.Services.Configure<VarlockConfig>(builder.Configuration);
```

**Why:** The generated `VarlockConfig` type + `IOptions<T>` is the recommended pattern, but today the user has to wire it manually. This one-liner eliminates the boilerplate.

**Dependency:** Requires Track A example `dotnet-console-di-options` to showcase the pattern.

### B.7 — Auto-enable MSBuild from `@generateTypes` in schema

**Package:** `Varlock.MSBuild`
**What:** Installing `Varlock.MSBuild` or `Varlock.SourceGeneration` should act as the opt-in signal for CLI-driven C# generation, so projects with `@generateTypes(lang="cs", ...)` no longer need an explicit `<VarlockEnabled>true</VarlockEnabled>` in the `.csproj`.

**Implementation options:**
- (a) An MSBuild target that reads the schema file at evaluation time and sets `VarlockEnabled=true` if `@generateTypes` is found — complex and fragile.
- (b) Keep `VarlockEnabled` but default it to `true` when the MSBuild package is installed (flip the default from `false` to `true` in props). The package being present IS the opt-in signal.
- (c) A dotnet tool or analyzer that warns when `@generateTypes` is in the schema but `VarlockEnabled` is `false`.

**Implemented path:** Option (b). The shipped props now default `VarlockEnabled` to `true`, the typed-config example proves generation without an explicit property, and the packed-package proof in `scripts/test-dotnet-proof.ts` proves a PackageReference consumer generates `obj/Varlock/AppConfig.g.cs` with the package-installed default alone. Explicit `VarlockEnabled=false` remains the escape hatch.

### B.8 — Actionable error messages

**Package:** `Varlock.DotNet` (bridge error handling)
**What:** When the varlock CLI fails (not found, schema parse error, validation failure), the .NET bridge should produce error messages that tell the user exactly what to do.

Current: `VarlockBridgeException: varlock process exited with code 1`
Proposed:
- CLI not found: `Varlock CLI not found. Install it with: npm install --save-dev varlock (searched: node_modules/.bin/varlock, PATH)`
- Schema parse error: `Schema parse error in .env.schema line 5: unknown decorator '@sensetive'. Did you mean '@sensitive'?`
- Validation failure: `Validation failed for SECRET_TOKEN: required value is empty. Set it in .env or via environment variable.`

**Why:** Developer trust erodes when errors are opaque. Every error should answer: what happened, why, and what should I do.

---

## Implementation Sequence

```
Phase 1 — Examples (Track A)
  A.1  Convert dotnet-console to single-file baseline
  A.2  Create first batch of siblings:
       - dotnet-console-direct-load (#2)
       - dotnet-console-typed-config (#3)
       - dotnet-console-sensitive (#4)
       - dotnet-console-serilog (#5)
       - dotnet-console-reload (#6)
  A.2  Create second batch of siblings:
       - dotnet-console-custom-schema-path (#7)
       - dotnet-console-custom-working-dir (#8)
       - dotnet-console-environment-name (#9)
       - dotnet-console-optional (#10)
       - dotnet-console-custom-runtime (#11)
  A.2  Create third batch of siblings:
       - dotnet-console-coercion (#12)
       - dotnet-console-validation (#13)
       - dotnet-console-public-only (#14)
       - dotnet-console-exec (#15)
       - dotnet-console-composition (#16)
       - dotnet-console-di-options (#17)
       - dotnet-console-explicit-executable (#18)
       - dotnet-console-leak-prevention (#19)
  A.3  Simplify existing framework examples
  A.4  Create shared reference schema

Phase 2 — API (Track B, unblocked after Phase 1 baseline exists)
  B.1  WebApplicationBuilder.AddVarlock()
  B.3  Static Varlock.DotNet.Env.Load()
  B.4  DI registration for IVarlockRuntime / VarlockResolvedGraph
  B.5  [VarlockSensitive] attribute on generated properties
  B.2  Metapackage (depends on B.1 existing)
  B.6  AddVarlock<TConfig>() generic overload
  B.7  Auto-enable MSBuild type generation
  B.8  Actionable error messages
```

---

## Current State Reference

### Existing API surface (what the implementer needs to know)

**`VarlockLoadOptions`** (7 configurable properties):
- `SchemaPath` (default: `.env.schema`)
- `WorkingDirectory` (default: `Environment.CurrentDirectory`)
- `EnvironmentName` (nullable — for multi-environment)
- `ExecutablePath` (nullable — override CLI lookup)
- `EnableLocalExecutableLookup` (default: `true`)
- `EnablePathLookup` (default: `true`)
- `EnvironmentVariables` (nullable — custom env vars for CLI subprocess)

**`VarlockConfigurationSource`** (extends `VarlockLoadOptions` surface + adds):
- `Optional` (default: `false` — if `true`, silently fails when CLI unavailable)
- `ReloadOnChange` (default: `false` — file watcher for hot reload)
- `ReloadFailureBehavior` (default: `KeepLastKnownGood`)
- `Runtime` (nullable — custom `IVarlockRuntime` for testing)

**`VarlockResolvedGraph`** (the loaded config):
- `Items`: `IReadOnlyDictionary<string, VarlockResolvedItem>` (key → value + `IsSensitive`)
- `Sources`: `IReadOnlyList<VarlockSourceInfo>` (which files contributed)
- `RedactLogs`: `bool` (schema-level flag)
- `PreventLeaks`: `bool` (schema-level flag)
- `BasePath`: `string?`
- `ContractVersion`: `int?`

**MSBuild properties** (8 configurable):
- `VarlockEnabled` (default: `true` when the MSBuild package is installed; set `false` to disable generation explicitly)
- `VarlockGenerateTypes` (default: `true`)
- `VarlockValidateOnBuild` (reserved, currently no-op)
- `VarlockSchemaPath`, `VarlockWorkingDirectory`, `VarlockGeneratedFile`
- `VarlockExecutablePath`, `VarlockEnableLocalExecutableLookup`, `VarlockEnablePathLookup`

### Key decisions (from prior discussion)
- **Serilog stays separate.** The metapackage does NOT include `Varlock.Serilog`.
- **No Roslyn source generator.** Type generation remains CLI-driven via MSBuild.
- **No native .NET runtime.** The CLI bridge architecture is permanent.
- **netstandard2.0** remains the target for library packages (broad compatibility).

### Package cross-references (in this monorepo, use ProjectReference)
All example `.csproj` files use `ProjectReference` to `../../packages/dotnet/{Package}/{Package}.csproj`. Published packages use normal `PackageReference`. The MSBuild props/targets are imported via `<Import>` in the monorepo; they auto-import when installed as NuGet packages.
