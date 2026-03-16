# Varlock.Serilog

Serilog-specific destructuring redaction and metadata enrichment helpers for Varlock.

- `WithVarlockRedaction(graph)` redacts exact, case-sensitive sensitive-key matches during Serilog destructuring and uses the literal `[REDACTED]`.
- `WithVarlockMetadata(graph)` enriches `VarlockRedactLogs`.
- Scalar message-template parameters and non-Serilog channels are outside this redaction path.
