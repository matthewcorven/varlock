# DI Options Console Example

This example shows the current manual hosted-options pattern only. `AddVarlock()` loads configuration, then a user-authored options class is populated explicitly from `IConfiguration` and consumed through `IOptionsMonitor<T>`; it does not claim any `AddVarlock<T>()` convenience API.

Run it from this directory:

```bash
dotnet run
```