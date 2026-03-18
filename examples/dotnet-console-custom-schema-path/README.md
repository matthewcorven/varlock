# Custom Schema Path Console Example

This example proves the narrow `SchemaPath` override story for `AddVarlock(...)`. The app keeps its schema and values under `config/` and points the provider at that non-default entry path.

It does not claim custom working-directory behavior, environment switching, optional startup, or custom runtime injection.

Run it from this directory:

```bash
dotnet run
```