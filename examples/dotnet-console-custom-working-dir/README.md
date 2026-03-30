# Custom Working Directory Console Example

This example shows the `WorkingDirectory` override for `AddVarlock(...)`. The app leaves `SchemaPath` at the default `.env.schema` name and instead points Varlock at a sibling `shared/` directory.

It does not claim non-default schema-path behavior, environment-specific loading, optional startup, or custom runtime injection.

Run it from this directory:

```bash
dotnet run
```