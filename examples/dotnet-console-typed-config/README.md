# Typed Configuration Console Example

This example shows the build-time `@generateTypes` path for strongly typed configuration access in C#. It stays scoped to the checked-in MSBuild generation and metadata flow.

## What It Shows

- `@generateTypes(lang="cs", path="...")` decorator in `.env.schema`
- MSBuild integration runs as soon as the package/targets are present; no `VarlockEnabled=true` property is required
- `VarlockConfig` generated class with PascalCase properties and correct C# types
- `VarlockConfigMetadata` with property bindings, key mappings, and sensitive key lists
- Populating the generated type from `IConfiguration` using metadata

## How It Works

During `dotnet build`, the Varlock MSBuild targets invoke `varlock typegen` to generate a `.g.cs` file from the `.env.schema`. This file is compiled into the project automatically, with the installed/imported MSBuild package acting as the opt-in signal.

## When to Use This Pattern

When you want compile-time type safety for configuration values — catching typos and type mismatches at build time rather than runtime.

## Run

```bash
dotnet build   # generates obj/Varlock/VarlockConfig.g.cs
dotnet run
```
