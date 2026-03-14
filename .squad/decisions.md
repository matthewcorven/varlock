# Squad Decisions

## Active Decisions

### 2026-03-13: First `.NET` implementation slice boundary

- Initiative: `dotnet-support`
- Source: Picard
- Decision: The first implementation slice is limited to phase-0 closure plus the minimum phase-1 work that can start immediately without leaking into hosting, reload, native runtime, analyzer expansion, or broader platform parity.
- Start now: Tuvok owns the machine-readable bridge contract and fixtures, O'Brien owns the executable-distribution specimen, Geordi owns the C# generation specimen, and Data starts the initial `Varlock.DotNet` plus `Varlock.Extensions.Configuration` bridge skeleton once contract and acquisition outputs are concrete.
- Explicitly deferred: `Varlock.Extensions.Hosting`, reload or watch semantics, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`, Serilog helpers, Azure Functions isolated, Blazor variants, native runtime, analyzer evolution, and `.NET` parity work for `varlock run` or `varlock scan`.

### 2026-03-13: First `.NET` bridge slice shape

- Initiative: `dotnet-support`
- Source: Data
- Decision: The first code-bearing `.NET` slice is a startup-only path from the existing `varlock` CLI machine contract into standard `.NET` configuration.
- Package shape: keep CLI hardening in `packages/varlock`, add `packages/dotnet/Varlock.DotNet` for executable resolution, process invocation, DTOs, and typed bridge errors, and add `packages/dotnet/Varlock.Extensions.Configuration` for `IConfigurationSource`, `IConfigurationProvider`, and `AddVarlock(...)`.
- Smallest viable deliverable: `ConfigurationBuilder().AddJsonFile(...).AddVarlock(...)` works at startup, Varlock-backed keys flatten into configuration, Varlock overrides `appsettings` by provider order, structured bridge failures surface for missing executable, incompatible version, missing schema, invalid schema, and resolution failure, and `optional: true` can yield an empty provider when the schema entry point is absent.
- Explicitly deferred: hosting helpers, reload or watch behavior, options-monitor semantics, managed executable distribution automation, source generation or MSBuild integration, and broader platform examples.

### 2026-03-13: First CLI bridge contract slice

- Initiative: `dotnet-support`
- Source: Tuvok
- Decision: The first supported machine-readable bridge entry point is `varlock load --format json-full --bridge-contract 1`.
- Transport: in bridge mode, the CLI emits JSON envelopes on stdout for both success and failure and uses the process exit code for status.
- Success shape: `{ contractVersion, cliVersion, command, format, ok: true, graph }`.
- Failure shape: `{ contractVersion, cliVersion, command, format, ok: false, category, message, ... }`.
- Proven failure categories: `executable-version-mismatch`, `schema-missing`, `schema-invalid`, `resolution-failed`, `plugin-load-failed`, and `bridge-internal-error`.
- Caller boundary: `executable-not-found` remains a caller-side bridge error because that failure happens before the CLI starts.

### 2026-03-13: Bridge scaffold flattening and parser seam

- Initiative: `dotnet-support`
- Source: Data
- Decision: The startup-only configuration scaffold will flatten structured Varlock values into standard `IConfiguration` child keys using `:` delimiters and will not emit duplicate root JSON-string entries for object or array values.
- Bridge contract alignment: `Varlock.DotNet` invokes `varlock load --format json-full --bridge-contract 1` and treats the stdout JSON envelope as the authoritative post-launch contract for both success and failure paths.
- Parser seam: the low-level runtime parser keeps a narrow internal fallback for the legacy unversioned raw `json-full` graph only when bridge-envelope markers are absent, while reserving explicit contract-version gating for the machine-readable handshake.
- Failure mapping: once the CLI starts, bridge failures map from the envelope category and location fields; only launch-time discovery remains a caller-side `executable-not-found` bridge error.
- Why: standard `.NET` binding stays predictable when structured values look like normal configuration sections, and the bridge can execute against today's CLI output without freezing premature public version-negotiation behavior.

### 2026-03-13: First `.NET` proof obligations

- Initiative: `dotnet-support`
- Source: O'Brien
- Decision: The first `.NET` implementation slice is only acceptable if it includes the minimum proof and release scaffolding needed to keep phase-1 claims honest.
- Required in-slice proof: a defined executable-distribution specimen for local development and CI, machine-readable contract-fixture coverage for success and named failure categories, proving examples for a console app and an ASP.NET Core MVC app, and at least one CI path that exercises executable discovery plus a proving example.
- Runnable proof shape: the first follow-up proof slice lands only `examples/dotnet-console-net8/` for direct `Varlock.DotNet` runtime usage and `examples/dotnet-aspnet-mvc-net8/` for startup-only `Varlock.Extensions.Configuration` ordering over `appsettings.json`.
- CI and task wiring: `bun run proof:dotnet` is the single repository proof command, and the main `test.yaml` workflow runs it on Ubuntu with the .NET SDK installed.
- Executable acquisition boundary: this slice proves the checked-in repo-local development lookup from the example working directories to `packages/varlock/bin/cli.js` without an explicit `ExecutablePath`, but it does not yet prove package-local installs, `node_modules/.bin`, offline acquisition, package-managed assets, broader plugin-layout support, opt-in `PATH` fallback, or a platform matrix.
- Documentation constraint: support-matrix and docs claims must be limited to what those proof artifacts actually demonstrate; proven rows stop at console direct runtime, ASP.NET startup-provider usage, and `dotnet run` startup discovery.
- Explicitly deferred: broader platform matrix proof, hosted reload behavior, worker or options-monitor proof, Azure Functions isolated, Blazor variants, and wider legacy-target validation.

### 2026-03-13: P1-A1 executable lookup and handshake hardening

- Initiative: `dotnet-support`
- Node: `P1-A1`
- Source: Data
- Decision: `Varlock.DotNet` hardens executable acquisition by preferring explicit configuration, then package-local and repo-local development layouts, and only falling back to `PATH` when that behavior is explicitly enabled.
- Lookup order: explicit `ExecutablePath`, package-local `node_modules/varlock/bin/cli.js`, local `node_modules/.bin/varlock`, repo-local `packages/varlock/bin/cli.js`, then `PATH` when enabled.
- Handshake: before the normal `load`, the runtime performs a preflight compatibility probe via `load --bridge-contract 0 --compact` and requires the CLI to answer with the existing `executable-version-mismatch` envelope that names supported contract version `1`.
- Failure boundary: launch-time discovery remains caller-side `executable-not-found`; once the executable starts, absent or incompatible handshake behavior maps to `executable-version-mismatch`, and only the later bridge `load` parses post-launch success or failure envelopes for schema or resolution diagnostics.
- Why: this advances executable acquisition hardening beyond the explicit repo-relative proof path without inventing a second protocol or broadening into hosting or packaging automation.

### 2026-03-13: P1-A1 lookup proof harnesses and narrowed executable claims

- Initiative: `dotnet-support`
- Node: `P1-A1`
- Source: Data and O'Brien
- Decision: prove package-local, local `node_modules/.bin/varlock`, and opt-in `PATH` executable acquisition with proof-only harnesses inside `bun run proof:dotnet` instead of widening the checked-in examples or broadening documentation beyond what the repository actually exercises by default.
- Proof shape: temporarily create `examples/dotnet-console-net8/node_modules/varlock/bin/cli.js` and `examples/dotnet-console-net8/node_modules/.bin/varlock`, delegate each wrapper to the real repo CLI, and assert distinct markers so proof can distinguish package-local and local `.bin` acquisition from repo-local fallback; for `PATH`, prepend a temporary `varlock` entry and set `VARLOCK_DOTNET_PROOF_FORCE_PATH_LOOKUP=1` so the console example disables local lookup only for that proof run.
- Proven surface now: checked-in proof covers repo-local, package-local, local `.bin`, and opt-in `PATH` lookup.
- Still planned: offline acquisition, version-handshake specimens, and broader packaging-layout proof remain follow-on work outside this node.
- Why: this closes the executable-acquisition branch of P1-A1 while keeping default example behavior narrow and support claims pinned to exercised layouts.

### 2026-03-13: First `lang=cs` specimen shape

- Initiative: `dotnet-support`
- Source: Geordi
- Decision: The first mergeable `lang=cs` specimen stays isolated inside the existing Varlock type-generation path in `packages/varlock` and does not depend on the current `.NET` bridge packages.
- Scope: generate a flat `Varlock.Generated.VarlockConfig` POCO from schema metadata, use PascalCase property names derived from env keys, preserve original env keys and sensitive-key membership in a generated metadata sidecar, and treat the checked-in `.env.schema` plus `.g.cs` golden file as the regression artifact.
- Explicitly deferred: nested object projection rules, binder-validation proof in a compiled `.NET` example, custom C# attributes for sensitive metadata, and MSBuild or package integration.
- Why: this opens a real `lang=cs` implementation seam now, keeps deterministic output under the existing typegen flow, and avoids colliding with bridge and MSBuild work before naming and binder proof are fully locked.

### 2026-03-13: C# typegen binder-prep metadata shape

- Initiative: `dotnet-support`
- Node: `P1-B1`
- Source: Geordi
- Decision: deepen the initial `lang=cs` specimen by emitting dependency-free binding metadata in `VarlockConfigMetadata` instead of taking a direct dependency on `Microsoft.Extensions.Configuration` binder attributes in this slice.
- Output shape: generated C# includes a `PropertyBindings` list whose entries preserve the original env key, generated property name, static required-ness, and sensitive status.
- Rationale: this keeps the specimen isolated inside the existing Varlock type-generation seam, avoids forcing a compile-time Microsoft.Extensions dependency onto every generated-type consumer, and gives `P2-B1` stable metadata for later binder or MSBuild glue without re-deriving names from schema.
- Deferred: direct `ConfigurationKeyNameAttribute` emission, binder package coupling, and generated runtime helpers that assume the bridge or MSBuild packages are already present.

### 2026-03-13: P1-B1 deterministic C# namespace and type naming overrides

- Initiative: `dotnet-support`
- Node: `P1-B1`
- Source: Geordi
- Decision: allow `@generateTypes(lang=cs, ...)` to accept deterministic `namespace` and `typeName` overrides, while deriving the metadata sidecar name as `${typeName}Metadata`.
- Validation: override validation stays inside `packages/varlock` type generation, and invalid overrides fail early with explicit generator errors.
- Boundaries: no binder package dependency or MSBuild target is introduced in this slice.
- Why: this gives teams a stable application-facing naming seam now and preserves a low-friction handoff into later binder and MSBuild work.

### 2026-03-13: P1 proof-accounting guardrails

- Initiative: `dotnet-support`
- Source: O'Brien
- Decision: `P1-A1` and `P1-B1` may advance as in-progress implementation lanes without silently widening the repository's proven support claims.
- Proof accounting: `P1-A1` is now repository-backed across the full intended v1 acquisition order: repo-local development lookup, package-local install layout, local `node_modules/.bin`, and opt-in `PATH` fallback all have checked-in proof via `bun run proof:dotnet` plus targeted runtime tests.
- Support-matrix constraint: executable-acquisition claims may be marked proven only for those exercised branches; offline acquisition, version-handshake specimens, and broader packaging-layout proof remain planned. C# generation remains planned in the support matrix until an example app consumes generated output or compiled binder validation exists.
- Progression alignment: the progression board should treat `P1-A1` as done and move attention to `P1-A2` and `P1-B1`.

### 2026-03-13: P1-A2 first proven proof-matrix rows

- Initiative: `dotnet-support`
- Node: `P1-A2`
- Source: Data and O'Brien
- Decision: the first accepted `P1-A2` expansion proves two narrow developer-experience rows on top of the existing console and ASP.NET examples: explicit `dotnet build` success for both checked-in examples, and ASP.NET User Secrets coexistence in Development.
- Proof command: `bun run proof:dotnet` must first run real `dotnet build` for `examples/dotnet-console-net8/` and `examples/dotnet-aspnet-mvc-net8/`, assert the expected `bin/Debug/net8.0/*.dll` outputs exist, then run the existing runtime assertions with `--no-build`.
- User Secrets boundary: the ASP.NET proof uses the example's `UserSecretsId`, writes a User Secrets-only key and an overlapping `APP_NAME`, and proves that `AddVarlock(...)` preserves the User Secrets-only key while still overriding overlapping values by provider order.
- Still deferred inside `P1-A2`: watch-mode behavior, IDE or IntelliSense observations beyond clean MSBuild compilation, options binding proof, wider platform coverage, Azure Functions local settings coexistence, and any generated-file/MSBuild integration beyond plain build success for the checked-in examples.
- Why: both rows are exercised against checked-in examples and stay within the current startup-only bridge slice without promoting broader hosting or tooling claims.

### 2026-03-13: P1-A2 machine-readable diagnostics proof uses shared CLI fixtures

- Initiative: `dotnet-support`
- Node: `P1-A2`
- Source: Picard and Tuvok
- Decision: the current machine-readable diagnostics proof row is satisfied by consuming the existing CLI load-bridge JSON fixtures directly from the `.NET` bridge-alignment tests instead of maintaining duplicate payload copies.
- Proof artifact boundary: the shared fixture set is the canonical proof artifact for success and failure envelope alignment across the CLI and `.NET` bridge tests, including a shared location-bearing `schema-invalid` parse-error envelope.
- Current proven surface: the shared fixtures now cover category and message fidelity, handshake compatibility, and location-bearing schema-parse diagnostics across both CLI and `.NET` bridge tests.
- Why: one checked-in machine contract keeps proof accounting honest and prevents the `.NET` lane from silently drifting away from the CLI bridge semantics it claims to prove.

### 2026-03-13: Progression-board prompt requirement

- Initiative: `dotnet-support`
- Source: Matthew Corven via Copilot
- Decision: When asking Matthew about work progression, prioritization, delegation, creation, or similar next-step choices, agents must present a visual progression reference and point to the relevant progression line or stable node IDs.
- Board: `.squad/progression.md` is the stable progression reference for the current `.NET` initiative.
- Why: next-step prompts should stay grounded in a visible progression map with minimal additional process.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
