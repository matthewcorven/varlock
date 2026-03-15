## Varlock.MSBuild

Build-time Varlock integration for SDK-style `.csproj` files.

This first slice wires deterministic C# type generation into `dotnet build` by invoking the existing `varlock typegen` flow and writing generated output into `obj/Varlock/`.
