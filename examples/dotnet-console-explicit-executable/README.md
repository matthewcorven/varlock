# Explicit Executable Path Console Example

This example shows the explicit executable-path override. It disables local and `PATH` lookup, points `ExecutablePath` at the checked-in repo CLI, and shows that the provider still loads configuration through that exact path.

Run it from this directory:

```bash
dotnet run
```