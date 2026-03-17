# Typed Config Console Example

Demonstrates build-time type generation via `@generateTypes` — strongly-typed access to configuration values.

## What It Shows

- `@generateTypes(lang="cs", path="...")` decorator in `.env.schema`
- `VarlockEnabled=true` in the `.csproj` to enable MSBuild integration
- `VarlockConfig` generated class with PascalCase properties and correct C# types
- `VarlockConfigMetadata` with property bindings, key mappings, and sensitive key lists
- Populating the generated type from `IConfiguration` using metadata

## How It Works

During `dotnet build`, the Varlock MSBuild targets invoke `varlock typegen` to generate a `.g.cs` file from the `.env.schema`. This file is compiled into the project automatically.

## When to Use This Pattern

When you want compile-time type safety for configuration values — catching typos and type mismatches at build time rather than runtime.

## Run

```bash
dotnet build   # generates obj/Varlock/VarlockConfig.g.cs
dotnet run
```
