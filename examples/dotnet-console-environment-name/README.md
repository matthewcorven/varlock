# Environment Name Console Example

This example shows the `EnvironmentName` propagation path for `AddVarlock(...)`. The app injects a tiny `IVarlockRuntime` that reads `VarlockLoadOptions.EnvironmentName` and returns the production variant when the provider supplies `EnvironmentName = "production"`.

The checked-in `.env` and `.env.production` files mirror the same base-versus-production split, but this specimen intentionally does not claim that the current CLI-backed configuration-provider entry path auto-loads those files from `EnvironmentName` alone.

Run it from this directory:

```bash
dotnet run
```