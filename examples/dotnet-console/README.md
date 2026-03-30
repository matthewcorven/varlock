# dotnet-console

This is the narrow happy-path Varlock `.NET` example. It shows that a console app can call `builder.Configuration.AddVarlock()` and read the resolved values back through standard `IConfiguration` access at startup.

The example keeps safe committed values alongside the app — the baseline `.env.schema` plus a matching `.env` for a familiar local starting point — so `dotnet run` from this directory resolves the baseline configuration without any extra example-local setup.

It does not claim reload support, typed config generation, direct runtime usage, DI helper extensions, sensitive/logging behavior, plugins, or any other advanced feature story.

Run it from this directory:

```bash
dotnet run
```
