# .NET Phase 4: Contract & Security Boundary Evolution Assessment

- **Author:** Tuvok (Contracts & Security Lead)
- **Deliverable:** P4-A1 / E3
- **Date:** 2026-03-16
- **Status:** Evaluation artifact — no product code authorized

---

## Scope

This document evaluates three evolution axes for the .NET Varlock packages:

1. Whether bridge-contract v1 is sufficient or where v2 would be required
2. What JS-runtime security behaviors remain impossible through the bridge vs. what could be approximated in .NET
3. What would be required for .NET-native plugin authoring

Each axis receives a justified recommendation: **now**, **later**, or **not justified**.

All claims are grounded in the locked Phase 3 contract (proposal lines 636–653), the proven bridge implementation (`VarlockCliRuntime.cs`), and the Phase 3 proof artifacts.

---

## 1. Bridge-Contract v1 Stability Assessment

### What v1 provides today

Bridge-contract v1 (`--bridge-contract 1`) is a single-shot, full-graph protocol:

- **Handshake:** Preflight probe via `--bridge-contract 0 --compact` → expects `executable-version-mismatch` envelope with `supportedContractVersion: 1` (see `VarlockCliRuntime.cs` lines 230–300, constant `SupportedContractVersion = 1`)
- **Load:** `varlock load --format json-full --bridge-contract 1 --compact --path <schema>` → JSON envelope on stdout
- **Success shape:** `{ contractVersion: 1, cliVersion, command, format, ok: true, graph: { config, sources, settings } }`
- **Failure shape:** `{ contractVersion: 1, …, ok: false, category, message, location? }`
- **7 error categories:** `ExecutableNotFound` (caller-side), `ExecutableVersionMismatch`, `SchemaMissing`, `SchemaInvalid`, `ResolutionFailed`, `PluginLoadFailed`, `BridgeInternalError` — mapped in `ParseFailureCategory` (lines 423–435)
- **Value model:** Items carry `{ value, isSensitive }`, settings carry `{ redactLogs, preventLeaks }`, sources carry `{ label, enabled, path? }`

### Scenario analysis: does v1 suffice?

| Scenario | v1 sufficient? | Rationale |
|----------|---------------|-----------|
| **Initial load at startup** | ✅ Yes | This is the designed use case. Proven across 7 examples. |
| **Reload via `ReloadOnChange`** | ✅ Yes | Reload re-invokes the same `load` command. No new envelope shape needed. Proven in `ReloadTests.cs`. |
| **Serilog redaction** | ✅ Yes | `graph.Items[key].IsSensitive` and `graph.RedactLogs` are sufficient signals. Proven in `VarlockSerilogExtensionsTests.cs`. |
| **`publicOnly` WASM generation** | ✅ Yes | Generation is CLI-side (`varlock typegen`), not bridge-side. The bridge is not involved. |
| **Incremental/partial reload** | ❌ No — but not needed | v1 always re-resolves the full graph. The `.NET` provider atomically swaps the full `Data` dictionary. A partial-graph delta protocol would require v2, but no user friction has been demonstrated. See E1 (Data's bridge limits audit) for latency evidence. |
| **In-process schema introspection** | ❌ No — but not needed | Querying schema metadata (types, decorators, constraints) without a full `load` round-trip would require a new CLI command or a native parser. No current .NET feature requires this. |
| **Plugin metadata query** | ❌ No — but not needed | Today, plugin success/failure surfaces through the normal `load` envelope. Querying available plugins, their capabilities, or their configuration surfaces would require a new protocol. No .NET feature requires this today. |
| **Streaming/chunked output** | ❌ No — but not needed | v1 reads full stdout after process exit. Streaming would require a different transport (e.g., line-delimited JSON). No use case justifies this for configuration loading. |
| **Bidirectional communication** | ❌ No — but not needed | The bridge is fire-and-forget: spawn → read → done. Interactive scenarios (e.g., prompting for secrets) would require stdin/stdout handshaking. No v1 .NET feature requires this. |

### v2 trigger conditions

A v2 bridge contract would be justified **only** if:

1. **E1 latency data** shows that full-graph reload causes measurable user-facing delays (>500ms p95) in representative schemas, AND incremental reload would demonstrably reduce that latency
2. A .NET feature requires **schema introspection without load** — e.g., a Roslyn analyzer that needs type information at design-time without resolving values
3. A .NET feature requires **plugin capability discovery** separate from loading

None of these conditions is currently met. The handshake mechanism (`--bridge-contract 0` probe) already provides a forward-compatible version-negotiation path: a future CLI that supports v2 would respond with `supportedContractVersion: 2` and the .NET client could opt in.

### Recommendation: **Not justified now. v1 is sufficient.**

The forward-compatibility mechanism exists. No demonstrated user friction requires v2. If E1 reveals latency problems, revisit incremental reload as a v2 candidate, but solve it by measuring first.

---

## 2. Security Boundary Completeness

### What the JS runtime does automatically

The JavaScript runtime provides three automatic security mechanisms that the .NET bridge does not replicate. These are documented from code inspection of `packages/varlock/src/`:

#### 2a. Console output redaction (`redactLogs`)

**JS mechanism:** `patch-console.ts` monkey-patches `console.log`, `console.warn`, `console.error`, etc. by hooking Node.js's internal `kWriteToConsole` symbol. `redactSensitiveConfig()` in `runtime/env.ts` builds a regex from all `isSensitive` values and replaces exact matches in every logged argument with masked strings (`"he▒▒▒▒▒"`).

**Triggered by:** `@redactLogs` decorator (default: `true` when any sensitive item exists)

**What .NET v1 provides instead:** `Varlock.Serilog.WithVarlockRedaction(graph)` — a Serilog-only destructuring policy using exact case-sensitive key matching, replacing sensitive values with `"[REDACTED]"`. Does NOT intercept `Console.WriteLine`, `Microsoft.Extensions.Logging`, `Debug.WriteLine`, or any non-Serilog channel. `VarlockRedactionHelper.Redact(graph, key, value)` provides manual per-value redaction.

**Could .NET approximate this?**

| Approach | Feasibility | Risk | Verdict |
|----------|------------|------|---------|
| Wrap `Console.Out` with a filtering `TextWriter` | Technically possible | Breaks assumptions of anything that reads `Console.Out` directly; incompatible with buffered I/O; may corrupt structured console output (JSON, progress bars); runtime performance cost on every write | **Not recommended** |
| `Microsoft.Extensions.Logging` provider with redaction | Technically possible via custom `ILoggerProvider` | Only covers MEL consumers; does not cover raw `Console.WriteLine`; regex-based value matching in log messages is fragile and slow | **Possible future package** (`Varlock.Logging`), but overclaims if marketed as equivalent to JS auto-redaction |
| Serilog sink-level redaction (redact at output, not destructuring) | Technically possible | Serilog-only; broader than current policy-level approach but still limited to Serilog pipeline | **Possible v2 of `Varlock.Serilog`**, but current destructuring-level redaction is sufficient for v1 |

**Assessment:** Global console redaction is architecturally inappropriate in .NET. The CLR does not provide the hooking surface that Node.js offers. The Serilog destructuring policy is the correct .NET-idiomatic approach for structured logging. Extending to MEL is justified as a future package if demand materializes, but it would still not be "automatic" in the JS sense — it only covers logging framework consumers, not raw `Console.WriteLine`.

#### 2b. HTTP response leak prevention (`preventLeaks`)

**JS mechanism:** `patch-response.ts` wraps the global `Response` constructor. `patch-server-response.ts` patches `http.ServerResponse.prototype.write()` and `.end()`, including gzip decompression, to scan response bodies for sensitive value substrings. Detection throws: `"🚨 DETECTED LEAKED SENSITIVE CONFIG - ${itemKey}"`.

**Triggered by:** `@preventLeaks` decorator (default: `true`)

**What .NET v1 provides instead:** `VarlockResolvedGraph.PreventLeaks` is surfaced as a boolean metadata property. No .NET code acts on it.

**Could .NET approximate this?**

| Approach | Feasibility | Risk | Verdict |
|----------|------------|------|---------|
| ASP.NET Core middleware that scans response bodies | Technically possible | Requires buffering the entire response body before sending (defeats streaming, chunked transfer); regex scan on every response is a performance tax; only covers ASP.NET Core, not Kestrel raw, gRPC, SignalR, or other transports; false positives on partial matches | **Possible as opt-in middleware**, but must NOT be marketed as equivalent to JS auto-prevention |
| ASP.NET Core `IStartupFilter` that conditionally registers the middleware | Cleanest integration point | Same body-buffering concerns | Same as above |
| `IHttpResponseBodyFeature` wrapper | Lower-level, less buffering | Complex, Kestrel-internal coupling | Not recommended for v1 |

**Assessment:** An opt-in ASP.NET Core middleware that scans response bodies for sensitive values is technically feasible and could be a useful `Varlock.AspNetCore` package. However, it would:
- Only cover ASP.NET Core (not console apps, WinForms, Worker Services, gRPC, etc.)
- Require explicit `app.UseVarlockLeakPrevention()` registration — not automatic
- Impose a performance cost from body buffering
- Not cover streaming responses

This is a **valid future package** but must be scoped as "ASP.NET Core response scanning middleware" — not "leak prevention." The term "prevention" implies a guarantee that middleware-based scanning cannot provide.

#### 2c. Process environment injection (`varlock run`)

**JS mechanism:** `varlock run` spawns a child process with resolved environment variables injected. The parent process can also intercept the child's stdout for redaction.

**What .NET v1 provides instead:** Nothing. No `varlock run` equivalent exists.

**Could .NET approximate this?**

`VarlockResolvedGraph` already contains all resolved values. A helper method like `BuildEnvironmentVariables(graph) → Dictionary<string, string>` could project graph items into a dictionary suitable for `ProcessStartInfo.Environment`. This is trivial and low-risk. The proposal (line 317) already defines `IVarlockProcessEnvironment.BuildEnvironmentVariables()` as a potential API.

**Assessment:** Environment-variable projection is justified as a small utility in `Varlock.DotNet`. It is "environment preparation" only (proposal line 647) — not a full `varlock run` replacement, since it does not provide parent-process output redaction or lifecycle management.

### Security boundary summary

| JS behavior | .NET v1 status | .NET approximation possible? | Recommended action |
|-------------|---------------|-----------------------------|--------------------|
| Console output redaction | Serilog destructuring only | MEL provider (future package) | **Later** — `Varlock.Logging` if demand materializes |
| HTTP response leak prevention | Metadata only | ASP.NET Core middleware (future package) | **Later** — `Varlock.AspNetCore` if demand materializes |
| Process output redaction via `varlock run` | Unsupported | Env-var projection (trivial) | **Now** — small utility in `Varlock.DotNet` (env projection only, not run parity) |
| `varlock scan` equivalent | Unsupported | Not feasible without parser | **Not justified** — remains CLI workflow |

### Recommendation: **Security expansion not justified now, with one exception.**

The one exception is `BuildEnvironmentVariables()` — a trivial, low-risk utility that projects graph items into a `Dictionary<string, string>` for child-process environment setup. This does not expand the security contract; it is convenience sugar over existing data.

All other security expansions (MEL redaction, ASP.NET leak scanning) are valid future packages but are not justified until user demand is demonstrated. They must never be marketed as equivalents to the JS runtime's automatic behavior.

---

## 3. Plugin Contract Evolution

### Current plugin architecture

Plugins in Varlock are JavaScript modules loaded by the CLI engine at schema-processing time:

- **Discovery:** `@plugin('./path.js')` or `@plugin('npm:package')` decorator in `.env.schema`
- **Loading:** The CLI engine executes the plugin JS file, injecting a `globalThis.plugin` object
- **Registration API:** Plugins call `plugin.registerRootDecorator()`, `plugin.registerItemDecorator()`, `plugin.registerDataType()`, `plugin.registerResolverFunction()`
- **SEA binary support:** For compiled binaries, plugins are loaded via `node:vm` with ESM→CJS rewriting
- **Failure path:** Loading errors are captured and surfaced through the `plugin-load-failed` bridge error category

### What .NET gets through the bridge

The .NET bridge invokes `varlock load` which internally loads plugins as part of schema resolution. From the .NET side:

- ✅ Plugin-resolved values appear in `VarlockResolvedGraph.Items` — transparent
- ✅ Plugin load failures surface as `VarlockBridgeErrorCategory.PluginLoadFailed` with diagnostics
- ❌ No visibility into which plugins were loaded or what they provide
- ❌ No ability to author plugins in C#
- ❌ No ability to register custom types, decorators, or resolvers from .NET

### What .NET-native plugin authoring would require

#### Minimum viable surface (type-mapping extensions only)

The lowest-risk plugin extension for .NET would be **custom C# type mappings** — allowing a .NET-side plugin to define how a Varlock schema type maps to a C# type in generated code.

Required contract changes:
- New interface: `IVarlockTypeMapping` with `SchemaType → CSharpType` mapping
- Registration: `VarlockLoadOptions.TypeMappings` collection
- Scope: Affects `@generateTypes(lang=cs)` output only — no runtime resolution changes

This does NOT require bridge-contract v2 because type generation is CLI-side. The .NET type mapping would be consumed by the C# code generator, not the bridge protocol.

**Risk:** Low. No bridge change. No new error categories. No runtime behavior change.

#### Medium surface (diagnostics extensions)

A diagnostics extension would allow .NET consumers to hook into the load lifecycle:

Required contract changes:
- New interface: `IVarlockDiagnosticsObserver` with `OnLoadStarted`, `OnLoadCompleted`, `OnLoadFailed` callbacks
- Registration: `VarlockLoadOptions.DiagnosticsObserver`
- Scope: Observation only — no mutation of load behavior

This does NOT require bridge-contract v2. It is a .NET-side observer over the existing bridge interaction.

**Risk:** Low. No bridge change. But the observer interface becomes a public contract that must remain stable.

#### Full parity (resolver/decorator authoring in C#)

Full .NET-native plugin parity would require:

1. **A .NET parser for `@env-spec`** — the PEG.js grammar (`packages/env-spec-parser/grammar.peggy`) would need a C# port or a grammar-to-C# code generator
2. **A .NET resolution engine** — the full `env-graph` resolution pipeline would need a C# implementation
3. **A .NET plugin host** — loading .NET assemblies, instantiating plugin types, invoking registration methods
4. **Bridge-contract v2 or retirement** — if .NET has its own engine, the bridge becomes optional or a fallback

This is a **native runtime**, not a plugin extension. It is a fundamentally different architecture with multi-month implementation scope and ongoing maintenance parity obligations with the JS engine.

**Risk:** Very high. Maintenance burden doubles. Behavioral drift between JS and .NET engines is near-certain without extensive shared test suites.

### Plugin evolution summary

| Surface | Bridge v2 needed? | Implementation scope | Risk | Recommended timing |
|---------|-------------------|---------------------|------|--------------------|
| Custom C# type mappings | No | Small — code-gen extension | Low | **Later** — when demand for custom types materializes |
| Diagnostics observer | No | Small — .NET-side interface | Low | **Later** — when debugging workflow feedback accumulates |
| Resolver/decorator authoring | Yes (or bridge retirement) | Very large — native runtime | Very high | **Not justified** — proposal line 994 requires demonstrated limits, not speculative parity |

### Recommendation: **Not justified now.**

No plugin extension is justified in the current phase. The bridge transparently surfaces plugin-resolved values and plugin-load failures. No .NET user has reported a scenario where C#-authored plugins are needed that JS plugins cannot serve.

The proposal is explicit (lines 994–995): "native evolution work is justified by demonstrated limits of the CLI bridge rather than by speculative parity concerns" and "any expanded plugin or analyzer scope is documented as a new support contract, not assumed retroactively."

If type-mapping extensions are later justified, they can be added without bridge-contract changes. Full resolver parity is a native-runtime decision, not a plugin decision.

---

## 4. Consolidated Recommendations

| Axis | Recommendation | Timing | Justification |
|------|---------------|--------|---------------|
| **Bridge-contract v2** | Not justified | — | v1 covers all proven scenarios. Forward-compatible handshake exists. No latency data yet demands incremental reload. |
| **Security: env-var projection** | Justified | Now | Trivial utility, no contract expansion, already in proposal design (line 317). Labeled as "environment preparation," not `varlock run` parity. |
| **Security: MEL redaction provider** | Not justified now | Later | Valid future `Varlock.Logging` package. Requires demonstrated demand. Must not claim JS auto-redaction parity. |
| **Security: ASP.NET leak scanning** | Not justified now | Later | Valid future `Varlock.AspNetCore` package. Body-buffering cost and limited transport coverage make it opt-in middleware, not "prevention." |
| **Security: `varlock scan` in .NET** | Not justified | — | Requires a .NET parser. Remains a CLI workflow. |
| **Plugin: C# type mappings** | Not justified now | Later | No demonstrated demand. Can be added without bridge v2 when needed. |
| **Plugin: diagnostics observer** | Not justified now | Later | No demonstrated demand. Can be added without bridge v2 when needed. |
| **Plugin: full resolver parity** | Not justified | — | This is a native-runtime decision. Multi-month scope. Maintenance parity risk. Must be justified by E1 evidence, not speculation. |

### Go/no-go input for Picard

- **Native-runtime go/no-go:** Tuvok's contract analysis finds no contract-level reason to move beyond the CLI bridge. E1 latency data is the remaining input. If latency is acceptable, the bridge is sufficient.
- **Plugin expansion go/no-go:** Not justified. No contract changes needed. Defer until demonstrated limits.
- **Security expansion go/no-go:** One small justified item (env-var projection). All others deferred until demand is demonstrated.

---

## Appendix: Evidence References

| Claim | Evidence |
|-------|----------|
| Bridge handshake forward-compatibility | `VarlockCliRuntime.cs` lines 230–300: probe sends v0, validates response contains `supportedContractVersion` |
| 7 error categories exhaustive | `VarlockBridgeErrorCategory.cs`: enum with 7 values; `ParseFailureCategory` (lines 423–435) maps string→enum with `BridgeInternalError` as default |
| Serilog redaction scope | `VarlockSerilogExtensions.cs`: `IDestructuringPolicy` implementation; `VarlockSerilogExtensionsTests.cs`: 5 test cases proving exact-match, non-intercept, metadata-only |
| Manual redaction scope | `VarlockRedactionHelper.cs`: single static method; console proof in `test-dotnet-proof.ts` asserts both redacted and raw output |
| `PreventLeaks` metadata-only | `VarlockResolvedGraph.cs` line 29: `bool PreventLeaks { get; }` — no consuming code exists in any .NET package |
| Plugin transparency through bridge | Plugin-resolved values are indistinguishable from normal values in `graph.config`; `PluginLoadFailed` fixture in `BridgeContractAlignmentTests.cs` |
| JS console patching mechanism | `packages/varlock/src/runtime/patch-console.ts`: hooks `kWriteToConsole` internal symbol |
| JS HTTP response patching mechanism | `packages/varlock/src/runtime/patch-server-response.ts`: patches `http.ServerResponse.prototype.write()` and `.end()` |
| Proposal exit criteria for Phase 4 | Lines 992–995: "justified by demonstrated limits," "documented as a new support contract" |
| Proposal security behavior scope | Lines 636–653: explicit unsupported behaviors, Serilog-only, metadata-only `PreventLeaks` |
