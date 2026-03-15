## Varlock.MSBuild

Build-time Varlock integration for SDK-style `.csproj` files.

This package auto-imports its `.props` and `.targets` when added through `PackageReference`; consumers do not need to hand-roll MSBuild imports. The current slice wires deterministic C# type generation into `dotnet build` by invoking the existing `varlock typegen` flow and writing generated output into `obj/Varlock/`.

### Install

```xml
<ItemGroup>
  <PackageReference Include="Varlock.MSBuild" Version="x.y.z" />
</ItemGroup>
```

### Common properties

```xml
<PropertyGroup>
  <VarlockEnabled>true</VarlockEnabled>
  <VarlockSchemaPath>.env.schema</VarlockSchemaPath>
  <VarlockGeneratedFile>$(BaseIntermediateOutputPath)Varlock/AppConfig.g.cs</VarlockGeneratedFile>
</PropertyGroup>
```

- `VarlockEnabled` — opt in to the targets. Defaults to `false`.
- `VarlockSchemaPath` — schema path resolved from `VarlockWorkingDirectory`. Defaults to `.env.schema`.
- `VarlockGeneratedFile` — generated C# path. Defaults to `$(BaseIntermediateOutputPath)Varlock/VarlockConfig.g.cs`.
- `VarlockWorkingDirectory` — working directory for schema resolution and CLI execution. Defaults to `$(MSBuildProjectDirectory)`.
- `VarlockExecutablePath` — explicit Varlock CLI path when you do not want automatic lookup.
- `VarlockEnableLocalExecutableLookup` — enables lookup of local `node_modules/varlock/bin/cli.js`, `node_modules/.bin/varlock`, and the repo-local development layout. Defaults to `true`.
- `VarlockEnablePathLookup` — allows falling back to `varlock` from `PATH` when local lookup does not resolve anything. Defaults to `true`.
- `VarlockGenerateTypes` — enables the C# generation target. Defaults to `true`.
- `VarlockValidateOnBuild` — reserved for follow-on validation work; the current package does not run a separate validation pass.

### Matching the schema decorator

`VarlockGeneratedFile` must match the `@generateTypes(lang=cs, path=...)` output path declared in the schema. The build fails if the target expects one path and the CLI writes somewhere else.

### What this package does not claim yet

- It does not ship the `varlock` executable.
- It does not currently add a separate validation step when `VarlockValidateOnBuild` is set.
- It does not claim `dotnet watch` behavior.
