# `.NET` Phase 4 DoD Gap Analysis

**Initiative:** `dotnet-support`  
**Node:** `P4-A1` / `E4`  
**Mode:** audit only — no implementation changes proposed in this document

## Scope and evidence

This audit classifies every Definition-of-Done bullet in `docs/proposals/dotnet-support.md` lines 1001-1209 using only current repository evidence.

Primary evidence sources:

- `docs/proposals/dotnet-support.md` (`proposal`)
- `docs/proposals/dotnet-support-ledger.yml` (`ledger`)
- `scripts/test-dotnet-proof.ts` (`proof`)
- `packages/dotnet/Varlock.DotNet.Tests/` (`tests`)
- `packages/dotnet/*/README.md` (`package READMEs`)
- `.github/workflows/test.yaml` (`CI`)
- `.squad/decisions/inbox/picard-p4-a1-design-review.md` (`P4-A1 design review`)
- `.squad/identity/now.md`

Status legend:

- ✅ **Complete** — already proven or documented strongly enough in the current repo
- 📝 **Documentation-only** — implementation/proof exists, but publishable user-facing documentation is still missing or too thin
- 🔍 **Depends on P4-A1 evaluation** — status depends on the active Phase 4 evaluation outcomes
- ⏳ **Deferred beyond v1** — explicitly outside the current v1 contract or reserved for a later phase

## Audit summary

### Totals

| Status | Count |
| --- | ---: |
| ✅ Complete | 113 |
| 📝 Documentation-only | 28 |
| 🔍 Depends on P4-A1 evaluation | 15 |
| ⏳ Deferred beyond v1 | 6 |

### Section summary

| DoD section | ✅ | 📝 | 🔍 | ⏳ | Main remaining issue |
| --- | ---: | ---: | ---: | ---: | --- |
| 1. Product and behavior definition | 9 | 1 | 1 | 0 | `dotnet watch` / IDE intersections and offline executable docs |
| 2. Package surface | 6 | 1 | 1 | 2 | `Varlock.SourceGeneration` decision; diagnostics workflow docs |
| 3. Configuration provider behavior | 13 | 0 | 0 | 0 | none |
| 4. `IOptions<T>` integration | 7 | 1 | 0 | 0 | `ValidateOnStart()` guidance |
| 5. Type generation | 10 | 1 | 0 | 0 | publish naming-rule docs |
| 6. MSBuild integration | 7 | 0 | 4 | 1 | `dotnet watch` / IDE-build behavior; validate-on-build is deferred |
| 7. Logging and redaction | 10 | 0 | 0 | 0 | none |
| 8. Plugin behavior | 1 | 0 | 4 | 2 | positive plugin proof and plugin-scope decision |
| 9. Example applications | 14 | 0 | 1 | 0 | plugin-backed example remains unproven |
| 10. Test coverage | 16 | 0 | 1 | 1 | plugin positive path unresolved; build validation deferred |
| 11. Cross-platform CI | 6 | 0 | 0 | 0 | none |
| 12. Documentation | 1 | 13 | 1 | 0 | no publishable `.NET` doc set yet |
| 13. Developer experience | 1 | 7 | 1 | 0 | onboarding and troubleshooting docs |
| 14. Distribution and release | 1 | 4 | 0 | 0 | public package/release/acquisition docs |
| 15. Repository hygiene | 7 | 0 | 0 | 0 | none |
| 16. Upstream readiness | 4 | 0 | 1 | 0 | open questions need explicit P4-A1 disposition |

## Section-by-section audit

### 1. Product and behavior definition is complete

- ✅ `1003` The official product stance is documented clearly: Varlock coexists with appsettings by default and can become the primary configuration source when a team chooses.  
  Proposal text and the example set consistently use coexistence rather than replacement.
- ✅ `1004` The default precedence model is documented and implemented: appsettings loads first, Varlock loads after it, and Varlock wins when keys overlap unless users explicitly opt into a different order.  
  Proven by the proposal, the ASP.NET/Functions examples, and `proof` assertions.
- ✅ `1005` The supported app-type matrix is documented with explicit notes about full support, partial support, and special constraints.  
  The support-matrix ledger is explicit and example-linked.
- ✅ `1006` The support-boundary model is documented clearly in terms of engine parity, `.NET`-native parity, and explicitly deferred JavaScript-specific parity.  
  Proposal boundary language is explicit and matches the current bridge contract.
- 🔍 `1007` The likely developer-experience intersections are documented explicitly, including minimal APIs, `dotnet watch`, User Secrets coexistence, IDE-driven builds/debugging, Azure Functions isolated local workflow, and legacy migration guidance where claimed.  
  User Secrets and Functions coexistence are proven, but `dotnet watch` / IDE-build behavior is still a planned ledger row and part of active P4-A1 evaluation.
- ✅ `1008` Blazor WebAssembly support is documented honestly as public-config-only while using the CLI bridge.  
  Proposal wording and the WASM proof specimen match this boundary.
- ✅ `1009` The initial CLI-bridge architecture is documented as intentional, not accidental, including the criteria that would justify a future native runtime.  
  Proposal exit criteria and the P4-A1 design review both frame the bridge as intentional.
- ✅ `1010` Unsupported parity gaps versus JavaScript-specific runtime integrations are called out explicitly.  
  The proposal explicitly distinguishes supported `.NET` behavior from deferred JS-only runtime features.
- ✅ `1011` The current Varlock product surfaces are mapped explicitly to `.NET` v1 statuses so `load`, `typegen`, plugin-backed resolution, `run`, `scan`, and JavaScript-only runtime behaviors are not left ambiguous.  
  The proposal and ledger map these surfaces to proven/planned/deferred states.
- ✅ `1012` The machine-readable CLI contract consumed by the `.NET` bridge is documented clearly enough to support long-term maintenance.  
  The proposal, shared bridge fixtures, and bridge-alignment tests give a stable maintenance contract.
- 📝 `1013` The executable acquisition and version-compatibility story is documented clearly enough for local development, CI, and offline/restricted environments.  
  Local-development and CI lookup order are proven; an offline/restricted-environment setup story is still proposal-only and not published as user-facing docs.

### 2. Package surface is complete and coherent

- ✅ `1017` `Varlock.DotNet` exists and exposes a stable low-level runtime bridge API.  
  The package exists and the runtime surface is exercised by examples and tests.
- ✅ `1018` `Varlock.Extensions.Configuration` exists and integrates with `IConfigurationBuilder` using standard .NET patterns.  
  Package, source, tests, and hosted examples all prove this.
- ✅ `1019` `Varlock.Extensions.Hosting` exists and provides clean host-builder helpers for Generic Host scenarios without introducing a second configuration path.  
  Package and hosting tests prove the supported helper path.
- 🔍 `1020` `Varlock.SourceGeneration` exists in at least the initial CLI-generated form, with a clear evolution path to richer analyzer/source-generator support.  
  The P4-A1 design review explicitly calls this out as an evaluation item: either create the package or justify why current MSBuild integration satisfies the intent.
- ✅ `1021` `Varlock.MSBuild` exists and provides build integration without requiring users to hand-roll targets.  
  Package contents, targets/props, and `proof` all confirm this.
- ✅ `1022` `Varlock.Serilog` exists and provides the documented Serilog destructuring-redaction and metadata-enrichment APIs.  
  Package, README, tests, and proofs align on `WithVarlockRedaction()` and `WithVarlockMetadata()`.
- ⏳ `1023` If child-process environment injection is claimed as supported, `Varlock.DotNet` exposes it as an explicit low-level API without presenting it as full `varlock run` parity.  
  `varlock run` parity is not part of the current v1 contract; this stays deferred unless later claimed.
- ⏳ `1024` Any `.NET` plugin package introduced is clearly marked supported, preview, or experimental.  
  No `.NET` plugin package exists in v1.
- 📝 `1025` A supported diagnostics or inspection workflow exists for debugging Varlock-backed `.NET` loads.  
  The bridge exceptions and fixtures exist, but the canonical user-facing troubleshooting workflow is not yet documented outside the proposal.
- ✅ `1026` Package names, namespaces, versioning, and dependency directions are consistent and defensible for upstream review.  
  The current package set is coherent and follows a sensible dependency direction.

### 3. Configuration provider behavior is fully implemented

- ✅ `1030` `AddVarlock()` works in standard configuration builder flows.  
  Proven by the provider package, examples, and hosted proofs.
- ✅ `1031` `SchemaPath`, `Optional`, `ReloadOnChange`, `EnvironmentName`, precedence settings, and reload failure behavior are implemented and documented.  
  Implemented in the provider/runtime surface and covered in proposal + tests.
- ✅ `1032` The provider performs a successful initial load when the schema is present and valid.  
  Proven by examples and startup tests.
- ✅ `1033` The provider behaves correctly when the schema is missing and `Optional = true`.  
  Covered by provider tests and reload tests.
- ✅ `1034` The provider fails predictably when the schema is missing and `Optional = false`.  
  Covered by startup behavior tests and bridge exceptions.
- ✅ `1035` The provider watches the correct active source set when `ReloadOnChange = true`.  
  Implemented in `VarlockConfigurationProvider` and exercised in reload tests.
- ✅ `1036` The active watch set is recomputed after successful reloads if imports or environment-specific sources change.  
  The provider recomputes watchers from the new graph after successful reload.
- ✅ `1037` Reloads are debounced sufficiently to avoid pathological repeated executions during file save bursts.  
  Debounce behavior is explicit in provider code and tested.
- ✅ `1038` Failed reloads do not replace active configuration with invalid or partial data.  
  Last-known-good behavior is proven in tests and hosted proof modes.
- ✅ `1039` Successful reloads update the provider atomically.  
  The provider swaps `Data` before notifying consumers and tests cover this.
- ✅ `1040` Configuration consumers see stable, predictable behavior during reload boundaries.  
  `IOptionsMonitor<T>` / `IOptionsSnapshot<T>` proofs demonstrate the expected boundaries.
- ✅ `1041` Provider behavior preserves sensitive metadata and other serialized settings required by the supported `.NET` experience.  
  The bridge graph preserves metadata used by the supported security-boundary specimens.
- ✅ `1042` Provider diagnostics include enough source identity to debug precedence and import-related behavior.  
  The bridge graph exposes sources and the error-contract fixtures preserve actionable location data.

### 4. `IOptions<T>` integration is complete

- ✅ `1046` Standard `IOptions<T>` binding works with user-authored options classes.  
  Proven in the ASP.NET example and hosted proof.
- ✅ `1047` Standard `IOptions<T>` binding works with generated Varlock C# types where applicable.  
  The C# generation specimen is binder-oriented and the proof harness checks generated-type compatibility.
- ✅ `1048` `IOptionsSnapshot<T>` reflects the latest successful configuration state per scope/request.  
  Proven by the snapshot proof.
- ✅ `1049` `IOptionsMonitor<T>` reflects the latest successful configuration state for long-lived consumers.  
  Proven by the worker and ASP.NET reload proofs.
- ✅ `1050` `IOptionsMonitor<T>.OnChange(...)` fires only after successful reloads.  
  Explicitly asserted in hosted proof modes.
- ✅ `1051` Failed reloads do not corrupt or regress `CurrentValue` for `IOptionsMonitor<T>`.  
  Explicitly asserted in hosted proof modes.
- ✅ `1052` Official examples demonstrate recommended usage for `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>`.  
  The ASP.NET and worker examples collectively cover all three patterns.
- 📝 `1053` Guidance explicitly covers when to use `ValidateOnStart()`.  
  The proposal says this guidance should exist, but no publishable `.NET` docs currently provide it.

### 5. Type generation is complete enough for first-class support

- ✅ `1057` Varlock supports generating C# types through the existing type-generation flow.  
  Proven by CLI tests, MSBuild integration, and example builds.
- ✅ `1058` `@generateTypes(lang=cs, ...)` is implemented and documented.  
  The generator exists, the proposal documents it, and proofs exercise it.
- ✅ `1059` Generated C# is valid for supported target frameworks.  
  Example builds and type-generation proofs validate emitted C#.
- ✅ `1060` Generated C# favors binder-friendly POCOs and clean interop with standard .NET patterns.  
  The design and proof specimen are explicitly binder-oriented.
- ✅ `1061` Sensitive and non-sensitive metadata is preserved where intended by the design.  
  Proof coverage includes metadata preservation and WASM public-only exclusion.
- 📝 `1062` Generated type naming rules are documented and consistently applied.  
  The generation specimen exercises naming behavior, but the publishable naming-rule documentation is not finished.
- ✅ `1063` Generated output paths are safe for MSBuild and repository hygiene.  
  Recommended output is under `obj/Varlock/`, not committed source.
- ✅ `1064` Generated code lands in build output directories for normal project flows unless the user explicitly chooses another path.  
  The MSBuild package defaults to intermediate output.
- ✅ `1065` C# generation preserves the existing deterministic schema-driven model rather than depending on resolved environment values.  
  This is both implementation reality and proposal intent.
- ✅ `1066` Imported-schema behavior and `auto=false` behavior are documented and tested consistently with the underlying Varlock type-generation model.  
  `auto=false` is covered in the proof harness and imported/decorator behavior is covered in the underlying Varlock tests/docs.
- ✅ `1067` The C# generation specimen exists in-repo and is used as a regression artifact for naming, structure, and binder compatibility.  
  The checked-in proof specimen serves that role today.

### 6. MSBuild integration is complete

- ✅ `1071` A supported MSBuild package exists.  
  `Varlock.MSBuild` exists and packs its props/targets.
- ✅ `1072` It can generate C# types automatically during build.  
  The targets invoke `varlock typegen` during `dotnet build`.
- ⏳ `1073` It can validate schema during build when enabled.  
  `VarlockValidateOnBuild` is explicitly reserved for follow-on work; the current package does not implement a separate validation pass.
- ✅ `1074` It uses incremental inputs and outputs correctly enough to avoid needless rebuild churn.  
  The targets declare `Inputs` and `Outputs` and the proposal/ledger treat normal build incrementality as proven.
- ✅ `1075` Build failures are surfaced as normal MSBuild diagnostics with actionable messages.  
  The targets emit ordinary MSBuild errors for missing schema, missing executable, and missing generated output.
- ✅ `1076` The integration works in standard command-line builds.  
  `proof` builds all checked-in examples.
- 🔍 `1077` The integration works in IDE-driven builds to the extent required for mainstream .NET workflows.  
  Design-time/IDE behavior is one of the explicit P4-A1 evaluation areas.
- 🔍 `1078` The integration behaves predictably under `dotnet watch` and does not create pathological rebuild or regeneration loops in supported scenarios.  
  The support ledger still marks `dotnet watch` behavior as planned.
- 🔍 `1079` The interaction between runtime reloads and generated-file updates is documented clearly enough that users can predict whether a change triggers provider reload, rebuild, or both.  
  This depends on the active `dotnet watch` / IDE evaluation.
- ✅ `1080` The integration does not require users to manually edit temporary generated files.  
  Generated output lives under build output and is managed by MSBuild.
- ✅ `1081` Generated artifacts are not committed accidentally as part of the recommended workflow.  
  The recommended path uses intermediate-output locations.
- 🔍 `1082` The watch and reload specimen proves the documented behavior under repeated file changes rather than relying only on unit tests.  
  That specimen is still one of the planned rows identified by the support ledger.

### 7. Logging and redaction support is complete

- ✅ `1086` Serilog integration exists as a supported package.  
  `Varlock.Serilog` exists.
- ✅ `1087` The supported Serilog scope is explicit: `WithVarlockRedaction(graph)` performs Serilog-specific destructuring redaction for exact, case-sensitive sensitive-key matches only, uses the literal `[REDACTED]`, and `WithVarlockMetadata(graph)` enriches `VarlockRedactLogs` only.  
  Package README, tests, proofs, and proposal now align exactly on this contract.
- ✅ `1088` Scalar message-template parameters and non-Serilog pipelines are documented as outside that redaction path.  
  Proposal and tests explicitly prove the non-coverage.
- ✅ `1089` Example applications demonstrate the hosted Serilog specimen and the separate non-Serilog manual helper specimen.  
  ASP.NET and console proofs now cover both paths.
- ✅ `1090` Reload-aware Serilog re-registration is documented as deferred; the v1 policy uses the graph snapshot captured when the logger is configured.  
  Proposal language is explicit about this deferral.
- ✅ `1091` The status of non-Serilog redaction and leak-prevention behavior is documented explicitly as supported, unsupported, or deferred.  
  The security-boundary section is now explicit.
- ✅ `1092` The docs state explicitly that repository/file scanning remains an existing CLI workflow in v1 rather than a `.NET` runtime feature.  
  Proposal wording is explicit here.
- ✅ `1093` If `PreventLeaks` metadata is exposed through the bridge, its supported `.NET` meaning is documented as metadata only and not enforced by the runtime packages.  
  The proposal and proof payloads treat it as metadata only.
- ✅ `1094` If repository/file scanning is not part of v1, that deferral is documented plainly.  
  Proposal wording is explicit.
- ✅ `1095` The security-boundary specimen demonstrates the supported Serilog story, the non-Serilog fallback story, and the Blazor public-only boundary.  
  `proof` now covers all three.

### 8. Plugin behavior is clearly supported

- 🔍 `1099` Existing Varlock CLI plugin behavior works through the CLI bridge for supported scenarios.  
  Failure-path fixtures exist, but the positive plugin-backed load path is still a planned support-matrix row.
- 🔍 `1100` Documentation explains what “plugin support” means in the CLI-bridge model.  
  The scope is not yet publishable because the positive supported layout/proof is still unsettled.
- ⏳ `1101` If any `.NET`-native plugin hooks are introduced, they are documented with explicit scope boundaries.  
  `.NET`-native plugin hooks are outside v1.
- ⏳ `1102` No experimental `.NET` plugin mechanism is presented as equivalent to full native Varlock engine parity unless it truly is.  
  No `.NET`-native plugin mechanism is part of the current v1 story.
- 🔍 `1103` Supported plugin packaging and discovery modes are documented.  
  The proposal still treats the exact supported layout as an open proof/evaluation item.
- ✅ `1104` Plugin loading failures are surfaced with actionable diagnostics.  
  Bridge-fixture tests prove `PluginLoadFailed` handling.
- 🔍 `1105` The executable distribution specimen proves at least one plugin-backed load in the exact supported package layout.  
  The support ledger still marks plugin-backed secret resolution as planned.

### 9. Example applications prove the support claims

- ✅ `1109` Minimal example projects exist for the agreed support matrix.  
  The seven checked-in example apps cover the current claim set.
- ✅ `1110` Each example is runnable without local, undocumented hand steps.  
  `proof` runs the examples directly.
- ✅ `1111` Each example is intentionally small and focused on proving specific behavior.  
  The examples are proof slices, not broad sample apps.
- ✅ `1112` Console and ASP.NET Core MVC examples exist and are working (Phase 1 proven slice); WinForms and other wider platform targets are deferred to `P3-A1`.  
  Phase 3 closed these wider targets and the proof harness covers them.
- ✅ `1113` Worker Service, Azure Functions isolated worker, Blazor Server, and Blazor WebAssembly public-config examples exist and are working before the initiative is called complete.  
  All are present and proven.
- ✅ `1114` Example apps demonstrate precedence with appsettings.  
  ASP.NET and Functions proofs cover this.
- ✅ `1115` Example apps demonstrate coexistence with other common `.NET` configuration layers where the docs claim coexistence, such as User Secrets or `local.settings.json` in the relevant app types.  
  The ASP.NET and Functions examples prove those coexistence stories.
- ✅ `1116` Example apps demonstrate typed access.  
  Hosted proof modes exercise typed binding.
- ✅ `1117` Example apps demonstrate validation behavior.  
  The hosted proof set and bridge-alignment coverage demonstrate the supported failure behavior.
- ✅ `1118` Example apps demonstrate `Optional` behavior where relevant.  
  The provider behavior is covered in tests and aligned with the supported example set.
- ✅ `1119` Example apps demonstrate `ReloadOnChange` behavior where relevant.  
  ASP.NET and worker proofs explicitly cover successful and failed reload behavior.
- ✅ `1120` At least one example demonstrates direct non-hosted loading without `IConfiguration` if that scenario is claimed as supported.  
  The console and WinForms examples do this.
- 🔍 `1121` At least one example demonstrates plugin-backed secret resolution if plugin support is claimed in user-facing docs.  
  No positive plugin-backed example is checked in yet.
- ✅ `1122` Example apps do not include unnecessary fork-only scaffolding or unfinished exploratory code.  
  Current examples are narrowly scoped proof artifacts.
- ✅ `1123` The support-matrix ledger links each claimed example-driven behavior to its corresponding example project.  
  That mapping is explicit.

### 10. Test coverage spans the entire support contract

- ✅ `1127` Automated tests cover the CLI bridge load path.  
  Covered by bridge-runtime tests and example proofs.
- ✅ `1128` Automated tests cover configuration provider startup behavior.  
  Covered by provider and hosting tests.
- ✅ `1129` Automated tests cover `Optional = true` and `Optional = false` semantics.  
  Covered by startup/reload tests.
- ✅ `1130` Automated tests cover `ReloadOnChange = true` success and failure flows.  
  Covered by reload tests and hosted proof modes.
- ✅ `1131` Automated tests cover atomic reload semantics.  
  Covered by provider tests/proof.
- ✅ `1132` Automated tests cover `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` behavior.  
  Covered by hosted proofs and tests.
- ✅ `1133` Automated tests cover generated C# output validity and representative schema shapes.  
  Covered by generator tests and the `.NET` proof harness.
- ⏳ `1134` Automated tests cover MSBuild integration sufficiently to catch regressions in generation and validation flows.  
  Generation is covered; a dedicated validate-on-build flow is explicitly deferred along with `VarlockValidateOnBuild`.
- ✅ `1135` Automated tests cover machine-readable error contract behavior in addition to success payload behavior.  
  Covered by shared bridge fixtures.
- ✅ `1136` Automated tests cover executable discovery, override-path handling, and executable version-mismatch failures.  
  Covered by bridge-alignment tests and proof harness branches.
- ✅ `1137` Automated tests cover watch/reload coalescing and last-known-good guarantees under repeated file changes.  
  Covered by reload tests and hosted proof modes.
- ✅ `1138` Automated tests cover Serilog-specific destructuring redaction behavior.  
  Covered by Serilog tests and proof modes.
- ✅ `1139` Example-app smoke tests exist and run in CI for the supported matrix that is claimed in docs.  
  CI runs `bun run proof:dotnet` on all three OSes.
- ✅ `1140` Automated tests cover the machine-readable CLI contract consumed by the `.NET` bridge.  
  Covered by bridge-alignment tests.
- 🔍 `1141` Automated tests cover plugin-backed load scenarios if plugin support is claimed.  
  Positive plugin-backed loading remains a planned proof row.
- ✅ `1142` Automated tests cover representative schema and resolution diagnostics.  
  Covered by bridge-fixture tests.
- ✅ `1143` Automated tests cover imported-schema and `auto=false` type-generation semantics.  
  Covered by the underlying Varlock tests plus the `.NET` proof specimen.
- ✅ `1144` Automated tests or golden fixtures cover the machine-readable contract examples referenced by the proposal.  
  Shared fixtures already do this.

### 11. Cross-platform support claims are proven in CI

- ✅ `1148` CI validates supported `.NET` examples and packages on Windows.  
  The matrix includes `windows-latest`.
- ✅ `1149` CI validates supported `.NET` examples and packages on Linux.  
  The matrix includes `ubuntu-latest`.
- ✅ `1150` CI validates supported `.NET` examples and packages on macOS where applicable.  
  The matrix includes `macos-latest`.
- ✅ `1151` CI covers at least one modern target such as `net8.0`.  
  This is the primary target across examples.
- ✅ `1152` CI covers at least one legacy Windows desktop target such as `net48` if that remains part of the support claim.  
  The WinForms `net48` proof remains part of the Windows slice.
- ✅ `1153` CI failures are actionable and attributable to the `.NET` support work rather than unowned ad hoc scripts.  
  The proof workflow is checked in and purpose-built.

### 12. Documentation is complete and publishable

- 📝 `1157` User-facing docs explain the `.NET` story without ambiguity.  
  The proposal exists, but there are no publishable `.NET` website docs yet.
- 📝 `1158` Docs explain the coexistence model with appsettings and when to prefer each layer.  
  This is described in the proposal, not in user-facing docs.
- 📝 `1159` Docs explain default precedence and how to change it if customization is supported.  
  Proposal-only today.
- 📝 `1160` Docs explain `Optional`, `ReloadOnChange`, and reload failure semantics.  
  Proposal + tests exist; user-facing docs do not.
- 📝 `1161` Docs explain `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` usage.  
  Example behavior is proven, but publishable guidance is still missing.
- 📝 `1162` Docs explain how C# type generation works.  
  The behavior exists, but the `.NET` doc set does not.
- 📝 `1163` Docs explain how MSBuild integration works.  
  `Varlock.MSBuild/README.md` helps, but there is no complete user-facing guide.
- 📝 `1164` Docs explain Serilog integration.  
  `Varlock.Serilog/README.md` exists, but the broader `.NET` docs set is still missing.
- 📝 `1165` Docs explain the machine-readable CLI bridge contract and supported inspection workflows.  
  Maintenance material exists in the proposal/tests; publishable docs do not.
- 📝 `1166` Docs explain what current Varlock security behaviors are included, deferred, or unsupported in `.NET` v1.  
  The proposal now does this, but it has not been turned into end-user docs.
- 🔍 `1167` Docs explain what plugin support means operationally for `.NET` consumers.  
  This depends on the active plugin-scope evaluation and positive supported-layout proof.
- 📝 `1168` Docs explain platform caveats, especially Blazor WebAssembly.  
  Proposal-only today.
- 📝 `1169` Docs include migration guidance for teams already using appsettings, DotEnv, or `ConfigurationManager.AppSettings`.  
  This guidance is still missing as publishable docs.
- ✅ `1170` Docs are organized in stable locations suitable for upstream merge and future maintenance.  
  Proposal and package README locations are sensible and stable.
- 📝 `1171` Docs link to the executable distribution specimen, machine-readable contract fixtures, support-matrix ledger, and relevant proving examples.  
  Cross-linking for a user-facing doc set is not in place yet.

### 13. Developer experience is acceptable

- 📝 `1175` A new `.NET` user can get a working example running with documented steps only.  
  The examples run, but there is no finished getting-started doc path.
- 📝 `1176` A user can add Varlock to an existing appsettings-based app without having to abandon existing configuration classes immediately.  
  The implementation supports this, but the migration/adoption docs are not written.
- 📝 `1177` A user can understand how Varlock should coexist with User Secrets, `local.settings.json`, and legacy configuration layers in the scenarios claimed by docs.  
  Proven in examples; not yet documented as a polished user journey.
- ✅ `1178` Errors during load, validation, and MSBuild generation are understandable and actionable.  
  Current bridge exceptions and MSBuild errors are actionable.
- 🔍 `1179` IDE and build outputs point users toward the correct schema or configuration problems.  
  Build outputs are good today, but IDE/design-time behavior is still under active P4-A1 evaluation.
- 📝 `1180` The required setup for the CLI bridge is explicit and not surprising.  
  The runtime behavior exists, but the onboarding docs are not explicit enough yet.
- 📝 `1181` The support story does not rely on users discovering hidden environment assumptions.  
  This still needs executable-acquisition/offline documentation to be truly publishable.
- 📝 `1182` The distinction between provider-based usage, direct runtime usage, and build-time generation usage is documented clearly.  
  The repo implements all three paths, but the docs are not consolidated.
- 📝 `1183` The supported debugging path for failed loads is obvious from docs and error messages.  
  The errors are decent; the docs are not yet obvious.

### 14. Distribution and release story is ready

- 📝 `1187` Each `.NET` package has a clear publishing strategy.  
  The packages are packable, but the public publishing strategy is not documented.
- 📝 `1188` The project implements and validates a supported story for how the `.NET` packages locate or obtain the `varlock` executable.  
  Lookup behavior is implemented and tested, but the publishable acquisition story is incomplete, especially for offline/restricted environments.
- 📝 `1189` Package metadata, README files, and versioning are ready for public consumption.  
  The package shells exist, but several README files are still extremely thin.
- 📝 `1190` Release steps are documented well enough that upstream maintainers can ship the packages without fork-specific tribal knowledge.  
  This remains a documentation gap.
- ✅ `1191` Any new CI or release workflow changes are minimal, reviewable, and aligned with the existing monorepo release model.  
  The current workflow additions are straightforward.

### 15. Repository hygiene and upstream mergeability are preserved

- ✅ `1195` Design documents live under `docs/proposals/` or another agreed permanent documentation location.  
  This is already true.
- ✅ `1196` Temporary notes, scratch code, or fork-only artifacts are not committed.  
  The current proof/doc lane is clean enough to review upstream.
- ✅ `1197` Example projects are added only when intentionally named, runnable, and scoped.  
  The example set fits this description.
- ✅ `1198` Generated artifacts are excluded from source control where appropriate.  
  Recommended generation paths use intermediate-output locations.
- ✅ `1199` Directory layout for `.NET` work is consistent with the rest of the monorepo.  
  The package/example layout is coherent.
- ✅ `1200` No package, example, or workflow names are fork-specific.  
  Current naming is neutral.
- ✅ `1201` The resulting diff set can be reviewed and merged upstream without requiring a cleanup PR first.  
  The Phase 1-3 output is already organized like an upstreamable package slice.

### 16. Upstream readiness is explicit

- ✅ `1205` The final set of proposals, package boundaries, and behavior decisions is understandable without requiring chat history.  
  The proposal, ledger, package readmes, and decisions are enough to understand the current shape.
- 🔍 `1206` Open questions are either resolved or intentionally deferred with clear rationale.  
  Some are still active P4-A1 evaluation questions: `Varlock.SourceGeneration`, `dotnet watch` / IDE behavior, and plugin scope/layout.
- ✅ `1207` Unsupported scenarios are called out plainly rather than implied.  
  The proposal is explicit about deferred/non-supported areas.
- ✅ `1208` The implementation can be delivered incrementally in reviewable PRs without invalidating the high-level design.  
  Phases 1-3 already demonstrated that incremental shape.
- ✅ `1209` Maintainers can tell from the repository itself, not just external discussion, what “done” means for `.NET` support.  
  The DoD, support ledger, proof harness, and decisions make that visible.

## Critical path for a first-class `.NET` support claim

The core bridge/provider/example/test/CI slice is already strong enough that the remaining blockers are mostly **scope decisions** and **documentation publication**, not broad product-code uncertainty.

### Blockers that are not yet complete

1. **`Varlock.SourceGeneration` disposition must be settled** (`1020`).  
   Picard explicitly called this a P4-A1 item. The repo either needs:
   - a minimal `Varlock.SourceGeneration` package wrapping the current CLI-generated flow, or
   - an accepted rationale that the existing MSBuild integration already satisfies the DoD intent.

2. **`dotnet watch` / IDE-build behavior must be evaluated and written down** (`1007`, `1077`-`1082`, `1179`, `1206`).  
   The support ledger still marks `dotnet watch` behavior as `planned`, and the active Geordi deliverable is supposed to document this interaction.

3. **Plugin-backed loading scope must be settled** (`1099`-`1105`, `1121`, `1141`, `1167`).  
   Current repo evidence proves plugin failure handling, but not a positive plugin-backed `.NET` load in a documented supported layout. If plugin-backed loading remains part of the v1 claim, a precise support boundary and proof artifact are still needed.

4. **The documentation-only gap is too large to wave away** (`1013`, `1025`, `1053`, most of section 12, most of section 13, most of section 14`).  
   The repo has a proposal and package shells, but not a publishable `.NET` doc set. There are no `.NET` pages under `packages/varlock-website/src/content/docs/` today.

### Items that should not block v1 if Picard accepts the current deferral

These are real DoD items, but current repo evidence already treats them as out of v1 scope rather than unfinished v1 product work:

- `1073` / `1134` — separate validate-on-build support via `VarlockValidateOnBuild`
- `1023` — any low-level child-process environment injection story presented as `varlock run` parity
- `1024`, `1101`, `1102` — `.NET`-native plugin packages or hooks

## Documentation-only gap estimate

### Size

**Large enough to justify a dedicated `P4-B1` docs batch.**

Why:

- 28 DoD bullets classify as documentation-only.
- The current repo has **no publishable `.NET` website docs**.
- Several package README files are still package stubs rather than end-user guides.
- The missing docs are not one README pass; they span onboarding, runtime usage, hosted usage, type generation, MSBuild, security boundaries, diagnostics, migration, and release guidance.

### Likely `P4-B1` deliverables

A realistic docs batch would need, at minimum:

1. **Getting started / install guide**  
   setup, executable acquisition, local dev vs CI, supported lookup order
2. **Configuration provider + coexistence guide**  
   precedence, appsettings coexistence, User Secrets / Functions caveats, `Optional`, reload semantics
3. **Typed options guide**  
   `IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`, and `ValidateOnStart()` guidance
4. **Type generation + MSBuild guide**  
   `@generateTypes(lang=cs, ...)`, output paths, binder story, `auto=false`, build integration, current `dotnet watch` caveats
5. **Security-boundary / logging guide**  
   Serilog scope, manual helper scope, non-Serilog limitations, `PreventLeaks` metadata-only meaning, WASM public-only boundary
6. **Diagnostics / troubleshooting guide**  
   error categories, inspection workflow, precedence debugging, executable mismatch debugging
7. **Migration guide**  
   appsettings coexistence, DotEnv migration, `ConfigurationManager.AppSettings` notes where still claimed
8. **README expansion for published packages**  
   especially `Varlock.DotNet`, `Varlock.Extensions.Configuration`, and `Varlock.Extensions.Hosting`

## Bottom line

Phase 3 appears to have closed the **implementation/proof** lane honestly. The remaining path to an honest “first-class `.NET` support” claim is now mostly:

1. finish the active **P4-A1 evaluation decisions** (`Varlock.SourceGeneration`, `dotnet watch` / IDE behavior, plugin scope), and
2. ship a real **P4-B1 documentation batch** large enough to turn the current proposal + proof assets into publishable `.NET` guidance.

No recommendation in this document requires new product code during P4-A1.
