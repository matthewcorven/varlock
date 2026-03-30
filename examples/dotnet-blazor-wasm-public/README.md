# Blazor WebAssembly Public-Only Example

This example shows the `publicOnly=true` build-time boundary for WebAssembly. `dotnet build` generates a public C# surface for client-safe values and metadata, while sensitive keys stay out of the generated file and there is no runtime Varlock bridge in the browser bundle.

Run it from this directory:

```bash
dotnet build
dotnet run
```