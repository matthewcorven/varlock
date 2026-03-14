# Geordi — History

## Core Context

- **Project:** A first-class Varlock .NET support initiative built around a v1 CLI bridge, proof artifacts and support-matrix validation, and an explicit path to future native runtime or analyzer evolution.
- **Role:** Tooling Lead
- **Joined:** 2026-03-13T10:56:25.545Z

## Learnings

<!-- Append learnings below -->
- 2026-03-13: Picard assigned Geordi the first C# generation specimen as phase-1 work that can advance in parallel once naming and output expectations are fixed.
- 2026-03-13: O'Brien's proof scope means the generation slice should travel with representative schema, `.g.cs` golden output, and binder-validation proof if `lang=cs` is included in the first implementation slice.
- 2026-03-13: The first isolated `lang=cs` slice can live entirely inside `packages/varlock` by emitting a flat POCO plus sidecar metadata for original keys and sensitive items, leaving binder attributes and MSBuild packaging for the next phase.
- 2026-03-13: The canonical specimen shape is now fixed at a flat `Varlock.Generated.VarlockConfig` POCO with PascalCase property names, metadata sidecar output, and checked-in schema plus `.g.cs` golden fixtures, with binder proof and packaging still deferred.
- 2026-03-13: For P1-B1, adding direct `ConfigurationKeyNameAttribute` output would prematurely force a Microsoft.Extensions compile-time dependency into every generated C# consumer, so the safer deepening move is a richer sidecar `PropertyBindings` shape that preserves key-to-property, required, and sensitive metadata for later binder/MSBuild work.
- 2026-03-13: O'Brien kept P1-B1 honest in the proof ledger, so richer generated metadata can move forward as implementation work, but support-matrix claims stay planned until an example app consumes the output or compiled binder validation exists.
- 2026-03-13: A small but durable follow-on seam for P1-B1 is letting `@generateTypes(lang=cs, ...)` override the emitted namespace and root type name while deriving the metadata sidecar name predictably, so future MSBuild integration can place generated symbols correctly without post-processing.
- 2026-03-13: That naming seam is now an active squad decision: `namespace` and `typeName` overrides are the binder-friendly next step, `${typeName}Metadata` stays derived, and invalid overrides should fail inside `packages/varlock` without adding binder or MSBuild dependencies.
