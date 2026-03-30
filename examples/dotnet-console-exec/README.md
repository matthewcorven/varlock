# Exec Console Example

This example shows the exact external-command seam that is reproducible in-repo today: Varlock resolves a sensitive value from a local `bun -e ...` command, and the .NET example shows the resolved value only through a manual redaction helper. It does not claim any broader secret-manager integration beyond that local command boundary.

Run it from this directory:

```bash
dotnet run
```