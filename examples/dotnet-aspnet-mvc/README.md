# ASP.NET Core MVC Example

This example proves the MVC integration seam only: `WebApplicationBuilder.AddVarlock()` joins the normal ASP.NET Core configuration pipeline, Varlock overrides overlapping keys from `appsettings.json`, and User Secrets-only keys still flow through in development. It does not carry typed-generation, Serilog, reload, or options demos; those live in the focused console siblings and package tests.

Run it from this directory:

```bash
dotnet run
```

For the proof payload used by `bun run proof:dotnet`:

```bash
dotnet run -- --dump-config
```