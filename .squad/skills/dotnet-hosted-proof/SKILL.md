---
name: "dotnet-hosted-proof"
description: "Reusable pattern for self-contained hosted .NET proof examples"
domain: "dotnet-hosted-proof"
confidence: "high"
source: "P3-A1b implementation"
---

# SKILL: Self-contained hosted `.NET` proof modes

Use this pattern when a checked-in `.NET` example must prove configuration reload or options behavior without relying on an external orchestrator to drive host internals.

## Pattern

1. **Keep the production extension thin**
   - Extension methods on `HostApplicationBuilder` should delegate to `builder.Configuration.AddVarlock(...)`.
   - If the example depends on content-root-relative schema discovery, default `WorkingDirectory` to `builder.Environment.ContentRootPath` only when the caller did not set one.

2. **Embed proof modes behind explicit CLI flags**
   - Add flags like `--dump-config`, `--reload-proof`, `--reload-fail-proof`, or `--snapshot-proof`.
   - Default app behavior stays normal; proof flags activate self-testing flows.

3. **Let the hosted app drive the proof**
   - **Worker / Generic Host:** run the proof in a `BackgroundService`, mutate `.env.schema`, subscribe to `IOptionsMonitor<T>.OnChange`, print tagged lines, then stop the app with `IHostApplicationLifetime.StopApplication()`.
   - **Scoped ASP.NET proof:** use `app.Services.CreateScope()` and `IOptionsSnapshot<T>` directly. One scope should stay alive across reload; later scopes prove the next successful state.

4. **Emit machine-readable tagged lines**
   - Prefix each line with a stable tag (`WORKER_RELOAD_PROOF_*`, `SNAPSHOT_PROOF_*`, etc.).
   - Keep payloads JSON so `bun run proof:dotnet` can parse them without scraping human log text.

5. **Always restore mutated schema content**
   - Read the original `.env.schema`, write proof mutations, and restore it in `finally`.
   - Failed reload proof should assert last-known-good behavior from new scopes or `CurrentValue`, not invent any Varlock-only semantics.

## Why

This pattern keeps the proof script small and keeps semantics in the real runtime flow. It also scales cleanly to future hosted examples (Functions, Blazor Server, other Generic Host apps) because the app process owns timing, change detection, and shutdown.
