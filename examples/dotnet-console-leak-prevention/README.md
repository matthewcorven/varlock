# Leak-Prevention Console Example

This example proves the current .NET boundary for `@preventLeaks`: the bridge surfaces `graph.PreventLeaks`, but the runtime packages do not automatically intercept console output or HTTP responses. The sample prints the flag, confirms the sensitive value is still present in configuration, and uses a manual redaction helper for display.

Run it from this directory:

```bash
dotnet run
```