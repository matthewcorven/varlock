# Varlock Repository Architecture

This document is a deep technical map of the repository as it exists today. It is intended for contributors who need to understand how the monorepo is organized, how development flows through it, and how Varlock behaves at runtime.

This document intentionally focuses on the current TypeScript, Bun, Turbo, Astro, Vite, VS Code, plugin, and binary build architecture in this repository. It does not cover the separate .NET proposal.

## Why This Repository Exists

Varlock adds declarative schema and runtime semantics to `.env` files using `@env-spec` decorator comments. The product spans multiple concerns:

- a language and parser for `@env-spec`
- a runtime engine that loads, validates, coerces, and resolves configuration
- framework integrations that adapt the runtime to common JavaScript toolchains
- plugin packages that teach Varlock how to talk to external secret systems
- tooling around docs, editor support, CI, smoke tests, binary distribution, and release automation

At a high level, the repository is not organized around a single executable package. It is organized around a pipeline:

1. Parse an `@env-spec` file into an AST.
2. Convert that AST into an environment graph made of sources, items, decorators, and resolvers.
3. Resolve the graph into concrete values plus metadata.
4. Serialize the result into a runtime-friendly representation.
5. Inject the resolved state into CLI output, child processes, application runtimes, framework builds, and security tooling.

That pipeline is the most important mental model for understanding the codebase.

## Monorepo Overview

The monorepo is managed with Bun workspaces and Turborepo.

Root orchestration lives in:

- `package.json`
- `turbo.json`
- `bunfig.toml`

The most important root scripts are:

- `bun run build` - build everything except the smoke test fixtures
- `bun run build:libs` - build all publishable libraries except the website
- `bun run dev` - start watch/dev tasks across packages in parallel
- `bun run test:ci` - run the normal test suites once
- `bun run smoke-test` - run end-to-end smoke tests from the fixture workspace
- `bun run check` - lint + build libraries + test
- `bun run lint:fix` - repository-wide auto-fix pass

Turbo is configured so package builds depend on upstream package builds. The repository leans on simple package-local scripts and uses Turbo mainly for orchestration, dependency ordering, and caching.

## Package Map

### Core packages

#### `packages/varlock`

This is the core product package.

It contains:

- the CLI executable
- the env graph engine
- schema processing and validation
- resolver execution
- type generation
- runtime injection
- log redaction and leak scanning
- plugin loading
- Bun-compiled binary build logic

If you only read one package, read this one.

#### `packages/env-spec-parser`

This is the language front-end for `@env-spec`.

It contains:

- the PEG grammar in `grammar.peggy`
- generated parser output
- AST and helper classes
- utilities for updating env-spec files programmatically

This package does not know how values are resolved at runtime. It stops at syntax, structure, and AST-level manipulation.

#### `packages/utils`

This is an internal shared utility package used by parser and runtime packages. It is intentionally small and unopinionated.

#### `packages/ci-env-info`

This package normalizes CI provider environment metadata. Varlock uses it to support builtin CI-aware variables and to make config behavior less provider-specific.

### Integration packages

#### `packages/integrations/vite`

This is the most substantial framework integration.

It does three important jobs:

- load and reload Varlock state during Vite config/build/dev cycles
- perform static replacement of safe `ENV.X` reads in source code
- inject runtime bootstrap code into SSR and client entry points

This package is the best place to study how Varlock adapts itself to a framework build tool.

#### `packages/integrations/astro`

This package is a thin Astro integration layered on top of the Vite integration. It mainly selects sensible SSR injection behavior based on the active Astro adapter.

#### `packages/integrations/nextjs`

This package takes a different approach than Astro and Vite. Instead of acting like a Vite plugin, it acts as a compatibility replacement for `@next/env` so it can fit naturally into Next.js startup and reload behavior.

### Plugin packages

The plugin packages provide support for external secret sources and other runtime extensions:

- `packages/plugins/1password`
- `packages/plugins/aws-secrets`
- `packages/plugins/azure-key-vault`
- `packages/plugins/bitwarden`
- `packages/plugins/google-secret-manager`
- `packages/plugins/infisical`
- `packages/plugins/pass`

These are not independent runtimes. They are loaded by the Varlock plugin host and register new data types, root decorators, item decorators, and resolver functions.

### Tooling and product-surface packages

#### `packages/varlock-website`

Astro/Starlight documentation site. It also serves as a real dogfooding surface because it depends on Varlock and the Astro integration.

#### `packages/vscode-plugin`

VS Code language support for `@env-spec` files. It is a separate contributor surface with its own release and packaging flow.

#### `packages/varlock-docs-mcp`

An MCP server that exposes Varlock docs over HTTP/SSE, implemented for Cloudflare-style deployment.

#### `packages/changeset-changelog`

Release infrastructure package used by Changesets.

### Test and fixture workspace

#### `smoke-tests`

This is a separate fixture workspace used for end-to-end smoke testing. It intentionally behaves more like a consumer project than an internal test directory.

## Dependency Shape

The conceptual dependency graph looks like this:

```mermaid
flowchart TD
  subgraph Core[Core Runtime]
    Parser[@env-spec/parser]
    Utils[@env-spec/utils]
    CIEnv[@varlock/ci-env-info]
    Varlock[varlock]
  end

  subgraph Integrations[Framework Integrations]
    Vite[@varlock/vite-integration]
    Astro[@varlock/astro-integration]
    Next[@varlock/nextjs-integration]
  end

  subgraph Plugins[Plugin Packages]
    P1[@varlock/1password-plugin]
    P2[@varlock/aws-secrets-plugin]
    P3[@varlock/azure-key-vault-plugin]
    P4[@varlock/bitwarden-plugin]
    P5[@varlock/google-secret-manager-plugin]
    P6[@varlock/infisical-plugin]
    P7[@varlock/pass-plugin]
  end

  subgraph Tooling[Tooling + Product Surfaces]
    VSCode[env-spec-language]
    Website[@varlock/website]
    DocsMCP[varlock-docs-mcp]
    Smoke[smoke-tests]
  end

  Utils --> Parser
  Utils --> Varlock
  CIEnv --> Varlock
  Parser --> Varlock

  Varlock --> Vite
  Vite --> Astro
  Varlock --> Next

  Varlock --> P1
  Varlock --> P2
  Varlock --> P3
  Varlock --> P4
  Varlock --> P5
  Varlock --> P6
  Varlock --> P7

  Varlock --> Website
  Astro --> Website

  Parser -. language semantics .-> VSCode
  Varlock -. docs and product knowledge .-> DocsMCP
  Varlock --> Smoke
  Next --> Smoke
```

The main architectural point is that almost everything important either feeds `varlock` or consumes it.

## The Core Runtime Pipeline

The runtime pipeline starts in the parser and ends in injected runtime state.

### 1. Parsing

Parsing starts in `packages/env-spec-parser/src/index.ts`, which exports `parseEnvSpecDotEnvFile(source)`.

The grammar lives in `packages/env-spec-parser/grammar.peggy`. It parses:

- config items
- comment blocks
- root decorators
- item decorators
- bare values
- function-call syntax inside values and decorators
- multiline strings

The parser emits AST-like classes such as:

- `ParsedEnvSpecFile`
- `ParsedEnvSpecConfigItem`
- `ParsedEnvSpecDecorator`
- `ParsedEnvSpecFunctionCall`
- `ParsedEnvSpecKeyValuePair`

Locations are preserved on parsed nodes. That location tracking is later used to produce useful schema and resolution errors with file/line context.

### 2. Graph loading

The main graph loading entrypoint is `packages/varlock/src/lib/load-graph.ts`, which delegates to `loadEnvGraph()` in `packages/varlock/src/env-graph/lib/loader.ts`.

This step:

- chooses a root source based on an explicit path or current working directory
- creates an `EnvGraph`
- attaches a root data source
- finishes loading the graph

At this point the system is still building structure, not resolving values.

### 3. Data sources

Data sources are the file-system-facing layer. The implementation lives in `packages/varlock/src/env-graph/lib/data-source.ts`.

Data sources model things like:

- a root directory containing `.env` files
- a single explicit env file
- imported child files
- env-specific sources enabled by the active environment
- partially imported files with key filtering

Important behaviors handled at this layer:

- discovering auto-loaded env files
- processing `@currentEnv` and `@envFlag`
- processing `@import`
- respecting `@disable`
- allowing root-level default behaviors like `@defaultSensitive`

This is where the file system turns into graph nodes.

### 4. Config items and decorators

Each config key becomes a `ConfigItem`. The graph keeps them in `configSchema`.

Decorators are split between:

- root decorators
- item decorators

Built-in root decorators cover things like:

- current environment selection
- imports
- type generation
- redaction settings
- leak-prevention settings

Built-in item decorators cover things like:

- type
- required/optional
- sensitive/public
- defaults/examples/placeholders
- validation metadata

The graph registers decorator definitions, data types, and resolver functions at construction time, and plugins can add to each registry.

### 5. Resolver graph

The resolver system lives in `packages/varlock/src/env-graph/lib/resolver.ts`.

Resolvers are the execution model for values. A value is not just a string; it may be:

- static text
- a function call
- a reference to another config item
- a composed value
- an external command result
- a plugin-defined secret fetch

Each resolver knows:

- its arguments
- whether it is static
- what keys it depends on
- its inferred type
- schema errors found during processing

The graph computes dependency relationships, checks for cycles, and resolves values in dependency order.

### 6. Resolution

Actual value resolution happens in `EnvGraph.resolveEnvValues()` in `packages/varlock/src/env-graph/lib/env-graph.ts`.

This stage:

- chooses keys to resolve
- walks dependencies
- resolves prerequisite items first
- runs root decorators that require resolved inputs
- captures final resolved values
- preserves `isSensitive` metadata for runtime behavior

After resolution, the graph can expose multiple views:

- plain resolved env object
- serialized graph
- per-item summaries for CLI output

### 7. Serialization and runtime injection

Once resolved, the graph can be serialized into a structure containing:

- enabled sources
- runtime settings
- key/value entries
- sensitivity flags

That serialized structure is stored in `__VARLOCK_ENV` and consumed by `packages/varlock/src/runtime/env.ts`.

Runtime initialization does three main things:

- populate the `ENV` proxy used by app code
- re-inject values into `process.env`
- build redaction state for sensitive values

The runtime layer is what bridges the abstract graph into normal application execution.

## The CLI Mental Model

The CLI entrypoint is `packages/varlock/bin/cli.js`, which imports `packages/varlock/src/cli/cli-executable.ts`.

The CLI uses `gunshi` and sets up a command table with lazy-loaded command implementations.

This is important because the CLI package is not one monolithic command runner. It is a set of distinct product surfaces.

### `varlock load`

Implemented in `packages/varlock/src/cli/commands/load.command.ts`.

Primary purpose:

- resolve config
- validate config
- print config in human- or machine-readable form

This is the inspection and debugging command.

It is also the canonical non-framework path used by other layers when they need a stable serialized output.

### `varlock run`

Implemented in `packages/varlock/src/cli/commands/run.command.ts`.

Primary purpose:

- resolve env
- inject it into a child process
- optionally redact stdout/stderr
- propagate child exit behavior

This is the process-wrapper mode. It is important both for user workflows and for integrations that need a stable bridge.

### `varlock init`

Implemented in `packages/varlock/src/cli/commands/init.command.ts`.

Primary purpose:

- scan existing `.env` files
- bootstrap a `.env.schema`
- infer some useful decorators and defaults
- install `varlock` into a JS project if possible
- disable Bun's native env loading by adding `env = false` to `bunfig.toml`

This command is heavily about developer experience rather than runtime semantics.

### `varlock scan`

Implemented in `packages/varlock/src/cli/commands/scan.command.ts`.

Primary purpose:

- resolve the actual sensitive values
- search the working tree or staged files for plaintext leaks
- support git hook installation patterns

This command is a major part of the repository's security story.

### `varlock typegen`

Implemented in `packages/varlock/src/cli/commands/typegen.command.ts`.

Primary purpose:

- generate types from schema metadata without requiring an environment-specific resolved execution path

The key design choice is that type generation uses stable schema information, not mutable per-environment runtime results.

## Why The Integrations Look Different

The integrations reflect differences in framework lifecycle rather than stylistic inconsistency.

### Vite integration

The Vite integration is the most general framework adapter.

It does several jobs:

- load Varlock state as Vite boots
- reload state in dev mode
- expose build-time-safe replacements for non-sensitive values
- inject SSR bootstrap code
- inject client-side dev-only metadata for better missing-key errors
- register loaded env sources as Vite config dependencies so file changes trigger rebuilds

This package effectively turns Varlock into part of the Vite compilation model.

### Astro integration

Astro is intentionally thin. Astro already sits on Vite, so the Astro package mainly selects or infers the correct SSR injection mode and installs the Vite plugin into the Astro configuration.

This means the real behavioral complexity for Astro still lives in the Vite integration.

### Next.js integration

Next.js does not fit the same plugin model. Its integration instead behaves like a replacement for `@next/env` and mirrors Next's expectations.

This integration handles:

- environment inference compatible with `next dev` and `next build`
- caching and forced reload behavior
- source display compatible with what Next expects to log
- `.env.schema` watch behavior
- special platform-oriented resolved env file output for platforms that boot Next in non-standard ways

The Next integration is a good example of the codebase preferring pragmatic compatibility over architectural purity.

## Runtime Safety, Redaction, And Leak Prevention

Sensitive data handling is not an afterthought in this repository. It is built into the runtime layer.

The main implementation lives in `packages/varlock/src/runtime/env.ts` and related patch modules:

- `patch-console.ts`
- `patch-response.ts`
- `patch-server-response.ts`

The rough flow is:

1. build a lookup of sensitive resolved strings
2. generate a maximal-munch regex for redaction
3. patch output paths so sensitive values are replaced before they leave the process

The runtime layer also exposes leak scanning helpers for places where blocking a leak is more important than merely redacting it.

One important practical consequence is that redaction can change TTY characteristics of child processes if stdout/stderr are piped through filters. That tradeoff shows up in both code and smoke-test documentation.

## Plugin Architecture

Plugin support is one of the most distinctive technical areas in the repository.

The plugin host lives in `packages/varlock/src/env-graph/lib/plugins.ts`.

Plugins are loaded through root decorators like `@plugin(...)` and register new capabilities onto the graph host.

### What plugins can register

Plugins can register:

- data types
- root decorators
- item decorators
- resolver functions

### How plugin code is written

Plugins target the surface exported by `packages/varlock/src/plugin-lib.ts`.

Instead of exporting a structured manifest, plugin modules mutate a globally provided `plugin` object at module initialization time. That global object is a deliberate design choice because it avoids cross-bundle duplication issues and keeps plugin loading simple.

### Why the SEA-specific loader exists

Varlock ships compiled Bun binaries. In that environment, dynamic import of arbitrary external plugin files is not straightforward.

To make plugins still work in compiled binaries, the host includes a fallback loader that:

- reads the plugin file from disk
- rewrites top-level ESM imports to `require()` calls
- provides synthetic `import.meta`
- executes the transformed module in a `vm` context

That behavior is unusual, but it is central to how compiled binary support and plugin support coexist.

### Example plugin design pattern

The plugin packages commonly implement:

- an instance registry keyed by plugin instance id
- one or more init decorators such as `@initOp(...)`
- custom data types for auth tokens or secret references
- resolver functions that fetch values from the external system

The concrete implementations differ by provider, but the overall integration pattern is consistent.

## Developer Experience In Practice

The daily contributor workflow is relatively simple:

1. `bun install`
2. `bun run build:libs`
3. `bun run test:ci` or package-local tests
4. `bun run smoke-test` when end-to-end behavior matters
5. `bun run lint:fix` before wrapping up a change

### What is pleasant about the DX

- package scripts are direct and readable
- most packages have very little custom build machinery beyond `tsup`
- the graph/runtime boundary is explicit enough to reason about
- integrations are real packages, not hidden feature folders
- smoke tests are separated into a consumer-like workspace

### What is easy to miss

- Bun's own env loading must be disabled in consumer projects using Varlock
- some framework behaviors are implemented via a CLI bridge rather than in-process API calls
- the plugin host has non-obvious compiled-binary constraints
- type generation intentionally runs before full env resolution
- Next and Vite integrations solve similar product problems using different lifecycle hooks

## Why The CLI Bridge Shows Up Repeatedly

One recurring pattern in the codebase is that some integrations call back into the CLI synchronously instead of using a pure in-process library path.

This can look strange until you consider the constraints:

- framework boot order is often synchronous or quasi-synchronous
- build systems want deterministic, isolated startup steps
- child-process boundaries can be more robust than sharing mutable initialization state across toolchains

Examples include:

- `packages/varlock/src/auto-load.ts`
- Vite reload behavior
- Next.js integration reload behavior

The repository consistently prefers "reliable startup under real tool constraints" over theoretical elegance here.

## Type Generation Strategy

Type generation is an important design clue.

Varlock does not treat generated types as a side effect of fully resolved runtime state. Instead, it treats them as a projection of stable schema metadata.

This means:

- output is deterministic
- env-specific runtime state does not distort the generated API
- typegen can run separately from runtime execution

That design reduces surprise for contributors and users, especially in frameworks that run multiple build phases.

## Error Handling Philosophy

The repository puts a lot of effort into surfacing useful config errors rather than generic exceptions.

The main ingredients are:

- AST node location tracking from the parser
- graph-aware schema and resolution errors
- CLI wrappers that print readable messages with suggestions

This is one reason the parser and graph layers feel tightly coupled even though they live in different packages: parser location fidelity is directly consumed by the runtime error UX.

## Testing Strategy

The repository uses a layered testing model.

### Package tests

Most packages use Vitest for unit and integration-style coverage.

Examples:

- parser grammar and updater logic
- runtime helpers and graph logic
- package-local behavior

### Smoke tests

The `smoke-tests` workspace verifies end-to-end behavior from the perspective of a consumer project.

It is particularly useful because it tests:

- CLI behavior
- log redaction behavior
- framework integration behavior
- runtime compatibility
- compiled binary behavior

Notably, the smoke tests use a separate package-manager setup and fixture apps, which makes them closer to the real world than internal unit tests.

### Binary tests

Compiled binary behavior is important enough to have specific tests, especially around nested `varlock run` behavior and argument handling.

This reflects the fact that the binary is not a packaging afterthought. It is a first-class distribution target.

## Build And Release Strategy

### Library builds

Most packages use `tsup`.

Parser is special because its grammar generation step must happen before TypeScript/library build output.

### Binary builds

The CLI binary is built with `bun build --compile`, orchestrated by `packages/varlock/scripts/build-binaries.ts`.

There are two modes:

- dev build for the current platform
- release builds for multiple target platforms with archives and checksums

### CI

The normal test workflow does:

1. install dependencies
2. lint
3. build libraries
4. run tests

### Release workflows

Releases are split across workflows:

- Changesets and npm publishing
- preview package publishing
- compiled binary release publishing
- Docker image publishing
- Homebrew formula updates

The release story is deliberately multi-channel because the product is distributed in multiple ways.

## Website, Docs, And Editor Surfaces

These packages are not core runtime packages, but they matter for contributor orientation.

### Website

The docs site is an Astro/Starlight app that also functions as a dogfooding surface for the main product and its Astro integration.

### VS Code extension

The VS Code extension is a language support package. It does not reuse the main runtime engine directly, but it does rely on the same language concepts and file semantics.

### Docs MCP server

The docs MCP package is a product-adjacent service rather than a runtime dependency of Varlock itself. It belongs in the same repo because it represents an official product surface for AI tooling.

## Suggested Read Order For New Contributors

If you want to understand the repository efficiently, read in this order:

1. `packages/varlock/src/cli/cli-executable.ts`
2. `packages/varlock/src/cli/commands/load.command.ts`
3. `packages/varlock/src/lib/load-graph.ts`
4. `packages/varlock/src/env-graph/lib/loader.ts`
5. `packages/varlock/src/env-graph/lib/env-graph.ts`
6. `packages/varlock/src/env-graph/lib/data-source.ts`
7. `packages/varlock/src/env-graph/lib/resolver.ts`
8. `packages/varlock/src/runtime/env.ts`
9. `packages/env-spec-parser/grammar.peggy`
10. `packages/integrations/vite/src/index.ts`
11. `packages/integrations/nextjs/src/next-env-compat.ts`
12. one plugin implementation such as `packages/plugins/1password/src/plugin.ts`

That order mirrors the actual flow from syntax to runtime behavior.

## The Most Important Architectural Facts To Retain

If a contributor forgets everything else, these are the facts worth retaining:

1. The repository is organized around a parse -> graph -> resolve -> inject pipeline.
2. `packages/varlock` is the center of the system; most other packages either feed it or adapt it.
3. The parser package is a language front-end, not a runtime engine.
4. Data sources, decorators, and resolvers are the three most important internal concepts in the core runtime.
5. Integrations are lifecycle adapters, not thin wrappers around one universal hook.
6. Plugins are first-class runtime extensions and must also work with compiled Bun binaries.
7. Security behavior such as redaction and leak prevention is embedded in the runtime, not bolted on.
8. Smoke tests are essential because significant behavior only becomes visible under real toolchains and real process boundaries.
9. The binary distribution path matters enough to shape implementation details in core and plugin loading.
10. Developer experience choices in this repository are usually pragmatic responses to framework and tooling constraints, not accidental inconsistency.

## Opportunities For Future Contributors

A contributor who understands this document should be well positioned to work on:

- new resolver functions
- new decorators
- plugin improvements
- framework integrations
- type generation changes
- CLI ergonomics
- parser syntax changes
- error message improvements
- binary and distribution hardening

The safest way to make changes is usually:

1. identify whether the change belongs in parser, graph, runtime, integration, or plugin space
2. verify whether the behavior is schema-time, resolve-time, or runtime-output behavior
3. trace corresponding smoke tests and package tests
4. make the smallest change at the lowest correct layer

That approach aligns well with how the repository is already structured.
