# Tuvok — History

## Core Context

- **Project:** A first-class Varlock .NET support initiative built around a v1 CLI bridge, proof artifacts and support-matrix validation, and an explicit path to future native runtime or analyzer evolution.
- **Role:** Security/Contracts Lead
- **Joined:** 2026-03-13T10:56:25.546Z

## Learnings

<!-- Append learnings below -->
- 2026-03-13: Picard assigned Tuvok first ownership of the machine-readable bridge contract, including a versioned success shape, stable error categories, and reproducible fixtures for success, missing executable, version mismatch, schema invalid, resolution failed, and plugin load failed.
- 2026-03-13: Data's bridge slice depends on those contract outputs before the initial `Varlock.DotNet` and configuration-provider skeleton should advance, so contract stability is the gating artifact for the first runtime implementation.
- 2026-03-13: The first engine-side bridge slice now uses `varlock load --format json-full --bridge-contract 1` with JSON envelopes on stdout for both success and failure; executable discovery remains a caller-side contract because the CLI cannot truthfully self-report launch failures.
- 2026-03-13: Data fixed the bridge scaffold so structured values flatten into ordinary `IConfiguration` child keys without JSON shadow entries, and the low-level parser keeps today's unversioned `json-full` success payload behind an internal seam until the explicit handshake is enforced end to end.
- 2026-03-13: The current P1-A2 diagnostics proof should reuse the checked-in CLI load-bridge fixtures from the `.NET` alignment tests instead of duplicating payloads, while keeping location-aware failure parsing covered by a narrow targeted test until the CLI fixture set includes that shape.
- 2026-03-13: A malformed schema parse error (`# @defaultSensitive(` followed by a normal entry) yields a real `schema-invalid` bridge envelope with `.env.schema:3:1`, which now serves as the shared location-bearing fixture for both CLI and .NET bridge-alignment tests.
