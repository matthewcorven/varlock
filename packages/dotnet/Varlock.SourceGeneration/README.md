# Varlock.SourceGeneration

Thin wrapper package for Varlock's current C# type-generation story.

Today this package does **not** ship a Roslyn source generator or analyzers. It exists so .NET consumers can depend on a stable source-generation package name while reusing the current `Varlock.MSBuild` integration for deterministic, CLI-driven `.g.cs` generation.

## Install

```xml
<ItemGroup>
  <PackageReference Include="Varlock.SourceGeneration" Version="x.y.z" />
</ItemGroup>
```

## What this package does today

Installing `Varlock.SourceGeneration` brings in `Varlock.MSBuild`, which:

- enables MSBuild type generation by default once the package is present
- runs `varlock typegen` during real builds
- writes generated C# to `obj/Varlock/`
- respects MSBuild incremental inputs and outputs
- lets IDE design-time compile reuse the last successful generated file

## What it does not do yet

- no Roslyn `IIncrementalGenerator`
- no analyzer diagnostics
- no live IDE preview before a real build
- no build-without-CLI fallback

## Current recommendation

Keep `@generateTypes(lang=cs, path=...)` aligned with `VarlockGeneratedFile`, prefer `obj/Varlock/*.g.cs`, and build once before expecting generated types in the IDE. Set `VarlockEnabled=false` only if you need to temporarily disable generation after installing the package.

Future analyzer or source-generator work stays behind an explicit follow-on phase. If user friction justifies that work later, this package name is where it belongs.
