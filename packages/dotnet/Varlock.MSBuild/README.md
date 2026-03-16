## Varlock.MSBuild

Build-time Varlock integration for SDK-style `.csproj` files. Automatically generates type-safe C# configuration classes from your Varlock schema during `dotnet build`.

This package auto-imports its `.props` and `.targets` when added through `PackageReference`; consumers do not need to hand-roll MSBuild imports.

## Installation

```bash
dotnet add package Varlock.MSBuild
```

Or manually in your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Varlock.MSBuild" Version="x.y.z" />
</ItemGroup>
```

## Basic setup

Create a `.env.schema` file in your project root with the `@generateTypes` decorator:

```javascript
/**
 * @env-spec
 * @defaultSensitive false
 */

// @generateTypes(lang=cs, path=obj/Varlock/AppConfig.g.cs)

APP_NAME = @string @default("my-app")
APP_PORT = @integer @default(5000)
DATABASE_URL = @string @required
```

Enable the MSBuild package in your `.csproj`:

```xml
<PropertyGroup>
  <VarlockEnabled>true</VarlockEnabled>
  <VarlockSchemaPath>.env.schema</VarlockSchemaPath>
</PropertyGroup>
```

Now when you run `dotnet build`, Varlock generates `AppConfig.g.cs` automatically.

## How it works

1. **Parse:** MSBuild reads your schema file during the build
2. **Validate:** Varlock validates the schema syntax and environment values
3. **Generate:** C# types are written to `obj/Varlock/` (or your custom output path)
4. **Compile:** Your project compiles with the generated types available for use

The generated file is placed in the `obj/` directory, so it's built but not committed to source control.

## Configuration properties

Control Varlock's MSBuild behavior with these properties in your `.csproj`:

```xml
<PropertyGroup>
  <!-- Enable or disable Varlock targets. Defaults to false for opt-in -->
  <VarlockEnabled>true</VarlockEnabled>
  
  <!-- Path to your schema file (relative to VarlockWorkingDirectory) -->
  <VarlockSchemaPath>.env.schema</VarlockSchemaPath>
  
  <!-- Output path for generated C#. Defaults to $(BaseIntermediateOutputPath)Varlock/VarlockConfig.g.cs -->
  <VarlockGeneratedFile>$(BaseIntermediateOutputPath)Varlock/AppConfig.g.cs</VarlockGeneratedFile>
  
  <!-- Working directory for schema resolution and CLI execution. Defaults to project directory -->
  <VarlockWorkingDirectory>$(MSBuildProjectDirectory)</VarlockWorkingDirectory>
  
  <!-- Explicit path to varlock CLI executable (optional, overrides automatic lookup) -->
  <VarlockExecutablePath>/usr/local/bin/varlock</VarlockExecutablePath>
  
  <!-- Enable lookup in node_modules and local development paths. Defaults to true -->
  <VarlockEnableLocalExecutableLookup>true</VarlockEnableLocalExecutableLookup>
  
  <!-- Allow fallback to varlock from system PATH. Defaults to true -->
  <VarlockEnablePathLookup>true</VarlockEnablePathLookup>
  
  <!-- Generate C# types during build. Defaults to true -->
  <VarlockGenerateTypes>true</VarlockGenerateTypes>
  
  <!-- Reserved for future validation work. Currently has no effect -->
  <VarlockValidateOnBuild>false</VarlockValidateOnBuild>
</PropertyGroup>
```

## Schema synchronization

Your `.env.schema` file **must** declare the output path in the `@generateTypes` comment:

```javascript
// @generateTypes(lang=cs, path=obj/Varlock/AppConfig.g.cs)
```

This path must match your `VarlockGeneratedFile` property. The build fails if there's a mismatch.

## Using generated types

Once generated, use the types in your application:

```csharp
// Inject IOptions<T> with the generated type
public class MyService
{
    private readonly IOptionsMonitor<AppConfig> _options;
    
    public MyService(IOptionsMonitor<AppConfig> options)
    {
        _options = options;
    }
    
    public void PrintConfig()
    {
        var config = _options.CurrentValue;
        Console.WriteLine($"App: {config.AppName}");
        Console.WriteLine($"Port: {config.AppPort}");
    }
}
```

See [Typed access](/integrations/dotnet/typed-options/) for detailed examples and patterns.

## Incremental builds

The generated C# file is part of the MSBuild cache:

- If schema and environment haven't changed, the target is skipped
- If schema changes, files are regenerated
- The generated file is excluded from source control (lives in `obj/`)

Clean the build with `dotnet clean` to force regeneration.

## Multi-project solutions

For solutions with multiple projects, each project can have its own `.env.schema`:

```
MyProject/
  .env.schema        ← Project-specific schema
  MyProject.csproj
  Program.cs

OtherProject/
  .env.schema        ← Different schema
  OtherProject.csproj
  Program.cs
```

Each project generates its own type file independently.

## Troubleshooting

| Issue | Solution |
| --- | --- |
| "varlock: command not found" | Install `varlock` as a dependency: `npm install --save-dev varlock` |
| "Schema not found" or path error | Verify `VarlockSchemaPath` matches your actual schema location |
| Generated file path mismatch | Ensure `VarlockGeneratedFile` matches the `path=` in your `@generateTypes(...)` comment |
| Generated types not in IDE | Run `dotnet clean && dotnet build` to regenerate from scratch |
| Build succeeds but types missing | Check that `VarlockGenerateTypes` is `true` and the generated file is written to `obj/` |

See [Troubleshooting & diagnostics](/integrations/dotnet/troubleshooting/) for more help.

## What this package does not do yet

This package focuses on deterministic, CLI-driven type generation in the standard build pipeline. It does not provide:

- Roslyn `IIncrementalGenerator` (live source generation without invoking the CLI)
- Analyzer diagnostics or IntelliSense during schema editing
- Live IDE preview before running `dotnet build`
- Build-without-CLI fallback (the CLI is required)

Future analyzer or source-generator work can extend this package. See [Varlock.SourceGeneration](/packages/dotnet/) for the current story.

## Related packages

- **[Varlock.SourceGeneration](/packages/dotnet/)** — Source-generation wrapper and future home for Roslyn features
- **[Varlock.Extensions.Configuration](/integrations/dotnet/getting-started/)** — Runtime configuration provider
- **[Varlock.DotNet](/packages/dotnet/)** — Low-level CLI bridge

## Next steps

- **[Getting started guide](/integrations/dotnet/getting-started/)** — Complete setup walkthrough
- **[Typed access](/integrations/dotnet/typed-options/)** — Using generated types in your app
- **[Type generation & MSBuild](/integrations/dotnet/typed-options/)** — Deep dive into the build process
