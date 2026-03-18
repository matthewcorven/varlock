# Public-Only Generation Console Example

This example proves only the build-time `publicOnly=true` boundary. `dotnet build` emits a generated C# type that keeps public properties and `PropertyKeys` metadata while excluding sensitive properties and sensitive metadata; the example does not claim any runtime enforcement.

Run it from this directory:

```bash
dotnet build
dotnet run
```