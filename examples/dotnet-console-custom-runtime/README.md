# Custom Runtime Console Example

This example proves the narrow `VarlockConfigurationSource.Runtime` injection seam. Instead of spawning the CLI bridge, the app supplies a custom `IVarlockRuntime` implementation that returns a fixed graph and lets the rest of the configuration-provider pipeline consume it normally.

It does not claim executable lookup, handshake compatibility, or schema-resolution behavior beyond the injected runtime contract itself.

Run it from this directory:

```bash
dotnet run
```