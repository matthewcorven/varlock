# Validation Console Example

This example intentionally fails. It shows the current configuration-provider startup behavior when a required key resolves empty: `dotnet run` exits non-zero after printing the bridge error category and message.

Expected behavior from this directory:

```bash
dotnet run
```

The command should exit with code `1`, report `VALIDATION_CATEGORY = ResolutionFailed`, and print a required-value message.