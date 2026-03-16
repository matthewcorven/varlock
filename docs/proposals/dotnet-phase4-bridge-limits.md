# .NET Phase 4: CLI bridge limits and native-runtime recommendation

## Scope

This note is Phase 4 evaluation-only evidence for the current `.NET` CLI bridge. It does **not** change product code, the support matrix, or the Phase 3 contract. Its job is to answer three questions:

1. what latency does the current bridge actually add
2. what concrete things can the bridge not do that a native `.NET` runtime could do
3. whether those limits justify native-runtime work now

The current bridge model is still the one documented in `docs/proposals/dotnet-support.md`: `VarlockCliRuntime.Load()` shells out to the Varlock CLI, performs a bridge-contract handshake, then performs the real `load --format json-full` command and parses the returned graph (`packages/dotnet/Varlock.DotNet/VarlockCliRuntime.cs:24-64,139-173`).

## Evidence scope

- **Measured environment:** local Darwin/macOS only (`macOS 26.3.1`, `.NET 10.0.0`, `Arm64`)
- **Not measured here:** Linux CI runner latency, Windows CI runner latency
- **Why:** this task was executed from a local macOS environment only; there is no checked-in benchmark lane that would let this doc honestly claim cross-OS timing numbers

That means the numbers below are **real measurements**, but only for the current local macOS host. They should be treated as operational evidence for this host, not as invented cross-platform averages.

## Methodology

I used a temporary out-of-repo benchmark harness against the current implementation to time three boundaries:

1. **Handshake only**: the bridge-contract probe path used by `Load()`
2. **Load process to parsed graph**: child-process launch through `ParseCliOutput(...)`
3. **Public `VarlockCliRuntime.Load()`**: handshake + real load together
4. **Reload end-to-end**: schema file write to `IConfiguration` reload notification using the current `VarlockConfigurationProvider`

Reload timing used the shipping provider behavior, including the fixed 300 ms debounce window (`packages/dotnet/Varlock.Extensions.Configuration/VarlockConfigurationProvider.cs:13-18,217-239`) and the current success-path reload implementation (`packages/dotnet/Varlock.Extensions.Configuration/VarlockConfigurationProvider.cs:242-269`).

Two schema sizes were measured:

- **small**: console-example-sized schema, 4 resolved items, 195 bytes
- **medium**: synthetic medium schema, 52 resolved items, 2434 bytes

Each startup case ran 15 iterations. Each reload case ran 8 iterations.

## Measured latency

### Cold start / initial load

`VarlockCliRuntime.Load()` currently performs **two** child-process executions on the happy path: handshake, then real load (`packages/dotnet/Varlock.DotNet/VarlockCliRuntime.cs:24-64`). Each execution is synchronous and blocks on full stdout/stderr reads before exit (`packages/dotnet/Varlock.DotNet/VarlockCliRuntime.cs:857-879`).

| Schema | Resolved items | Handshake median / p95 | Load-to-graph median / p95 | Public `Load()` median / p95 |
| --- | ---: | ---: | ---: | ---: |
| small | 4 | 79.44 ms / 87.91 ms | 83.79 ms / 92.11 ms | 164.15 ms / 179.81 ms |
| medium | 52 | 80.10 ms / 87.09 ms | 87.71 ms / 93.05 ms | 166.84 ms / 179.36 ms |

### Reload on change

The provider coalesces file-system events into a single reload cycle with a fixed 300 ms debounce, then does a full `_runtime.Load(...)`, full flatten, atomic `Data` swap, watcher recompute, and only then calls `OnReload()` (`packages/dotnet/Varlock.Extensions.Configuration/VarlockConfigurationProvider.cs:217-269`).

| Schema | Fixed debounce | End-to-end median / p95 | Bridge load inside reload median / p95 | Residual after debounce median / p95 |
| --- | ---: | ---: | ---: | ---: |
| small | 300 ms | 547.46 ms / 549.65 ms | 192.45 ms / 198.82 ms | 51.99 ms / 55.97 ms |
| medium | 300 ms | 550.47 ms / 556.09 ms | 198.30 ms / 206.38 ms | 52.10 ms / 52.60 ms |

## What the measurements say

### 1. The bridge is process-bound more than schema-bound at current sizes

Going from 4 resolved items to 52 resolved items changed median public `Load()` time by only ~2.7 ms and median reload time by only ~3.0 ms. For the current proof-sized workloads, the cost is dominated by process launch / IPC overhead, not by schema size.

### 2. The current startup floor is roughly 160-180 ms per runtime load on this host

For the current implementation, a user-visible `VarlockCliRuntime.Load()` call has a local median around **164-167 ms** and a local p95 around **179-180 ms**. That is the cost floor before any app-specific options binding or additional host startup work.

### 3. The current reload floor is effectively about 0.52-0.56 s on this host

The fixed debounce consumes 300 ms by design, and the reload path then pays another ~192-198 ms for the full bridge load plus ~52 ms of provider bookkeeping/notification overhead. In practice, successful reloads landed at roughly **547-550 ms median** on this machine.

### 4. Sub-300 ms successful reloads are not realistic without changing the design

Even if bridge execution itself became free, the provider already spends 300 ms in debounce by default. With the current design, anything like “config reload should feel near-instant” would require design changes even before discussing a native parser.

## Concrete bridge limits

These are the current practical limits of the CLI bridge design.

### A. Every public runtime load pays two child-process launches

`Load()` always does:

1. bridge-contract handshake
2. real `load --format json-full`

That behavior is fixed in the current implementation (`packages/dotnet/Varlock.DotNet/VarlockCliRuntime.cs:24-64,139-173`). A native runtime could collapse that to in-process work with no child-process boundary.

### B. Every successful reload is a full-graph reload

The provider does not perform partial invalidation or diff-based updates. On each successful reload it calls `_runtime.Load(_loadOptions)`, flattens the whole graph again, swaps the full `Data` dictionary, recomputes the watch set, and then fires `OnReload()` (`packages/dotnet/Varlock.Extensions.Configuration/VarlockConfigurationProvider.cs:242-269`).

A native runtime could potentially support in-process caching, finer-grained invalidation, or incremental recomputation. The current bridge cannot.

### C. The bridge requires a runnable JS CLI host

Executable discovery currently resolves local package/bin wrappers and development CLI scripts (`packages/dotnet/Varlock.DotNet/VarlockCliRuntime.cs:640-742,795-817`). On Windows, if the resolved executable is `.js`, the runtime explicitly launches `node` (`packages/dotnet/Varlock.DotNet/VarlockCliRuntime.cs:586-612`).

That means the bridge story depends on the JavaScript CLI distribution model. A native `.NET` runtime could remove that dependency and ship as a pure NuGet/in-process experience.

### D. The bridge does not expose in-process schema/model introspection APIs

The runtime contract is just `Load(...)` / `LoadAsync(...)` returning a `VarlockResolvedGraph` (`packages/dotnet/Varlock.DotNet/IVarlockRuntime.cs:6-10`). The returned graph exposes resolved items, sources, and a small set of flags/base-path metadata (`packages/dotnet/Varlock.DotNet/VarlockResolvedGraph.cs:6-34`).

That is enough for the current configuration-loading story, but not for richer in-process features like:

- incremental invalidation
- AST/schema inspection
- Roslyn-style tooling
- request-time diagnostics or design-time exploration without a CLI round-trip

A native runtime could expose those APIs directly.

### E. Child-process-free hosts stay outside the runtime model

The current support doc already calls out that Blazor WebAssembly cannot use the same runtime model with a CLI bridge and therefore stays on a **build-time public-config-only** workflow (`docs/proposals/dotnet-support.md:154-160`). That is an honest current boundary, not a bug.

Still, it is a real bridge limit: anything that cannot launch child processes is outside the current runtime architecture. A native runtime could remove the process-launch blocker, though separate security/product decisions would still be required before expanding support there.

## Capability-gap inventory and materiality

| Gap | Bridge evidence | What a native runtime could add | Materiality now |
| --- | --- | --- | --- |
| Full-graph reload on every change | Provider always reloads/flattens/swaps the whole graph (`VarlockConfigurationProvider.cs:242-269`) | incremental caching, diff-based recompute, cheaper reloads | **Moderate** for reload-heavy dev loops; **low** for current proof-sized startup flows |
| No in-process schema introspection | `IVarlockRuntime` only loads graphs; `VarlockResolvedGraph` is a resolved snapshot (`IVarlockRuntime.cs:6-10`, `VarlockResolvedGraph.cs:6-34`) | schema/AST APIs, analyzers, design-time tooling, richer diagnostics | **Low now**, because current proof slice only needs resolved config; **high later** if tooling becomes a goal |
| JS/Node-based host dependency | current executable resolution targets CLI scripts/wrappers and may invoke `node` (`VarlockCliRuntime.cs:586-612,640-742,795-817`) | pure NuGet / in-process deployment story | **Conditional**: not a blocker in the repo’s current proof environments, but real friction for locked-down `.NET`-only hosts |
| Process spawn on every load | public `Load()` shells out twice and blocks synchronously (`VarlockCliRuntime.cs:24-64,857-879`) | lower startup latency, no handshake/load spawn tax | **Observable but modest** at current scale: ~164-167 ms median startup load |
| No runtime story for child-process-free hosts | current support matrix keeps Blazor WASM at build-time public-only (`dotnet-support.md:154-160`) | possible future in-process story where security permits | **Not material to current support claims**, because the public-only boundary is already explicit and proven |

## Materiality against the currently proven `.NET` slice

`bun run proof:dotnet` currently proves:

- direct runtime loading for the console example
- hosted configuration-provider usage for ASP.NET MVC, worker service, Azure Functions isolated, and Blazor Server
- legacy non-hosted WinForms bridge usage on Windows
- Blazor WebAssembly public-only generation boundary
- the narrow Serilog/manual-redaction security boundary

See `scripts/test-dotnet-proof.ts:731-840` and the broader proof/support summary in `docs/proposals/dotnet-support.md:788-807,846,982-983`.

Against that proof slice, the measured bridge limits land like this:

- **Console / WinForms direct runtime startup:** the extra ~165 ms median load is noticeable but acceptable for startup-only use.
- **ASP.NET MVC / Worker / Functions / Blazor Server startup:** same story; the bridge cost is visible but not severe relative to the rest of host startup.
- **Reload-on-change:** this is the one place where the bridge is meaningfully felt. A roughly 0.55 s successful reload loop is fine for occasional config edits, but it is not “instant” and would become annoying if the product later leans heavily on high-frequency config editing.
- **Blazor WebAssembly:** the bridge is already intentionally not the runtime story. A native runtime could open new possibilities, but that is not required to satisfy the current documented support claim.

So the bridge has **real architectural limits**, but only one of them is currently user-visible in the proven slice: reload latency.

## Recommendation

### Recommendation: do **not** start native `.NET` runtime implementation yet

The current evidence does **not** justify native-runtime work now.

Why:

1. the current proven `.NET` support slice is startup-centric, and the measured startup tax is modest and stable on this host
2. the one clearly user-visible pain point is reload latency, but even there the current 300 ms debounce means part of the problem is provider design, not just “bridge vs native”
3. the other major bridge limits (schema introspection, pure NuGet/no-Node deployment, child-process-free hosts) are real, but they are not currently blocking the documented and proven support matrix

### Re-open native-runtime work if any of these become product requirements

- sub-300 ms successful end-to-end reload targets
- a pure `.NET` / no-JS-host deployment requirement
- in-process schema/AST/tooling APIs
- support expansion into hosts where child processes are unavailable or unacceptable

If any of those become committed requirements, a native runtime becomes much easier to justify. With the current support contract and current measured evidence, it does not.
