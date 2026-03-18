# Varlock

Metapackage that bundles all core Varlock packages for .NET:

- **Varlock.DotNet** — Core library for loading and validating `.env` files with `@env-spec` schemas
- **Varlock.Extensions.Configuration** — `IConfiguration` provider integration
- **Varlock.Extensions.Hosting** — `IHostBuilder` / `WebApplicationBuilder` extensions
- **Varlock.MSBuild** — Build-time type generation and optional schema validation

## Installation

```bash
dotnet add package Varlock
```

## Optional packages

These are **not** included in the metapackage and must be added separately if needed:

- **Varlock.Serilog** — Serilog enricher that redacts sensitive values in logs
