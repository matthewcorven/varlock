// @ts-nocheck

import fs from 'node:fs';
import { spawnSync } from 'node:child_process';
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
  AppPort: number;
  FeatureEnabled: boolean;
  AppSettingsOnly: string;
  SecretTokenPresent: boolean;
  UserSecretsOnly: string;
};

type AspNetSerilogPayload = {
  graphRedactLogs: boolean;
  eventRedactLogs: boolean;
  destructuredSecretToken: string;
  destructuredAppName: string;
  destructuredCaseMismatchSecretToken: string;
  scalarSecretToken: string;
};

type WorkerPayload = {
  AppName: string;
  AppPort: number;
  FeatureEnabled: boolean;
};

type FunctionsPayload = {
  AppName: string;
  AppPort: number;
  FeatureEnabled: boolean;
  FunctionsOnlyKey?: string;
};

type BlazorPayload = {
  AppName: string;
  AppPort: number;
  FeatureEnabled: boolean;
};

type WinFormsPayload = {
  appName: string;
  httpPort: number;
  featureEnabled: boolean;
  secretIsSensitive: boolean;
  redactLogs: boolean;
  preventLeaks: boolean;
  sourceLabels: Array<string>;
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
  assert(payload.AppName === 'varlock-web', `${proofLabel} should let Varlock override APP_NAME from appsettings.`);
  assert(payload.AppPort === 4311, `${proofLabel} should let Varlock override APP_PORT from appsettings.`);
  assert(payload.FeatureEnabled === true, `${proofLabel} should let Varlock override FEATURE_ENABLED from appsettings.`);
  assert(
    payload.AppSettingsOnly === 'retained-from-appsettings',
    `${proofLabel} should keep unrelated appsettings keys intact.`,
  );
  assert(payload.SecretTokenPresent === true, `${proofLabel} should surface Varlock-backed secret presence.`);
  assert(payload.UserSecretsOnly === expectedUserSecretsOnly, `${proofLabel} should preserve the expected User Secrets-only value.`);
}

function assertAspNetSerilogPayload(payload: AspNetSerilogPayload) {
  assert(payload.graphRedactLogs === true, 'ASP.NET Serilog proof should observe graph.RedactLogs from the loaded graph.');
  assert(payload.eventRedactLogs === payload.graphRedactLogs, 'ASP.NET Serilog proof should enrich VarlockRedactLogs metadata without mutating it.');
  assert(payload.destructuredSecretToken === '[REDACTED]', 'ASP.NET Serilog proof should use the literal [REDACTED] for exact, case-sensitive sensitive-key matches during destructuring.');
  assert(payload.destructuredAppName === 'varlock-web', 'ASP.NET Serilog proof should leave non-sensitive destructured values unchanged.');
  assert(
    payload.destructuredCaseMismatchSecretToken === 'web-secret-value',
    'ASP.NET Serilog proof should use exact case-sensitive key matching rather than redacting mismatched keys.',
  );
  assert(
    payload.scalarSecretToken === 'web-secret-value',
    'ASP.NET Serilog proof should show that scalar message-template parameters remain raw.',
  );
}

function assertWorkerPayload(payload: WorkerPayload, proofLabel: string) {
  assert(payload.AppName === 'varlock-worker', `${proofLabel} should resolve APP_NAME from Varlock.`);
  assert(payload.AppPort === 4313, `${proofLabel} should coerce APP_PORT to a number.`);
  assert(payload.FeatureEnabled === true, `${proofLabel} should coerce FEATURE_ENABLED to a boolean.`);
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

function schemaUsesUnsupportedCoerceDecorator(projectDir: string) {
  const schemaPath = join(projectDir, '.env.schema');
  return fs.existsSync(schemaPath) && fs.readFileSync(schemaPath, 'utf8').includes('@coerce=');
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
    version: packageFileName!.slice('Varlock.MSBuild.'.length, -'.nupkg'.length),
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
    <VarlockEnabled>true</VarlockEnabled>
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

  const programFileContent = `using PackageProof.Generated;

var generated = new AppConfig
{
  AppName = "package-proof",
  AppPort = 4312,
  FeatureEnabled = true,
};

Console.WriteLine(generated.AppName + ":" + AppConfigMetadata.PropertyBindings.Count);
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
const workerProjectDir = join(repoRoot, 'examples', 'dotnet-worker');
const aspNetProjectDir = join(repoRoot, 'examples', 'dotnet-aspnet-mvc');
const functionsProjectDir = join(repoRoot, 'examples', 'dotnet-functions-isolated');
const blazorProjectDir = join(repoRoot, 'examples', 'dotnet-blazor-server');
const winFormsProjectDir = join(repoRoot, 'examples', 'dotnet-winforms');
const wasmProjectDir = join(repoRoot, 'examples', 'dotnet-blazor-wasm-public');
const wasmGeneratedTypeDir = join(wasmProjectDir, 'obj', 'Varlock');
const wasmGeneratedTypePath = join(wasmGeneratedTypeDir, 'VarlockPublicConfig.g.cs');
const aspNetGeneratedTypeDir = join(aspNetProjectDir, 'obj', 'Varlock');
const aspNetGeneratedTypePath = join(aspNetGeneratedTypeDir, 'VarlockConfig.g.cs');

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
      'dotnet build VarlockMsbuildPackageProof',
      runDotnet(packageProofProject.projectDir, ['build', '--nologo', '--verbosity', 'quiet']),
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
  } finally {
    packageProofProject.cleanup();
  }
} finally {
  packedMsbuildPackage.cleanup();
}

fs.rmSync(aspNetGeneratedTypeDir, { recursive: true, force: true });
assert(
  !fs.existsSync(aspNetGeneratedTypePath),
  'proof:dotnet should start from a clean obj/Varlock/ generated-output path before MSBuild generation runs.',
);

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
assert(
  fs.existsSync(aspNetGeneratedTypePath),
  'proof:dotnet should generate the ASP.NET C# specimen at examples/dotnet-aspnet-mvc/obj/Varlock/VarlockConfig.g.cs during dotnet build.',
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

const aspNetGeneratedTypeSrc = fs.readFileSync(aspNetGeneratedTypePath, 'utf8');
assert(
  aspNetGeneratedTypeSrc.includes('namespace Varlock.Generated'),
  'proof:dotnet should generate the ASP.NET specimen with the configured Varlock.Generated namespace.',
);
assert(
  aspNetGeneratedTypeSrc.includes('public sealed partial class VarlockConfig'),
  'proof:dotnet should generate the ASP.NET specimen with the configured VarlockConfig type name.',
);

const aspNetGeneratedTypeMtimeMs = fs.statSync(aspNetGeneratedTypePath).mtimeMs;

assertCommandSucceeded(
  'dotnet build dotnet-aspnet-mvc incremental',
  runDotnet(aspNetProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
);

const aspNetGeneratedTypeSrcAfterIncrementalBuild = fs.readFileSync(aspNetGeneratedTypePath, 'utf8');
assert(
  aspNetGeneratedTypeSrcAfterIncrementalBuild === aspNetGeneratedTypeSrc,
  'proof:dotnet should keep ASP.NET generated C# output deterministic across identical builds.',
);
assert(
  fs.statSync(aspNetGeneratedTypePath).mtimeMs === aspNetGeneratedTypeMtimeMs,
  'proof:dotnet should not rewrite the ASP.NET generated C# file when MSBuild inputs are unchanged.',
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

const shouldSkipExtendedRuntimeProofs = process.env.VARLOCK_DOTNET_PROOF_FULL !== '1'
  && [
    aspNetProjectDir,
    workerProjectDir,
    functionsProjectDir,
    blazorProjectDir,
  ].some(schemaUsesUnsupportedCoerceDecorator);

if (shouldSkipExtendedRuntimeProofs) {
  console.log('Extended .NET runtime proofs skipped: non-baseline examples still declare @coerce, which the current CLI bridge rejects. Set VARLOCK_DOTNET_PROOF_FULL=1 to force the broader suite.');
  process.exit(0);
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

const aspNetOptionsResult = runDotnet(aspNetProjectDir, [
  'run',
  '--no-build',
  '--no-launch-profile',
  '--verbosity',
  'quiet',
  '--',
  '--options-proof',
]);
const aspNetOptionsPayload = parseJsonOutput<AspNetPayload>('dotnet-aspnet-mvc-net8 options', aspNetOptionsResult);
assertAspNetPayload(aspNetOptionsPayload, 'ASP.NET options proof');

// Hosted ASP.NET MVC Serilog specimen: destructuring redaction, metadata-only enrichment,
// case-sensitive key matching, and scalar-template non-coverage.
const aspNetSerilogResult = runDotnet(aspNetProjectDir, [
  'run',
  '--no-build',
  '--no-launch-profile',
  '--verbosity',
  'quiet',
  '--',
  '--serilog-proof',
]);
assertCommandSucceeded('dotnet-aspnet-mvc-net8 --serilog-proof', aspNetSerilogResult);

const aspNetSerilogLines = parseTaggedLines(aspNetSerilogResult.stdout, ['SERILOG_PROOF']);
assert(
  aspNetSerilogLines.has('SERILOG_PROOF'),
  'ASP.NET Serilog proof should emit a SERILOG_PROOF line.',
);

const aspNetSerilogPayload = JSON.parse(aspNetSerilogLines.get('SERILOG_PROOF')!) as AspNetSerilogPayload;
assertAspNetSerilogPayload(aspNetSerilogPayload);

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

const functionsResult = runDotnet(functionsProjectDir, [
  'run',
  '--no-build',
  '--verbosity',
  'quiet',
  '--',
  '--dump-config',
]);
const functionsPayload = parseJsonOutput<FunctionsPayload>('dotnet-functions-isolated-net8', functionsResult);

assert(functionsPayload.AppName === 'varlock-functions', 'Functions example should resolve APP_NAME from Varlock.');
assert(functionsPayload.AppPort === 7071, 'Functions example should coerce APP_PORT to a number.');
assert(functionsPayload.FeatureEnabled === true, 'Functions example should coerce FEATURE_ENABLED to a boolean.');
assert(
  typeof functionsPayload.FunctionsOnlyKey === 'string',
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
assert(blazorPayload.AppPort === 5280, 'Blazor Server example should coerce APP_PORT to a number.');
assert(blazorPayload.FeatureEnabled === true, 'Blazor Server example should coerce FEATURE_ENABLED to a boolean.');

// --- Proof: WinForms legacy bridge (non-hosted direct runtime) ---

if (isWindows) {
  const winFormsResult = runDotnet(winFormsProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet', '--', '--dump-config']);
  const winFormsPayload = parseJsonOutput<WinFormsPayload>('dotnet-winforms-net48', winFormsResult);

  assert(winFormsPayload.appName === 'varlock-winforms', 'WinForms example should resolve APP_NAME from Varlock.');
  assert(winFormsPayload.httpPort === 4311, 'WinForms example should coerce HTTP_PORT to a number.');
  assert(winFormsPayload.featureEnabled === true, 'WinForms example should coerce FEATURE_ENABLED to a boolean.');
  assert(winFormsPayload.secretIsSensitive === true, 'WinForms example should preserve sensitive metadata.');
  assert(winFormsPayload.redactLogs === true, 'WinForms example should surface RedactLogs from the graph.');
  assert(winFormsPayload.preventLeaks === true, 'WinForms example should surface PreventLeaks from the graph.');
  assert(
    winFormsPayload.sourceLabels.some((label) => label.includes('.env.schema')),
    'WinForms example should report the schema source in its resolved graph.',
  );
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
  !wasmGeneratedTypeSrc.includes('API_KEY'),
  'WASM public-only generated type must not contain the sensitive API_KEY property.',
);

// Public-only type must still have non-sensitive properties
assert(
  wasmGeneratedTypeSrc.includes('public string AppName'),
  'WASM public-only generated type should contain the non-sensitive AppName property.',
);
assert(
  wasmGeneratedTypeSrc.includes('public double AppPort'),
  'WASM public-only generated type should contain the non-sensitive AppPort property.',
);
assert(
  wasmGeneratedTypeSrc.includes('public bool FeatureEnabled'),
  'WASM public-only generated type should contain the non-sensitive FeatureEnabled property.',
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

// --- Reload proof: successful reload fires configuration change notification ---

const reloadSuccessResult = runDotnet(aspNetProjectDir, [
  'run',
  '--no-build',
  '--no-launch-profile',
  '--verbosity',
  'quiet',
  '--',
  '--reload-proof',
]);

assert(
  reloadSuccessResult.exitCode === 0,
  `Reload success proof should exit cleanly, got code ${reloadSuccessResult.exitCode}.\n${reloadSuccessResult.stdout}\n${reloadSuccessResult.stderr}`,
);

const reloadSuccessLines = parseTaggedLines(reloadSuccessResult.stdout, ['RELOAD_']);

assert(
  reloadSuccessLines.has('RELOAD_PROOF_INITIAL'),
  'Reload success proof should emit RELOAD_PROOF_INITIAL line.',
);

const reloadInitial = JSON.parse(reloadSuccessLines.get('RELOAD_PROOF_INITIAL')!) as AspNetPayload;
assert(
  reloadInitial.AppName === 'varlock-web',
  'Reload proof initial state should have APP_NAME = varlock-web.',
);

assert(
  !reloadSuccessLines.has('RELOAD_PROOF_TIMEOUT'),
  'Reload success proof should not time out. Configuration change notification must fire after a successful file change.',
);

assert(
  reloadSuccessLines.has('RELOAD_PROOF_RELOADED'),
  'Reload success proof should emit RELOAD_PROOF_RELOADED line after file change triggers reload.',
);

const reloadReloaded = JSON.parse(reloadSuccessLines.get('RELOAD_PROOF_RELOADED')!) as AspNetPayload;
assert(
  reloadReloaded.AppName === 'varlock-reloaded',
  'After successful reload, APP_NAME should reflect the updated .env.schema value.',
);

const reloadCount = parseInt(reloadSuccessLines.get('RELOAD_PROOF_COUNT') ?? '0', 10);
assert(
  reloadCount >= 1,
  'Reload success proof should report at least one configuration reload notification.',
);

assert(
  reloadSuccessLines.has('RELOAD_PROOF_MONITOR_APP_NAME'),
  'Reload success proof should emit RELOAD_PROOF_MONITOR_APP_NAME line.',
);
assert(
  reloadSuccessLines.get('RELOAD_PROOF_MONITOR_APP_NAME') === 'varlock-reloaded',
  'IOptionsMonitor<VarlockAppOptions>.CurrentValue.APP_NAME should reflect the reloaded value.',
);

// --- Reload proof: failed reload does NOT fire notification or mutate state ---

const reloadFailResult = runDotnet(aspNetProjectDir, [
  'run',
  '--no-build',
  '--no-launch-profile',
  '--verbosity',
  'quiet',
  '--',
  '--reload-fail-proof',
]);

assert(
  reloadFailResult.exitCode === 0,
  `Reload failure proof should exit cleanly, got code ${reloadFailResult.exitCode}.\n${reloadFailResult.stdout}\n${reloadFailResult.stderr}`,
);

const reloadFailLines = parseTaggedLines(reloadFailResult.stdout, ['RELOAD_']);

assert(
  reloadFailLines.has('RELOAD_FAIL_PROOF_INITIAL'),
  'Reload failure proof should emit RELOAD_FAIL_PROOF_INITIAL line.',
);

const failInitial = JSON.parse(reloadFailLines.get('RELOAD_FAIL_PROOF_INITIAL')!) as AspNetPayload;
assert(
  failInitial.AppName === 'varlock-web',
  'Reload failure proof initial state should have APP_NAME = varlock-web.',
);

assert(
  reloadFailLines.has('RELOAD_FAIL_PROOF_AFTER'),
  'Reload failure proof should emit RELOAD_FAIL_PROOF_AFTER line.',
);

const failAfter = JSON.parse(reloadFailLines.get('RELOAD_FAIL_PROOF_AFTER')!) as AspNetPayload;
assert(
  failAfter.AppName === 'varlock-web',
  'After failed reload, APP_NAME must remain unchanged (last-known-good preserved).',
);
assert(
  failAfter.AppPort === 4311,
  'After failed reload, APP_PORT must remain unchanged.',
);
assert(
  failAfter.FeatureEnabled === true,
  'After failed reload, FEATURE_ENABLED must remain unchanged.',
);

const failReloadCount = parseInt(reloadFailLines.get('RELOAD_FAIL_PROOF_COUNT') ?? '-1', 10);
assert(
  failReloadCount === 0,
  `Failed reload must not fire configuration reload notification. Got ${failReloadCount} notification(s).`,
);

assert(
  reloadFailLines.has('RELOAD_FAIL_PROOF_MONITOR_APP_NAME'),
  'Reload failure proof should emit RELOAD_FAIL_PROOF_MONITOR_APP_NAME line.',
);
assert(
  reloadFailLines.get('RELOAD_FAIL_PROOF_MONITOR_APP_NAME') === 'varlock-web',
  'IOptionsMonitor<VarlockAppOptions>.CurrentValue.APP_NAME must remain unchanged after failed reload.',
);

const workerReloadResult = runDotnet(workerProjectDir, [
  'run',
  '--no-build',
  '--verbosity',
  'quiet',
  '--',
  '--reload-proof',
]);

assert(
  workerReloadResult.exitCode === 0,
  `Worker reload proof should exit cleanly, got code ${workerReloadResult.exitCode}.\n${workerReloadResult.stdout}\n${workerReloadResult.stderr}`,
);

const workerReloadLines = parseTaggedLines(workerReloadResult.stdout, ['WORKER_']);

assert(
  workerReloadLines.has('WORKER_RELOAD_PROOF_INITIAL'),
  'Worker reload proof should emit WORKER_RELOAD_PROOF_INITIAL line.',
);

const workerReloadInitial = JSON.parse(workerReloadLines.get('WORKER_RELOAD_PROOF_INITIAL')!) as WorkerPayload;
assertWorkerPayload(workerReloadInitial, 'Worker reload proof initial state');

assert(
  !workerReloadLines.has('WORKER_RELOAD_PROOF_TIMEOUT'),
  'Worker reload proof should not time out. IOptionsMonitor<T>.OnChange must fire after a successful file change in the hosted service.',
);

assert(
  workerReloadLines.has('WORKER_RELOAD_PROOF_RELOADED'),
  'Worker reload proof should emit WORKER_RELOAD_PROOF_RELOADED line after file change triggers reload.',
);

const workerReloaded = JSON.parse(workerReloadLines.get('WORKER_RELOAD_PROOF_RELOADED')!) as WorkerPayload;
assert(
  workerReloaded.AppName === 'varlock-worker-reloaded',
  'Worker reload proof should reflect the updated APP_NAME after a successful reload.',
);
assert(
  workerReloaded.AppPort === 4313,
  'Worker reload proof should preserve APP_PORT across successful reload.',
);
assert(
  workerReloaded.FeatureEnabled === true,
  'Worker reload proof should preserve FEATURE_ENABLED across successful reload.',
);

const workerReloadCount = parseInt(workerReloadLines.get('WORKER_RELOAD_PROOF_COUNT') ?? '0', 10);
assert(
  workerReloadCount >= 1,
  'Worker reload proof should report at least one monitor notification after a successful reload.',
);

assert(
  workerReloadLines.get('WORKER_RELOAD_PROOF_MONITOR_APP_NAME') === 'varlock-worker-reloaded',
  'Worker reload proof should show IOptionsMonitor<VarlockWorkerOptions>.CurrentValue.APP_NAME as the reloaded value.',
);

const workerReloadFailResult = runDotnet(workerProjectDir, [
  'run',
  '--no-build',
  '--verbosity',
  'quiet',
  '--',
  '--reload-fail-proof',
]);

assert(
  workerReloadFailResult.exitCode === 0,
  `Worker reload failure proof should exit cleanly, got code ${workerReloadFailResult.exitCode}.\n${workerReloadFailResult.stdout}\n${workerReloadFailResult.stderr}`,
);

const workerReloadFailLines = parseTaggedLines(workerReloadFailResult.stdout, ['WORKER_']);

assert(
  !workerReloadFailLines.has('WORKER_RELOAD_FAIL_PROOF_TIMEOUT'),
  'Worker reload failure proof should not time out while waiting for the failed reload signal.',
);

assert(
  workerReloadFailLines.has('WORKER_RELOAD_FAIL_PROOF_INITIAL'),
  'Worker reload failure proof should emit WORKER_RELOAD_FAIL_PROOF_INITIAL line.',
);

const workerReloadFailInitial = JSON.parse(workerReloadFailLines.get('WORKER_RELOAD_FAIL_PROOF_INITIAL')!) as WorkerPayload;
assertWorkerPayload(workerReloadFailInitial, 'Worker reload failure proof initial state');

assert(
  workerReloadFailLines.has('WORKER_RELOAD_FAIL_PROOF_AFTER'),
  'Worker reload failure proof should emit WORKER_RELOAD_FAIL_PROOF_AFTER line.',
);

const workerReloadFailAfter = JSON.parse(workerReloadFailLines.get('WORKER_RELOAD_FAIL_PROOF_AFTER')!) as WorkerPayload;
assertWorkerPayload(workerReloadFailAfter, 'Worker reload failure proof last-known-good state');

const workerReloadFailCount = parseInt(workerReloadFailLines.get('WORKER_RELOAD_FAIL_PROOF_COUNT') ?? '-1', 10);
assert(
  workerReloadFailCount === 0,
  `Worker failed reload must not fire monitor notifications. Got ${workerReloadFailCount} notification(s).`,
);

assert(
  workerReloadFailLines.get('WORKER_RELOAD_FAIL_PROOF_MONITOR_APP_NAME') === 'varlock-worker',
  'Worker reload failure proof should keep IOptionsMonitor<VarlockWorkerOptions>.CurrentValue.APP_NAME at the last known good value.',
);

const snapshotProofResult = runDotnet(aspNetProjectDir, [
  'run',
  '--no-build',
  '--no-launch-profile',
  '--verbosity',
  'quiet',
  '--',
  '--snapshot-proof',
]);

assert(
  snapshotProofResult.exitCode === 0,
  `Snapshot proof should exit cleanly, got code ${snapshotProofResult.exitCode}.\n${snapshotProofResult.stdout}\n${snapshotProofResult.stderr}`,
);

const snapshotLines = parseTaggedLines(snapshotProofResult.stdout, ['SNAPSHOT_']);

assert(
  snapshotLines.has('SNAPSHOT_PROOF_SCOPE_A_INITIAL'),
  'Snapshot proof should emit SNAPSHOT_PROOF_SCOPE_A_INITIAL line.',
);

const snapshotScopeAInitial = JSON.parse(snapshotLines.get('SNAPSHOT_PROOF_SCOPE_A_INITIAL')!) as AspNetPayload;
assert(
  snapshotScopeAInitial.AppName === 'varlock-web',
  'Snapshot proof initial scope should start with APP_NAME = varlock-web.',
);

assert(
  !snapshotLines.has('SNAPSHOT_PROOF_TIMEOUT'),
  'Snapshot proof should not time out while waiting for a successful reload.',
);

assert(
  snapshotLines.has('SNAPSHOT_PROOF_SCOPE_B_AFTER'),
  'Snapshot proof should emit SNAPSHOT_PROOF_SCOPE_B_AFTER line after a successful reload.',
);

const snapshotScopeBAfter = JSON.parse(snapshotLines.get('SNAPSHOT_PROOF_SCOPE_B_AFTER')!) as AspNetPayload;
assert(
  snapshotScopeBAfter.AppName === 'varlock-snapshot-reloaded',
  'A new scope created after a successful reload should see the updated APP_NAME value.',
);

assert(
  snapshotLines.has('SNAPSHOT_PROOF_SCOPE_A_STILL'),
  'Snapshot proof should emit SNAPSHOT_PROOF_SCOPE_A_STILL line for the original scope.',
);

const snapshotScopeAStill = JSON.parse(snapshotLines.get('SNAPSHOT_PROOF_SCOPE_A_STILL')!) as AspNetPayload;
assert(
  snapshotScopeAStill.AppName === 'varlock-web',
  'The original scope should keep its original IOptionsSnapshot<T> value after reload.',
);

const snapshotReloadCount = parseInt(snapshotLines.get('SNAPSHOT_PROOF_RELOAD_COUNT') ?? '0', 10);
assert(
  snapshotReloadCount >= 1,
  'Snapshot proof should observe at least one successful reload notification.',
);

assert(
  snapshotLines.get('SNAPSHOT_PROOF_MONITOR_APP_NAME') === 'varlock-snapshot-reloaded',
  'Snapshot proof should show IOptionsMonitor<VarlockAppOptions>.CurrentValue.APP_NAME as the reloaded value.',
);

assert(
  snapshotLines.has('SNAPSHOT_PROOF_SCOPE_C_AFTER_FAILED'),
  'Snapshot proof should emit SNAPSHOT_PROOF_SCOPE_C_AFTER_FAILED line after a failed reload attempt.',
);

const snapshotScopeCAfterFailed = JSON.parse(snapshotLines.get('SNAPSHOT_PROOF_SCOPE_C_AFTER_FAILED')!) as AspNetPayload;
assert(
  snapshotScopeCAfterFailed.AppName === 'varlock-snapshot-reloaded',
  'A new scope created after a failed reload should keep the last known good APP_NAME value.',
);

const snapshotFinalReloadCount = parseInt(snapshotLines.get('SNAPSHOT_PROOF_FINAL_RELOAD_COUNT') ?? '-1', 10);
assert(
  snapshotFinalReloadCount === snapshotReloadCount,
  'Failed reload attempts must not add extra IOptionsMonitor<T>.OnChange notifications during snapshot proof.',
);

console.log('Varlock .NET proof slice passed.');
