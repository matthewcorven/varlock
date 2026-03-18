# Explicit Executable Console Example

This example proves only the explicit CLI-path override. It disables local and `PATH` lookup, points `ExecutablePath` at the checked-in repo CLI, and shows that the provider still loads configuration through that exact path.

Run it from this directory:

```bash
dotnet run
```