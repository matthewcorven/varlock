# Options Monitor Console Example

This example shows `IOptionsMonitor<T>` long-lived singleton access with Varlock configuration. `IOptionsMonitor<AppOptions>` is resolved as a singleton, demonstrating that the Varlock configuration provider works with the standard .NET options monitoring pattern.

Run it from this directory:

```bash
dotnet run
```
