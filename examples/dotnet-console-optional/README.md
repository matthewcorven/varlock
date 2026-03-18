# Optional Console Example

This example proves the narrow `Optional = true` startup path for `AddVarlock(...)`. `Program.cs` intentionally points Varlock at a missing working directory so the provider starts empty instead of failing the app.

The checked-in root schema and values are only a reference shape for the example; they are not used by the proof run because the app is demonstrating graceful degradation when the configured entry point is absent.

Run it from this directory:

```bash
dotnet run
```