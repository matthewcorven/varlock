# SKILL: Hosting JS CLI Entrypoints from .NET

**Version:** 1.0  
**Author:** Data (Bridge/Hosting Lead)  
**Context:** `.NET` process launch for Node-based CLIs  
**Last Updated:** 2026-03-16

## Purpose

When a `.NET` runtime discovers a JavaScript CLI entrypoint and launches child processes with `UseShellExecute = false`, especially on Windows, keep discovery semantics honest while making execution actually work.

## Pattern

1. **Resolve the CLI path semantically first**  
   Decide whether the winner is explicit config, package-local install layout, local `.bin`, repo-local development path, or `PATH`. Do not change lookup order just to accommodate one platform's launcher behavior.

2. **Treat launch hosting as a separate concern**  
   If the resolved executable ends in `.js` on Windows, launch:

   ```csharp
   FileName = "node";
   Arguments = QuoteArgument(resolvedCliPath) + " " + existingArguments;
   ```

   Keep `UseShellExecute = false` so stdout/stderr capture and failure handling stay consistent.

3. **Run non-JS targets directly**  
   `.cmd`, `.bat`, `.exe`, and normal Unix executables should keep their existing direct-launch behavior unless a separate platform bug proves otherwise.

4. **Test the real fallback path, not only a helper**  
   Add a regression test that:
   - omits explicit executable configuration
   - creates the repo-local or package-local `cli.js`
   - exercises the actual handshake + load flow
   - uses a path containing spaces so argument quoting is validated

5. **Keep bridge semantics unchanged at the boundary**  
   The runtime should still report the same Varlock executable path as the semantic winner; only the process-host command changes on Windows.

## When to Use

- `.NET` wrappers around Node-based CLIs
- Hosting layers that capture stdout/stderr and must keep `UseShellExecute = false`
- Cross-platform bridge work where Unix shebang execution masks a Windows-specific failure

## Varlock Example

- Resolved executable: `packages/varlock/bin/cli.js`
- Windows launch: `node "packages/varlock/bin/cli.js" load ...`
- Validation: `BridgeContractAlignmentTests.Load_executes_repo_local_js_entrypoint_without_explicit_executable_path` plus `bun run proof:dotnet`
