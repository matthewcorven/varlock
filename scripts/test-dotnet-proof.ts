// @ts-nocheck

import fs from 'node:fs';
import { spawn, spawnSync } from 'node:child_process';
import { delimiter, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRepoTempDirSync } from '../packages/utils/src/repo-temp';

type CommandResult = {
  stdout: string;
  stderr: string;
  exitCode: number;
};

type ConsoleBaselineOutput = {
  appName: string;
  httpPort: string;
  featureEnabled: string;
  providerType: string;
  schemaPath: string;
};

type AspNetPayload = {
  AppName: string;
  AppSettingsOnly: string;
  UserSecretsOnly: string;
};

type WorkerPayload = {
  AppName: string;
  WorkerMessage: string;
};

type FunctionsPayload = {
  AppName: string;
  FunctionsOnlyKey?: string;
};

type BlazorPayload = {
  AppName: string;
  ComponentMessage: string;
};

type WinFormsPayload = {
  AppName: string;
  WindowTitle: string;
  SchemaSourcePresent: boolean;
};

type ExecutableHarness = {
  markerPath: string;
  cleanup: () => void;
};

type PathExecutableHarness = {
  executablePath: string;
  pathDirectory: string;
  cleanup: () => void;
};

type PackedPackage = {
  packagePath: string;
  packageSourceDir: string;
  version: string;
  cleanup: () => void;
};

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const isWindows = process.platform === 'win32';

function getBuildOutputPath(projectDir: string, assemblyName: string, targetFramework = 'net8.0', extension = 'dll') {
  return join(projectDir, 'bin', 'Debug', targetFramework, `${assemblyName}.${extension}`);
}

function runDotnet(projectDir: string, args: Array<string>, envOverrides: NodeJS.ProcessEnv = {}, timeoutMs?: number): CommandResult {
  const result = spawnSync('dotnet', args, {
    cwd: projectDir,
    encoding: 'utf-8',
    timeout: timeoutMs,
    env: {
      ...process.env,
      ...envOverrides,
    },
  });

  return {
    stdout: result.stdout ?? '',
    stderr: result.stderr ?? '',
    exitCode: result.status ?? (result.signal ? 0 : 1),
  };
}

function runBuiltAssembly(
  projectDir: string,
  assemblyName: string,
  targetFramework: string,
  args: Array<string>,
  envOverrides: NodeJS.ProcessEnv = {},
  timeoutMs?: number,
): CommandResult {
  return runDotnet(
    projectDir,
    [getBuildOutputPath(projectDir, assemblyName, targetFramework), ...args],
    envOverrides,
    timeoutMs,
  );
}

type WatchResult = {
  started: boolean;
  buildCount: number;
  rebuildCount: number;
  output: string;
};

function runDotnetWatch(projectDir: string, stabilityWindowMs = 6000, overallTimeoutMs = 60_000): Promise<WatchResult> {
  return new Promise((resolve) => {
    const child = spawn('dotnet', ['watch', 'run', '--no-launch-profile'], {
      cwd: projectDir,
      env: { ...process.env },
      stdio: ['ignore', 'pipe', 'pipe'],
    });

    let output = '';
    let started = false;
    let stabilityTimer: ReturnType<typeof setTimeout> | undefined;
    let resolved = false;

    const finish = (result: WatchResult) => {
      if (resolved) return;
      resolved = true;
      if (stabilityTimer) clearTimeout(stabilityTimer);
      child.kill('SIGTERM');
      // Give process a moment to exit after SIGTERM
      setTimeout(() => {
        try { child.kill('SIGKILL'); } catch {}
        resolve(result);
      }, 1000);
    };

    const overallTimer = setTimeout(() => {
      finish({ started, buildCount: 0, rebuildCount: 0, output: output + '\n[TIMEOUT]' });
    }, overallTimeoutMs);

    const onData = (chunk: Buffer) => {
      const text = chunk.toString();
      output += text;

      if (!started && (text.includes('Application started') || text.includes('Now listening on'))) {
        started = true;
        // Start stability window after detecting startup
        stabilityTimer = setTimeout(() => {
          clearTimeout(overallTimer);
          // Count rebuild indicators in output collected AFTER startup
          const postStartupOutput = output.slice(output.indexOf('Application started') !== -1
            ? output.indexOf('Application started')
            : output.indexOf('Now listening on'));
          const lines = postStartupOutput.split('\n');
          const rebuildLines = lines.filter(
            (l) => l.includes('Building...') || l.includes('Restarting the app') || l.includes('Hot reload'),
          );
          // Count initial builds from the full output
          const allLines = output.split('\n');
          const buildLines = allLines.filter((l) => l.includes('Building...'));
          finish({
            started: true,
            buildCount: buildLines.length,
            rebuildCount: rebuildLines.length,
            output,
          });
        }, stabilityWindowMs);
      }
    };

    child.stdout.on('data', onData);
    child.stderr.on('data', onData);

    child.on('error', () => {
      clearTimeout(overallTimer);
      finish({ started: false, buildCount: 0, rebuildCount: 0, output: output + '\n[PROCESS ERROR]' });
    });

    child.on('exit', () => {
      // If process exits before stability window completes, resolve with what we have
      if (!resolved) {
        clearTimeout(overallTimer);
        finish({ started, buildCount: 0, rebuildCount: 0, output });
      }
    });
  });
}

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) {
    throw new Error(message);
  }
}

function assertCommandSucceeded(name: string, result: CommandResult) {
  if (result.exitCode !== 0) {
    throw new Error([
      `${name} exited with code ${result.exitCode}.`,
      result.stdout,
      result.stderr,
    ].filter(Boolean).join('\n'));
  }
}

function parseJsonOutput<T>(name: string, result: CommandResult): T {
  assertCommandSucceeded(name, result);

  try {
    return JSON.parse(result.stdout.trim()) as T;
  } catch (error) {
    throw new Error([
      `${name} did not emit valid JSON.`,
      result.stdout,
      result.stderr,
      error instanceof Error ? error.message : String(error),
    ].filter(Boolean).join('\n'));
  }
}

function parseTaggedLines(stdout: string, prefixes: Array<string>): Map<string, string> {
  const lines = stdout.split('\n').map((l) => l.trim()).filter(Boolean);
  const result = new Map<string, string>();
  for (const line of lines) {
    const colonIndex = line.indexOf(':');
    if (colonIndex > 0 && prefixes.some((prefix) => line.startsWith(prefix))) {
      result.set(line.substring(0, colonIndex), line.substring(colonIndex + 1));
    }
  }

  return result;
}

function parseAssignmentLines(stdout: string, separator = ' = '): Map<string, string> {
  const lines = stdout.split('\n').map((line) => line.trim()).filter(Boolean);
  const result = new Map<string, string>();

  for (const line of lines) {
    const separatorIndex = line.indexOf(separator);
    if (separatorIndex > 0) {
      result.set(line.substring(0, separatorIndex), line.substring(separatorIndex + separator.length));
    }
  }

  return result;
}

function parseConsoleBaselineOutput(name: string, result: CommandResult): ConsoleBaselineOutput {
  assertCommandSucceeded(name, result);

  const lines = parseAssignmentLines(result.stdout);
  const requiredKeys = ['APP_NAME', 'HTTP_PORT', 'FEATURE_ENABLED', 'VARLOCK_PROVIDER', 'VARLOCK_SCHEMA_PATH'];
  for (const key of requiredKeys) {
    assert(
      lines.has(key),
      `${name} should emit a "${key} = ..." line.\n${result.stdout}\n${result.stderr}`,
    );
  }

  return {
    appName: lines.get('APP_NAME')!,
    httpPort: lines.get('HTTP_PORT')!,
    featureEnabled: lines.get('FEATURE_ENABLED')!,
    providerType: lines.get('VARLOCK_PROVIDER')!,
    schemaPath: lines.get('VARLOCK_SCHEMA_PATH')!,
  };
}

function assertConsoleBaselineOutput(output: ConsoleBaselineOutput, proofLabel: string) {
  assert(output.appName === 'varlock-console', `${proofLabel} should resolve APP_NAME from the default .env value file.`);
  assert(output.httpPort === '4310', `${proofLabel} should expose HTTP_PORT through IConfiguration.`);
  assert(output.featureEnabled === 'True', `${proofLabel} should expose FEATURE_ENABLED through IConfiguration after coercion.`);
  assert(output.providerType === 'VarlockConfigurationProvider', `${proofLabel} should wire in the Varlock configuration provider.`);
  assert(output.schemaPath === '.env.schema', `${proofLabel} should report the default schema path.`);
}

function assertAspNetPayload(payload: AspNetPayload, proofLabel: string, expectedUserSecretsOnly = '') {
  assert(payload.AppName === 'varlock-mvc', `${proofLabel} should let Varlock override APP_NAME from appsettings.`);
  assert(
    payload.AppSettingsOnly === 'retained-from-appsettings',
    `${proofLabel} should keep unrelated appsettings keys intact.`,
  );
  assert(payload.UserSecretsOnly === expectedUserSecretsOnly, `${proofLabel} should preserve the expected User Secrets-only value.`);
}

function assertWorkerPayload(payload: WorkerPayload, proofLabel: string) {
  assert(payload.AppName === 'varlock-worker', `${proofLabel} should resolve APP_NAME from Varlock.`);
  assert(
    payload.WorkerMessage === 'BackgroundService received configuration from Varlock',
    `${proofLabel} should expose the worker-specific message from Varlock.`,
  );
}

function createExecutableHarness(
  projectDir: string,
  executableSegments: Array<string>,
  markerFileName: string,
): ExecutableHarness {
  const wrapperPath = join(projectDir, ...executableSegments);
  const markerPath = join(projectDir, markerFileName);
  const upstreamCliPath = join(repoRoot, 'packages', 'varlock', 'bin', 'cli.js');

  let wrapperSource: string;
  if (isWindows) {
    const escapedMarkerPath = markerPath.replaceAll('"', '""');
    wrapperSource = `@echo off
(
  echo package-local
) > "${escapedMarkerPath}"
node "${upstreamCliPath.replaceAll('"', '""')}" %*
`;
  } else {
    wrapperSource = `#!/usr/bin/env node
const fs = require('node:fs');
const { spawnSync } = require('node:child_process');

fs.writeFileSync(${JSON.stringify(markerPath)}, 'package-local\\n');

const result = spawnSync(process.execPath, [${JSON.stringify(upstreamCliPath)}, ...process.argv.slice(2)], {
  cwd: process.cwd(),
  env: process.env,
  stdio: 'inherit',
});

if (result.error) {
  throw result.error;
}

process.exit(result.status ?? 1);
`;
  }

  fs.mkdirSync(dirname(wrapperPath), { recursive: true });
  fs.writeFileSync(wrapperPath, wrapperSource, 'utf8');

  if (!isWindows) {
    fs.chmodSync(wrapperPath, 0o755);
  }

  return {
    markerPath,
    cleanup: () => {
      if (fs.existsSync(markerPath)) {
        fs.rmSync(markerPath);
      }

      const nodeModulesPath = join(projectDir, 'node_modules');
      if (fs.existsSync(nodeModulesPath)) {
        fs.rmSync(nodeModulesPath, { recursive: true, force: true });
      }
    },
  };
}

function createPathHarness(projectDir: string): PathExecutableHarness {
  const harnessRoot = join(projectDir, '.varlock-path-proof');
  const executableName = isWindows ? 'varlock.cmd' : 'varlock';
  const wrapperPath = join(harnessRoot, 'bin', executableName);
  const upstreamCliPath = join(repoRoot, 'packages', 'varlock', 'bin', 'cli.js');

  fs.mkdirSync(dirname(wrapperPath), { recursive: true });
  if (isWindows) {
    const wrapperSource = [
      '@echo off',
      `node "${upstreamCliPath.replaceAll('"', '""')}" %*`,
      'exit /b %ERRORLEVEL%',
      '',
    ].join('\r\n');

    fs.writeFileSync(wrapperPath, wrapperSource, 'utf8');
  } else {
    try {
      fs.symlinkSync(upstreamCliPath, wrapperPath);
    } catch (error) {
      if (
        error instanceof Error
        && 'code' in error
        && error.code === 'EEXIST'
      ) {
        fs.rmSync(wrapperPath, { force: true });
        fs.symlinkSync(upstreamCliPath, wrapperPath);
      } else {
        throw error;
      }
    }
  }

  return {
    executablePath: wrapperPath,
    pathDirectory: dirname(wrapperPath),
    cleanup: () => {
      if (fs.existsSync(harnessRoot)) {
        fs.rmSync(harnessRoot, { recursive: true, force: true });
      }
    },
  };
}

function createPackageLocalHarness(projectDir: string): ExecutableHarness {
  const segments = isWindows
    ? ['node_modules', 'varlock', 'bin', 'cli.cmd']
    : ['node_modules', 'varlock', 'bin', 'cli.js'];
  return createExecutableHarness(projectDir, segments, '.varlock-package-local-proof');
}

function createLocalBinHarness(projectDir: string): ExecutableHarness {
  const segments = isWindows
    ? ['node_modules', '.bin', 'varlock.cmd']
    : ['node_modules', '.bin', 'varlock'];
  return createExecutableHarness(projectDir, segments, '.varlock-local-bin-proof');
}

function setUserSecret(projectDir: string, key: string, value: string) {
  assertCommandSucceeded(
    `dotnet user-secrets set ${key}`,
    runDotnet(projectDir, ['user-secrets', 'set', key, value]),
  );
}

function clearUserSecrets(projectDir: string) {
  assertCommandSucceeded(
    'dotnet user-secrets clear',
    runDotnet(projectDir, ['user-secrets', 'clear']),
  );
}

function escapeXml(value: string) {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('"', '&quot;')
    .replaceAll('\'', '&apos;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');
}

function packVarlockMsbuildPackage(): PackedPackage {
  const tempRoot = createRepoTempDirSync('varlock-msbuild-pack-proof');
  const packageSourceDir = join(tempRoot, 'nupkgs');
  const packageVersion = `0.0.0-dxproof.${Date.now()}`;
  fs.mkdirSync(packageSourceDir, { recursive: true });

  const packageProjectDir = join(repoRoot, 'packages', 'dotnet', 'Varlock.MSBuild');
  assertCommandSucceeded(
    'dotnet pack Varlock.MSBuild',
    runDotnet(packageProjectDir, [
      'pack',
      'Varlock.MSBuild.csproj',
      '--nologo',
      '--verbosity',
      'quiet',
      '-p:PackageVersion=' + packageVersion,
      '--output',
      packageSourceDir,
    ]),
  );

  const packageFileName = fs.readdirSync(packageSourceDir).find((entry) => entry.startsWith('Varlock.MSBuild.')
    && entry.endsWith('.nupkg')
    && !entry.endsWith('.snupkg')
    && !entry.includes('.symbols.'));

  assert(
    packageFileName,
    'dotnet pack should produce a Varlock.MSBuild .nupkg file.',
  );

  return {
    packagePath: join(packageSourceDir, packageFileName!),
    packageSourceDir,
    version: packageVersion,
    cleanup: () => {
      if (fs.existsSync(tempRoot)) {
        fs.rmSync(tempRoot, { recursive: true, force: true });
      }
    },
  };
}

function createMsbuildPackageProofProject(packageVersion: string, packageSourceDir: string) {
  const projectDir = createRepoTempDirSync('varlock-msbuild-consumer-proof');
  const assemblyName = 'VarlockMsbuildPackageProof';
  const generatedFilePath = join(projectDir, 'obj', 'Varlock', 'AppConfig.g.cs');
  const cliPath = join(repoRoot, 'packages', 'varlock', 'bin', 'cli.js');

  const projectFileContent = `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <AssemblyName>${assemblyName}</AssemblyName>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RestoreSources>${escapeXml(packageSourceDir)}</RestoreSources>
    <VarlockSchemaPath>.env.schema</VarlockSchemaPath>
    <VarlockGeneratedFile>$(BaseIntermediateOutputPath)Varlock/AppConfig.g.cs</VarlockGeneratedFile>
    <VarlockExecutablePath>${escapeXml(cliPath)}</VarlockExecutablePath>
    <VarlockEnableLocalExecutableLookup>false</VarlockEnableLocalExecutableLookup>
    <VarlockEnablePathLookup>false</VarlockEnablePathLookup>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Varlock.MSBuild" Version="${packageVersion}" />
  </ItemGroup>
</Project>
`;

  const schemaFileContent = `# @defaultSensitive=false
# @generateTypes(lang=cs, path=obj/Varlock/AppConfig.g.cs, namespace=PackageProof.Generated, typeName=AppConfig, auto=false)
# ---

APP_NAME=package-proof

# @type=number(min=1024, max=65535)
APP_PORT=4312

# @type=boolean
FEATURE_ENABLED=true

# @sensitive
SECRET_TOKEN=proof-secret-value
`;

  const programFileContent = `Console.WriteLine("Varlock.MSBuild package proof");
`;

  fs.writeFileSync(join(projectDir, 'VarlockMsbuildPackageProof.csproj'), projectFileContent, 'utf8');
  fs.writeFileSync(join(projectDir, '.env.schema'), schemaFileContent, 'utf8');
  fs.writeFileSync(join(projectDir, 'Program.cs'), programFileContent, 'utf8');

  return {
    assemblyName,
    generatedFilePath,
    projectDir,
    cleanup: () => {
      if (fs.existsSync(projectDir)) {
        fs.rmSync(projectDir, { recursive: true, force: true });
      }
    },
  };
}

const consoleProjectDir = join(repoRoot, 'examples', 'dotnet-console');
const directLoadProjectDir = join(repoRoot, 'examples', 'dotnet-console-direct-load');
const sensitiveProjectDir = join(repoRoot, 'examples', 'dotnet-console-sensitive');
const reloadProjectDir = join(repoRoot, 'examples', 'dotnet-console-reload');
const serilogProjectDir = join(repoRoot, 'examples', 'dotnet-console-serilog');
const typedConfigProjectDir = join(repoRoot, 'examples', 'dotnet-console-typed-config');
const customSchemaPathProjectDir = join(repoRoot, 'examples', 'dotnet-console-custom-schema-path');
const customWorkingDirProjectDir = join(repoRoot, 'examples', 'dotnet-console-custom-working-dir');
const environmentNameProjectDir = join(repoRoot, 'examples', 'dotnet-console-environment-name');
const optionalProjectDir = join(repoRoot, 'examples', 'dotnet-console-optional');
const customRuntimeProjectDir = join(repoRoot, 'examples', 'dotnet-console-custom-runtime');
const coercionProjectDir = join(repoRoot, 'examples', 'dotnet-console-coercion');
const validationProjectDir = join(repoRoot, 'examples', 'dotnet-console-validation');
const publicOnlyProjectDir = join(repoRoot, 'examples', 'dotnet-console-public-only');
const execProjectDir = join(repoRoot, 'examples', 'dotnet-console-exec');
const compositionProjectDir = join(repoRoot, 'examples', 'dotnet-console-composition');
const diOptionsProjectDir = join(repoRoot, 'examples', 'dotnet-console-di-options');
const optionsSnapshotProjectDir = join(repoRoot, 'examples', 'dotnet-console-options-snapshot');
const optionsMonitorProjectDir = join(repoRoot, 'examples', 'dotnet-console-options-monitor');
const explicitExecutableProjectDir = join(repoRoot, 'examples', 'dotnet-console-explicit-executable');
const leakPreventionProjectDir = join(repoRoot, 'examples', 'dotnet-console-leak-prevention');
const workerProjectDir = join(repoRoot, 'examples', 'dotnet-worker');
const aspNetProjectDir = join(repoRoot, 'examples', 'dotnet-aspnet-mvc');
const functionsProjectDir = join(repoRoot, 'examples', 'dotnet-functions-isolated');
const blazorProjectDir = join(repoRoot, 'examples', 'dotnet-blazor-server');
const winFormsProjectDir = join(repoRoot, 'examples', 'dotnet-winforms');
const wasmProjectDir = join(repoRoot, 'examples', 'dotnet-blazor-wasm-public');
const wasmGeneratedTypeDir = join(wasmProjectDir, 'obj', 'Varlock');
const wasmGeneratedTypePath = join(wasmGeneratedTypeDir, 'VarlockPublicConfig.g.cs');
const publicOnlyGeneratedTypeDir = join(publicOnlyProjectDir, 'obj', 'Varlock');
const publicOnlyGeneratedTypePath = join(publicOnlyGeneratedTypeDir, 'VarlockPublicConfig.g.cs');

const packedMsbuildPackage = packVarlockMsbuildPackage();
try {
  assert(
    fs.existsSync(packedMsbuildPackage.packagePath),
    'proof:dotnet should pack Varlock.MSBuild into a real .nupkg before validating package consumption.',
  );

  const packageProofProject = createMsbuildPackageProofProject(
    packedMsbuildPackage.version,
    packedMsbuildPackage.packageSourceDir,
  );

  try {
    assertCommandSucceeded(
      'dotnet restore VarlockMsbuildPackageProof',
      runDotnet(packageProofProject.projectDir, ['restore', '--nologo', '--verbosity', 'quiet']),
    );

    assertCommandSucceeded(
      'dotnet msbuild /t:VarlockGenerateTypes VarlockMsbuildPackageProof',
      runDotnet(packageProofProject.projectDir, ['msbuild', '/t:VarlockGenerateTypes', '/nologo', '/verbosity:quiet']),
    );

    assertCommandSucceeded(
      'dotnet build VarlockMsbuildPackageProof',
      runDotnet(packageProofProject.projectDir, ['build', '--no-restore', '--nologo', '--verbosity', 'quiet']),
    );

    assert(
      fs.existsSync(getBuildOutputPath(packageProofProject.projectDir, packageProofProject.assemblyName)),
      'Package proof should produce the temporary PackageReference consumer assembly under bin/Debug/net8.0.',
    );
    assert(
      fs.existsSync(packageProofProject.generatedFilePath),
      'Package proof should generate obj/Varlock/AppConfig.g.cs through the packed Varlock.MSBuild assets.',
    );

    const packageGeneratedTypeSrc = fs.readFileSync(packageProofProject.generatedFilePath, 'utf8');
    assert(
      packageGeneratedTypeSrc.includes('namespace PackageProof.Generated'),
      'Package proof should generate the PackageProof.Generated namespace from the packed Varlock.MSBuild assets.',
    );
    assert(
      packageGeneratedTypeSrc.includes('public sealed partial class AppConfig'),
      'Package proof should generate the AppConfig type from the packed Varlock.MSBuild assets.',
    );
    assert(
      !fs.readFileSync(join(packageProofProject.projectDir, 'VarlockMsbuildPackageProof.csproj'), 'utf8').includes('<VarlockEnabled>'),
      'Package proof should rely on the installed Varlock.MSBuild package as the opt-in signal instead of an explicit VarlockEnabled property.',
    );
  } finally {
    packageProofProject.cleanup();
  }
} finally {
  packedMsbuildPackage.cleanup();
}

assertCommandSucceeded(
  'dotnet build dotnet-console',
  runDotnet(consoleProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
);
assert(
  fs.existsSync(getBuildOutputPath(consoleProjectDir, 'dotnet-console', 'net10.0')),
  'dotnet build proof should produce the console example assembly under bin/Debug/net10.0.',
);

assertCommandSucceeded(
  'dotnet build dotnet-aspnet-mvc',
  runDotnet(aspNetProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
);
assert(
  fs.existsSync(getBuildOutputPath(aspNetProjectDir, 'dotnet-aspnet-mvc', 'net10.0')),
  'dotnet build proof should produce the ASP.NET example assembly under bin/Debug/net10.0.',
);

assertCommandSucceeded(
  'dotnet build dotnet-worker',
  runDotnet(workerProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
);
assert(
  fs.existsSync(getBuildOutputPath(workerProjectDir, 'dotnet-worker', 'net10.0')),
  'dotnet build proof should produce the worker example assembly under bin/Debug/net10.0.',
);

assertCommandSucceeded(
  'dotnet build dotnet-functions-isolated',
  runDotnet(functionsProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
);
assert(
  fs.existsSync(getBuildOutputPath(functionsProjectDir, 'dotnet-functions-isolated', 'net10.0')),
  'dotnet build proof should produce the functions example assembly under bin/Debug/net10.0.',
);

assertCommandSucceeded(
  'dotnet build dotnet-blazor-server',
  runDotnet(blazorProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
);
assert(
  fs.existsSync(getBuildOutputPath(blazorProjectDir, 'dotnet-blazor-server', 'net10.0')),
  'dotnet build proof should produce the blazor example assembly under bin/Debug/net10.0.',
);

if (isWindows) {
  assertCommandSucceeded(
    'dotnet build dotnet-winforms',
    runDotnet(winFormsProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
  );
  assert(
    fs.existsSync(getBuildOutputPath(winFormsProjectDir, 'dotnet-winforms', 'net10.0-windows')),
    'dotnet build proof should produce the winforms example assembly under bin/Debug/net10.0-windows.',
  );
} else {
  console.log('WinForms build proof skipped (Windows-only).');
}

assertCommandSucceeded(
  'dotnet build dotnet-blazor-wasm-public',
  runDotnet(wasmProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
);
assert(
  fs.existsSync(wasmGeneratedTypePath),
  'dotnet build proof should generate the WASM public-only C# specimen at examples/dotnet-blazor-wasm-public/obj/Varlock/VarlockPublicConfig.g.cs during dotnet build.',
);

const consoleResult = runDotnet(consoleProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
const consoleOutput = parseConsoleBaselineOutput('dotnet-console', consoleResult);
assertConsoleBaselineOutput(consoleOutput, 'Console example through repo-local lookup');

const packageLocalHarness = createPackageLocalHarness(consoleProjectDir);
try {
  const packageLocalResult = runDotnet(consoleProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
  const packageLocalOutput = parseConsoleBaselineOutput('dotnet-console package-local', packageLocalResult);

  assertConsoleBaselineOutput(packageLocalOutput, 'Console example through package-local lookup');
  assert(
    fs.existsSync(packageLocalHarness.markerPath),
    'Package-local proof should execute the wrapper under node_modules/varlock/bin/cli.js before the repo-local fallback.',
  );
} finally {
  packageLocalHarness.cleanup();
}

const localBinHarness = createLocalBinHarness(consoleProjectDir);
try {
  const localBinResult = runDotnet(consoleProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
  const localBinOutput = parseConsoleBaselineOutput('dotnet-console local-bin', localBinResult);

  assertConsoleBaselineOutput(localBinOutput, 'Console example through local .bin lookup');
  assert(
    fs.existsSync(localBinHarness.markerPath),
    'Local .bin proof should execute the wrapper under node_modules/.bin/varlock before the repo-local fallback.',
  );
} finally {
  localBinHarness.cleanup();
}

const pathHarness = createPathHarness(consoleProjectDir);
try {
  const pathResult = runDotnet(
    consoleProjectDir,
    ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet'],
    {
      PATH: process.env.PATH
        ? `${pathHarness.pathDirectory}${delimiter}${process.env.PATH}`
        : pathHarness.pathDirectory,
      VARLOCK_DOTNET_PROOF_FORCE_PATH_LOOKUP: '1',
    },
  );
  const pathOutput = parseConsoleBaselineOutput('dotnet-console path', pathResult);

  assertConsoleBaselineOutput(pathOutput, 'Console example through opt-in PATH lookup');
  assert(
    fs.existsSync(pathHarness.executablePath),
    'PATH proof should create the opt-in PATH executable before the console example runs.',
  );
} finally {
  pathHarness.cleanup();
}

// ── DX-A2 Sibling Console Examples ─────────────────────────────────────

// Direct-load sibling
const directLoadBuild = runDotnet(directLoadProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-direct-load', directLoadBuild);

const directLoadResult = runDotnet(directLoadProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-direct-load', directLoadResult);
{
  const lines = parseAssignmentLines(directLoadResult.stdout);
  assert(lines.has('APP_NAME'), 'direct-load should emit APP_NAME');
  assert(lines.get('APP_NAME') === 'varlock-direct', 'direct-load APP_NAME should be varlock-direct');
  assert(lines.has('HTTP_PORT'), 'direct-load should emit HTTP_PORT');
  assert(lines.get('API_KEY') === '***', 'direct-load should mask sensitive API_KEY as ***');
  assert(directLoadResult.stdout.includes('RedactLogs'), 'direct-load should report RedactLogs');
  assert(directLoadResult.stdout.includes('PreventLeaks'), 'direct-load should report PreventLeaks');
}

// Sensitive sibling
const sensitiveBuild = runDotnet(sensitiveProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-sensitive', sensitiveBuild);

const sensitiveResult = runDotnet(sensitiveProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-sensitive', sensitiveResult);
{
  const stdout = sensitiveResult.stdout;
  assert(stdout.includes('APP_NAME = varlock-sensitive (sensitive=False)'), 'sensitive example should show APP_NAME as non-sensitive');
  assert(stdout.includes('DATABASE_URL = [REDACTED] (sensitive=True)'), 'sensitive example should redact DATABASE_URL');
  assert(stdout.includes('API_KEY = [REDACTED] (sensitive=True)'), 'sensitive example should redact API_KEY');
  assert(stdout.includes('DEBUG_MODE = True (sensitive=False)'), 'sensitive example should show DEBUG_MODE as non-sensitive');
}

// Reload sibling (runs with a timer so we just check initial output)
const reloadBuild = runDotnet(reloadProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-reload', reloadBuild);

const reloadResult = runDotnet(reloadProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet'], {}, 8000);
// Reload example keeps running; we expect partial output before it times out
{
  const stdout = reloadResult.stdout;
  assert(stdout.includes('APP_NAME = varlock-reload'), 'reload example should print APP_NAME = varlock-reload');
  assert(stdout.includes('MAX_RETRIES = 3'), 'reload example should print MAX_RETRIES = 3');
}

// Serilog sibling
const serilogBuild = runDotnet(serilogProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-serilog', serilogBuild);

const serilogResult = runDotnet(serilogProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-serilog', serilogResult);
{
  const stdout = serilogResult.stdout;
  assert(stdout.includes('"APP_NAME": "varlock-serilog"'), 'serilog example should log APP_NAME');
  assert(stdout.includes('"API_KEY": "[REDACTED]"'), 'serilog example should redact API_KEY in structured log');
  assert(stdout.includes('APP_NAME = varlock-serilog'), 'serilog example should print APP_NAME for verification');
  assert(stdout.includes('API_KEY = [REDACTED]'), 'serilog example should print redacted API_KEY');
}

// Typed-config sibling
const typedConfigBuild = runDotnet(typedConfigProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-typed-config', typedConfigBuild);

const typedConfigResult = runDotnet(typedConfigProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-typed-config', typedConfigResult);
{
  const stdout = typedConfigResult.stdout;
  assert(stdout.includes('config.AppName = varlock-typed'), 'typed-config should map APP_NAME to AppName');
  assert(stdout.includes('config.HttpPort = 4330'), 'typed-config should map HTTP_PORT to HttpPort');
  assert(stdout.includes('config.DebugMode = False'), 'typed-config should map DEBUG_MODE to DebugMode');
  assert(stdout.includes('APP_NAME -> AppName'), 'typed-config should list PropertyBindings');
}

// Custom-schema-path sibling
const customSchemaPathBuild = runDotnet(customSchemaPathProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-custom-schema-path', customSchemaPathBuild);

const customSchemaPathResult = runDotnet(customSchemaPathProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-custom-schema-path', customSchemaPathResult);
{
  const lines = parseAssignmentLines(customSchemaPathResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-custom-schema', 'custom-schema-path should load APP_NAME from config/.env');
  assert(lines.get('HTTP_PORT') === '4340', 'custom-schema-path should load HTTP_PORT from config/.env');
  assert(lines.get('VARLOCK_SCHEMA_PATH') === 'config/.env.schema', 'custom-schema-path should report the configured non-default schema path');
}

// Custom-working-dir sibling
const customWorkingDirBuild = runDotnet(customWorkingDirProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-custom-working-dir', customWorkingDirBuild);

const customWorkingDirResult = runDotnet(customWorkingDirProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-custom-working-dir', customWorkingDirResult);
{
  const lines = parseAssignmentLines(customWorkingDirResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-working-dir', 'custom-working-dir should load APP_NAME from the shared working directory');
  assert(lines.get('HTTP_PORT') === '4341', 'custom-working-dir should load HTTP_PORT from the shared working directory');
  assert(lines.get('VARLOCK_SCHEMA_PATH') === '.env.schema', 'custom-working-dir should keep the default schema file name');
  assert(lines.get('VARLOCK_WORKING_DIRECTORY_NAME') === 'shared', 'custom-working-dir should report the configured working-directory name');
}

// Environment-name sibling
const environmentNameBuild = runDotnet(environmentNameProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-environment-name', environmentNameBuild);

const environmentNameResult = runDotnet(environmentNameProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-environment-name', environmentNameResult);
{
  const lines = parseAssignmentLines(environmentNameResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-production', 'environment-name should load the production override value');
  assert(lines.get('API_BASE_URL') === 'https://api.production.varlock.test', 'environment-name should load .env.production overrides through EnvironmentName');
  assert(lines.get('VARLOCK_ENVIRONMENT_NAME') === 'production', 'environment-name should report the configured environment name');
}

// Optional sibling
const optionalBuild = runDotnet(optionalProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-optional', optionalBuild);

const optionalResult = runDotnet(optionalProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-optional', optionalResult);
{
  const lines = parseAssignmentLines(optionalResult.stdout);
  assert(lines.get('APP_NAME') === '(missing)', 'optional should keep APP_NAME empty when the configured schema entry point is missing');
  assert(lines.get('HTTP_PORT') === '(missing)', 'optional should keep HTTP_PORT empty when the configured schema entry point is missing');
  assert(lines.get('VARLOCK_OPTIONAL') === 'True', 'optional should report Optional = true');
  assert(lines.get('VARLOCK_WORKING_DIRECTORY_NAME') === 'missing-config', 'optional should report the configured missing working directory');
}

// Custom-runtime sibling
const customRuntimeBuild = runDotnet(customRuntimeProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-custom-runtime', customRuntimeBuild);

const customRuntimeResult = runDotnet(customRuntimeProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-custom-runtime', customRuntimeResult);
{
  const lines = parseAssignmentLines(customRuntimeResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-custom-runtime', 'custom-runtime should read APP_NAME from the injected runtime graph');
  assert(lines.get('HTTP_PORT') === '4343', 'custom-runtime should read HTTP_PORT from the injected runtime graph');
  assert(lines.get('FEATURE_ENABLED') === 'True', 'custom-runtime should read FEATURE_ENABLED from the injected runtime graph');
  assert(lines.get('RUNTIME_TYPE') === 'FakeVarlockRuntime', 'custom-runtime should report the injected runtime type');
}

// Coercion sibling
const coercionBuild = runDotnet(coercionProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-coercion', coercionBuild);

const coercionResult = runDotnet(coercionProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-coercion', coercionResult);
{
  const lines = parseAssignmentLines(coercionResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-coercion', 'coercion should resolve APP_NAME from the committed example values');
  assert(lines.get('MAX_CONNECTIONS') === '25', 'coercion should flatten numeric values into IConfiguration strings');
  assert(lines.get('FEATURE_ENABLED') === 'True', 'coercion should flatten boolean values into IConfiguration strings');
  assert(lines.get('REQUEST_TIMEOUT_SECONDS') === '1.5', 'coercion should flatten decimal values into IConfiguration strings');
  assert(lines.get('MAX_CONNECTIONS_TYPE') === 'Int64', 'coercion should preserve integer graph values as Int64.');
  assert(lines.get('FEATURE_ENABLED_TYPE') === 'Boolean', 'coercion should preserve boolean graph values as Boolean.');
  assert(lines.get('REQUEST_TIMEOUT_SECONDS_TYPE') === 'Decimal', 'coercion should preserve decimal graph values as Decimal.');
}

// Validation sibling
const validationBuild = runDotnet(validationProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-validation', validationBuild);

const validationResult = runDotnet(validationProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assert(
  validationResult.exitCode === 1,
  `dotnet run dotnet-console-validation should exit with code 1.\n${validationResult.stdout}\n${validationResult.stderr}`,
);
{
  const lines = parseAssignmentLines(validationResult.stdout);
  assert(lines.get('VALIDATION_CATEGORY') === 'ResolutionFailed', 'validation should report the ResolutionFailed bridge category.');
  assert(
    (lines.get('VALIDATION_MESSAGE') ?? '').includes('Value is required but is currently empty'),
    'validation should print the missing-required-value bridge message.',
  );
}

// Public-only sibling
const publicOnlyBuild = runDotnet(publicOnlyProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-public-only', publicOnlyBuild);
assert(
  fs.existsSync(getBuildOutputPath(publicOnlyProjectDir, 'dotnet-console-public-only', 'net10.0')),
  'dotnet build proof should produce the public-only example assembly under bin/Debug/net10.0.',
);
assert(
  fs.existsSync(publicOnlyGeneratedTypePath),
  'dotnet build proof should generate the public-only C# specimen at examples/dotnet-console-public-only/obj/Varlock/VarlockPublicConfig.g.cs.',
);

const publicOnlyGeneratedTypeSrc = fs.readFileSync(publicOnlyGeneratedTypePath, 'utf8');
assert(
  !publicOnlyGeneratedTypeSrc.includes('SecretToken'),
  'public-only generated type must exclude the sensitive SecretToken property.',
);
assert(
  !publicOnlyGeneratedTypeSrc.includes('SensitiveKeys'),
  'public-only generated type must exclude SensitiveKeys metadata.',
);
assert(
  !publicOnlyGeneratedTypeSrc.includes('PropertyBinding'),
  'public-only generated type must exclude PropertyBinding metadata.',
);
assert(
  publicOnlyGeneratedTypeSrc.includes('PropertyKeys'),
  'public-only generated type must keep PropertyKeys metadata.',
);

const publicOnlyResult = runDotnet(publicOnlyProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-public-only', publicOnlyResult);
{
  const lines = parseAssignmentLines(publicOnlyResult.stdout);
  assert(
    lines.get('PUBLIC_PROPERTIES') === 'AppName,FeatureEnabled,PublicBaseUrl',
    'public-only should report only the non-sensitive generated properties.',
  );
  assert(lines.get('HAS_SECRET_TOKEN') === 'False', 'public-only should report that SecretToken is absent.');
  assert(lines.get('HAS_SENSITIVE_KEYS_METADATA') === 'False', 'public-only should report that SensitiveKeys metadata is absent.');
  assert(lines.get('HAS_PROPERTY_BINDINGS_METADATA') === 'False', 'public-only should report that PropertyBindings metadata is absent.');
  assert(lines.get('PROPERTY_KEYS_COUNT') === '3', 'public-only should keep PropertyKeys for the three public properties.');
}

// Exec sibling
const execBuild = runDotnet(execProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-exec', execBuild);

const execResult = runDotnet(execProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-exec', execResult);
{
  const lines = parseAssignmentLines(execResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-exec', 'exec should resolve APP_NAME from the checked-in example values.');
  assert(lines.get('SERVICE_TOKEN') === '[REDACTED]', 'exec should redact the sensitive token in its proof output.');
  assert(lines.get('SERVICE_TOKEN_PRESENT') === 'True', 'exec should resolve a non-empty token from the local command seam.');
  assert(lines.get('SERVICE_TOKEN_IS_SENSITIVE') === 'True', 'exec should preserve sensitive metadata on the resolved token.');
  assert(lines.get('EXEC_SOURCE') === 'local-bun-command', 'exec should stay scoped to the local bun command seam.');
}

// Composition sibling
const compositionBuild = runDotnet(compositionProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-composition', compositionBuild);

const compositionResult = runDotnet(compositionProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-composition', compositionResult);
{
  const lines = parseAssignmentLines(compositionResult.stdout);
  assert(lines.get('API_BASE_URL') === 'https://staging.api.varlock.test', 'composition should resolve API_BASE_URL from schema refs.');
  assert(lines.get('USERS_ENDPOINT') === 'https://staging.api.varlock.test/users', 'composition should resolve USERS_ENDPOINT from composed values.');
  assert(lines.get('ADMIN_ENDPOINT') === 'https://staging.api.varlock.test/admin', 'composition should resolve ADMIN_ENDPOINT from composed values.');
}

// DI/options sibling
const diOptionsBuild = runDotnet(diOptionsProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-di-options', diOptionsBuild);

const diOptionsResult = runDotnet(diOptionsProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-di-options', diOptionsResult);
{
  const lines = parseAssignmentLines(diOptionsResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-di-options', 'di-options should resolve APP_NAME into the manual options object.');
  assert(lines.get('HTTP_PORT') === '4350', 'di-options should resolve HTTP_PORT into the manual options object.');
  assert(lines.get('FEATURE_ENABLED') === 'True', 'di-options should resolve FEATURE_ENABLED into the manual options object.');
  assert(lines.get('OPTIONS_PATTERN') === 'manual-map', 'di-options should keep the proof scoped to the manual mapping pattern.');
}

// Options-snapshot sibling
const optionsSnapshotBuild = runDotnet(optionsSnapshotProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-options-snapshot', optionsSnapshotBuild);

const optionsSnapshotResult = runDotnet(optionsSnapshotProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-options-snapshot', optionsSnapshotResult);
{
  const lines = parseAssignmentLines(optionsSnapshotResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-options-snapshot', 'options-snapshot should resolve APP_NAME through IOptionsSnapshot.');
  assert(lines.get('HTTP_PORT') === '4360', 'options-snapshot should resolve HTTP_PORT through IOptionsSnapshot.');
  assert(lines.get('FEATURE_ENABLED') === 'True', 'options-snapshot should resolve FEATURE_ENABLED through IOptionsSnapshot.');
  assert(lines.get('OPTIONS_PATTERN') === 'snapshot-scoped', 'options-snapshot should prove scoped snapshot access pattern.');
}

// Options-monitor sibling
const optionsMonitorBuild = runDotnet(optionsMonitorProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-options-monitor', optionsMonitorBuild);

const optionsMonitorResult = runDotnet(optionsMonitorProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-options-monitor', optionsMonitorResult);
{
  const lines = parseAssignmentLines(optionsMonitorResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-options-monitor', 'options-monitor should resolve APP_NAME through IOptionsMonitor.');
  assert(lines.get('MAX_RETRIES') === '5', 'options-monitor should resolve MAX_RETRIES through IOptionsMonitor.');
  assert(lines.get('VERBOSE') === 'False', 'options-monitor should resolve VERBOSE through IOptionsMonitor.');
  assert(lines.get('OPTIONS_PATTERN') === 'monitor-singleton', 'options-monitor should prove singleton monitor access pattern.');
}

// Explicit-executable sibling
const explicitExecutableBuild = runDotnet(explicitExecutableProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-explicit-executable', explicitExecutableBuild);

const explicitExecutableResult = runDotnet(explicitExecutableProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-explicit-executable', explicitExecutableResult);
{
  const lines = parseAssignmentLines(explicitExecutableResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-explicit-executable', 'explicit-executable should resolve APP_NAME through the configured CLI path.');
  assert(lines.get('EXECUTABLE_PATH') === '../../packages/varlock/bin/cli.js', 'explicit-executable should report the configured repo-relative CLI path.');
  assert(lines.get('LOCAL_LOOKUP') === 'False', 'explicit-executable should disable local lookup in the proof specimen.');
  assert(lines.get('PATH_LOOKUP') === 'False', 'explicit-executable should disable PATH lookup in the proof specimen.');
}

// Leak-prevention sibling
const leakPreventionBuild = runDotnet(leakPreventionProjectDir, ['build', '--nologo', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet build dotnet-console-leak-prevention', leakPreventionBuild);

const leakPreventionResult = runDotnet(leakPreventionProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
assertCommandSucceeded('dotnet run dotnet-console-leak-prevention', leakPreventionResult);
{
  const lines = parseAssignmentLines(leakPreventionResult.stdout);
  assert(lines.get('APP_NAME') === 'varlock-leak-prevention', 'leak-prevention should resolve APP_NAME from the committed example values.');
  assert(lines.get('PREVENT_LEAKS') === 'True', 'leak-prevention should surface graph.PreventLeaks as true.');
  assert(lines.get('SECRET_TOKEN_PRESENT') === 'True', 'leak-prevention should show that the sensitive token is still present in configuration.');
  assert(lines.get('SECRET_TOKEN_IS_SENSITIVE') === 'True', 'leak-prevention should preserve sensitive metadata on the token.');
  assert(lines.get('DISPLAY_SECRET_TOKEN') === '[REDACTED]', 'leak-prevention should use the manual redaction helper for display.');
  assert(lines.get('LEAK_PREVENTION_BOUNDARY') === 'metadata-only', 'leak-prevention should scope the proof to metadata-only behavior.');
}

const aspNetResult = runDotnet(aspNetProjectDir, [
  'run',
  '--no-build',
  '--no-launch-profile',
  '--verbosity',
  'quiet',
  '--',
  '--dump-config',
]);
const aspNetPayload = parseJsonOutput<AspNetPayload>('dotnet-aspnet-mvc-net8', aspNetResult);
assertAspNetPayload(aspNetPayload, 'ASP.NET baseline proof');

// --- Proof: dotnet watch runtime reload stability ---
{
  console.log('Running dotnet watch stability proof...');
  const watchResult = await runDotnetWatch(aspNetProjectDir, 6000);
  assert(watchResult.started, 'dotnet watch should start the ASP.NET MVC app successfully.');
  assert(watchResult.rebuildCount === 0,
    `dotnet watch should not trigger pathological rebuild loops. Observed ${watchResult.rebuildCount} rebuild(s) during stability window.\n${watchResult.output}`);
  console.log('dotnet watch stability proof passed.');
}

const workerResult = runDotnet(workerProjectDir, [
  'run',
  '--no-build',
  '--verbosity',
  'quiet',
  '--',
  '--dump-config',
]);
const workerPayload = parseJsonOutput<WorkerPayload>('dotnet-worker-net8', workerResult);
assertWorkerPayload(workerPayload, 'Worker example through HostApplicationBuilder.AddVarlock()');

const functionsResult = runBuiltAssembly(functionsProjectDir, 'dotnet-functions-isolated', 'net10.0', [
  '--dump-config',
]);
const functionsPayload = parseJsonOutput<FunctionsPayload>('dotnet-functions-isolated-net8', functionsResult);

assert(functionsPayload.AppName === 'varlock-functions', 'Functions example should resolve APP_NAME from Varlock.');
assert(
  functionsPayload.FunctionsOnlyKey === 'retained-from-local-settings',
  'Functions example should preserve local.settings.json keys alongside Varlock configuration.',
);

const blazorResult = runDotnet(blazorProjectDir, [
  'run',
  '--no-build',
  '--verbosity',
  'quiet',
  '--',
  '--dump-config',
]);
const blazorPayload = parseJsonOutput<BlazorPayload>('dotnet-blazor-server-net8', blazorResult);

assert(blazorPayload.AppName === 'varlock-blazor-server', 'Blazor Server example should resolve APP_NAME from Varlock.');
assert(
  blazorPayload.ComponentMessage === 'Loaded from IConfiguration inside a Razor component',
  'Blazor Server example should expose the component-specific message from Varlock.',
);

// --- Proof: WinForms legacy bridge (non-hosted direct runtime) ---

if (isWindows) {
  const winFormsResult = runDotnet(winFormsProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet', '--', '--dump-config']);
  const winFormsPayload = parseJsonOutput<WinFormsPayload>('dotnet-winforms-net48', winFormsResult);

  assert(winFormsPayload.AppName === 'varlock-winforms', 'WinForms example should resolve APP_NAME from Varlock.');
  assert(winFormsPayload.WindowTitle === 'Varlock WinForms', 'WinForms example should resolve WINDOW_TITLE from Varlock.');
  assert(winFormsPayload.SchemaSourcePresent === true, 'WinForms example should report the schema source in its resolved graph.');
} else {
  console.log('WinForms runtime proof skipped (Windows-only).');
}

// --- Proof: Blazor WASM public-only generation boundary ---

const wasmGeneratedTypeSrc = fs.readFileSync(wasmGeneratedTypePath, 'utf8');

// Public-only boundary validation: sensitive metadata must not appear in generated type
assert(
  !wasmGeneratedTypeSrc.includes('SensitiveKeys'),
  'WASM public-only generated type must not contain SensitiveKeys array.',
);
assert(
  !wasmGeneratedTypeSrc.includes('PropertyBinding'),
  'WASM public-only generated type must not contain PropertyBinding class.',
);
assert(
  !wasmGeneratedTypeSrc.includes('IsSensitive'),
  'WASM public-only generated type must not contain IsSensitive metadata.',
);
assert(
  !wasmGeneratedTypeSrc.includes('SecretToken'),
  'WASM public-only generated type must not contain the sensitive SecretToken property.',
);

// Public-only type must still have non-sensitive properties
assert(
  wasmGeneratedTypeSrc.includes('public string AppName'),
  'WASM public-only generated type should contain the non-sensitive AppName property.',
);
assert(
  wasmGeneratedTypeSrc.includes('public string PublicBaseUrl'),
  'WASM public-only generated type should contain the non-sensitive PublicBaseUrl property.',
);

// PropertyKeys metadata (safe for public bundles) must still be present
assert(
  wasmGeneratedTypeSrc.includes('PropertyKeys'),
  'WASM public-only generated metadata must contain PropertyKeys dictionary.',
);
assert(
  wasmGeneratedTypeSrc.includes('["AppName"] = "APP_NAME"'),
  'WASM public-only PropertyKeys must map public property names to original env keys.',
);

try {
  clearUserSecrets(aspNetProjectDir);
  setUserSecret(aspNetProjectDir, 'USERSECRETS_ONLY', 'loaded-from-user-secrets');
  setUserSecret(aspNetProjectDir, 'APP_NAME', 'user-secrets-fallback');

  const aspNetUserSecretsResult = runDotnet(aspNetProjectDir, [
    'run',
    '--no-build',
    '--no-launch-profile',
    '--verbosity',
    'quiet',
    '--',
    '--dump-config',
  ], {
    ASPNETCORE_ENVIRONMENT: 'Development',
  });
  const aspNetUserSecretsPayload = parseJsonOutput<AspNetPayload>('dotnet-aspnet-mvc-net8 user-secrets', aspNetUserSecretsResult);

  assertAspNetPayload(aspNetUserSecretsPayload, 'ASP.NET user-secrets proof', 'loaded-from-user-secrets');
} finally {
  clearUserSecrets(aspNetProjectDir);
}

console.log('Varlock .NET proof slice passed.');
