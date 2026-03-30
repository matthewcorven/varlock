# Public-Only Type Generation Console Example

This example shows the build-time `publicOnly=true` type-generation boundary. `dotnet build` emits a generated C# type that keeps public properties and `PropertyKeys` metadata while excluding sensitive properties and sensitive metadata; the example does not claim any runtime enforcement.

Run it from this directory:

```bash
dotnet build
dotnet run
```