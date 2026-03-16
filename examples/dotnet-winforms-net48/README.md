# Varlock WinForms .NET Framework 4.8 Example

Minimal legacy desktop bridge proof. This example demonstrates that the Varlock .NET runtime packages (built targeting `netstandard2.0`) can load and validate configuration on a legacy .NET Framework 4.8 WinForms application.

## Scope

This example proves:

- The Varlock .NET runtime can target `net48` via `netstandard2.0`
- Executable discovery and CLI bridge invocation work on Windows legacy runtime
- Configuration loading, schema validation, and metadata access function correctly
- Machine-readable proof output via `--dump-config` flag

It **does not** prove:

- Full desktop UI integration
- Configuration reload or watch semantics
- Generated C# types or MSBuild integration for legacy targets
- Any modern hosting abstractions

## Running

This example requires Windows and .NET Framework 4.8 Developer Pack.

### Interactive mode (MessageBox display):
```bash
cd examples/dotnet-winforms-net48
dotnet build
dotnet run
```

The application will display a MessageBox showing the loaded configuration as JSON.

### Proof mode (machine-readable stdout):
```bash
cd examples/dotnet-winforms-net48
dotnet run -- --dump-config
```

The application will print JSON to stdout and exit immediately, suitable for automated proof harnesses.

