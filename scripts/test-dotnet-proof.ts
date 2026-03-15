// @ts-nocheck

import fs from 'node:fs';
import { spawnSync } from 'node:child_process';
import { tmpdir } from 'node:os';
import { delimiter, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

type CommandResult = {
  stdout: string;
  stderr: string;
  exitCode: number;
};

type ConsolePayload = {
  appName: string;
  httpPort: number;
  featureEnabled: boolean;
  secretIsSensitive: boolean;
  redactLogs: boolean;
  preventLeaks: boolean;
  sourceLabels: Array<string>;
};

type AspNetPayload = {
  AppName: string;
  AppPort: number;
  FeatureEnabled: boolean;
  AppSettingsOnly: string;
  SecretTokenPresent: boolean;
  UserSecretsOnly: string;
};

type WorkerPayload = {
  AppName: string;
  AppPort: number;
  FeatureEnabled: boolean;
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

function getBuildOutputPath(projectDir: string, assemblyName: string) {
  return join(projectDir, 'bin', 'Debug', 'net8.0', `${assemblyName}.dll`);
}

function runDotnet(projectDir: string, args: Array<string>, envOverrides: NodeJS.ProcessEnv = {}): CommandResult {
  const result = spawnSync('dotnet', args, {
    cwd: projectDir,
    encoding: 'utf-8',
    env: {
      ...process.env,
      ...envOverrides,
    },
  });

  return {
    stdout: result.stdout ?? '',
    stderr: result.stderr ?? '',
    exitCode: result.status ?? 1,
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

function assertConsolePayload(payload: ConsolePayload, proofLabel: string) {
  assert(payload.appName === 'varlock-console', `${proofLabel} should resolve APP_NAME from Varlock.`);
  assert(payload.httpPort === 4310, `${proofLabel} should coerce HTTP_PORT to a number.`);
  assert(payload.featureEnabled === true, `${proofLabel} should coerce FEATURE_ENABLED to a boolean.`);
  assert(payload.secretIsSensitive === true, `${proofLabel} should preserve sensitive metadata.`);
  assert(payload.redactLogs === true, `${proofLabel} should surface RedactLogs from the graph.`);
  assert(payload.preventLeaks === true, `${proofLabel} should surface PreventLeaks from the graph.`);
  assert(
    payload.sourceLabels.some((label) => label.includes('.env.schema')),
    `${proofLabel} should report the schema source in its resolved graph.`,
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

function escapeXml(value: string) {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('"', '&quot;')
    .replaceAll('\'', '&apos;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');
}

function packVarlockMsbuildPackage(): PackedPackage {
  const tempRoot = fs.mkdtempSync(join(tmpdir(), 'varlock-msbuild-pack-proof-'));
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
  const projectDir = fs.mkdtempSync(join(tmpdir(), 'varlock-msbuild-consumer-proof-'));
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

const consoleProjectDir = join(repoRoot, 'examples', 'dotnet-console-net8');
const workerProjectDir = join(repoRoot, 'examples', 'dotnet-worker-net8');
const aspNetProjectDir = join(repoRoot, 'examples', 'dotnet-aspnet-mvc-net8');
const aspNetGeneratedTypeDir = join(aspNetProjectDir, 'obj', 'Varlock');
const aspNetGeneratedTypePath = join(aspNetGeneratedTypeDir, 'AppConfig.g.cs');

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
  'dotnet build dotnet-console-net8',
  runDotnet(consoleProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
);
assert(
  fs.existsSync(getBuildOutputPath(consoleProjectDir, 'dotnet-console-net8')),
  'dotnet build proof should produce the console example assembly under bin/Debug/net8.0.',
);

assertCommandSucceeded(
  'dotnet build dotnet-aspnet-mvc-net8',
  runDotnet(aspNetProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
);
assert(
  fs.existsSync(getBuildOutputPath(aspNetProjectDir, 'dotnet-aspnet-mvc-net8')),
  'dotnet build proof should produce the ASP.NET example assembly under bin/Debug/net8.0.',
);
assert(
  fs.existsSync(aspNetGeneratedTypePath),
  'proof:dotnet should generate the ASP.NET C# specimen at examples/dotnet-aspnet-mvc-net8/obj/Varlock/AppConfig.g.cs during dotnet build.',
);

assertCommandSucceeded(
  'dotnet build dotnet-worker-net8',
  runDotnet(workerProjectDir, ['build', '--nologo', '--verbosity', 'quiet']),
);
assert(
  fs.existsSync(getBuildOutputPath(workerProjectDir, 'dotnet-worker-net8')),
  'dotnet build proof should produce the worker example assembly under bin/Debug/net8.0.',
);

const aspNetGeneratedTypeSrc = fs.readFileSync(aspNetGeneratedTypePath, 'utf8');
assert(
  aspNetGeneratedTypeSrc.includes('namespace DotnetAspNetMvcNet8.Generated'),
  'proof:dotnet should generate the ASP.NET specimen with the configured DotnetAspNetMvcNet8.Generated namespace.',
);
assert(
  aspNetGeneratedTypeSrc.includes('public sealed partial class AppConfig'),
  'proof:dotnet should generate the ASP.NET specimen with the configured AppConfig type name.',
);

const aspNetGeneratedTypeMtimeMs = fs.statSync(aspNetGeneratedTypePath).mtimeMs;

assertCommandSucceeded(
  'dotnet build dotnet-aspnet-mvc-net8 incremental',
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
const consolePayload = parseJsonOutput<ConsolePayload>('dotnet-console-net8', consoleResult);
assertConsolePayload(consolePayload, 'Console example through repo-local lookup');

const packageLocalHarness = createPackageLocalHarness(consoleProjectDir);
try {
  const packageLocalResult = runDotnet(consoleProjectDir, ['run', '--no-build', '--no-launch-profile', '--verbosity', 'quiet']);
  const packageLocalPayload = parseJsonOutput<ConsolePayload>('dotnet-console-net8 package-local', packageLocalResult);

  assertConsolePayload(packageLocalPayload, 'Console example through package-local lookup');
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
  const localBinPayload = parseJsonOutput<ConsolePayload>('dotnet-console-net8 local-bin', localBinResult);

  assertConsolePayload(localBinPayload, 'Console example through local .bin lookup');
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
  const pathPayload = parseJsonOutput<ConsolePayload>('dotnet-console-net8 path', pathResult);

  assertConsolePayload(pathPayload, 'Console example through opt-in PATH lookup');
  assert(
    fs.existsSync(pathHarness.executablePath),
    'PATH proof should create the opt-in PATH executable before the console example runs.',
  );
} finally {
  pathHarness.cleanup();
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

assert(aspNetPayload.AppName === 'varlock-web', 'ASP.NET example should let Varlock override APP_NAME from appsettings.');
assert(aspNetPayload.AppPort === 4311, 'ASP.NET example should let Varlock override APP_PORT from appsettings.');
assert(aspNetPayload.FeatureEnabled === true, 'ASP.NET example should let Varlock override FEATURE_ENABLED from appsettings.');
assert(
  aspNetPayload.AppSettingsOnly === 'retained-from-appsettings',
  'ASP.NET example should keep unrelated appsettings keys intact.',
);
assert(aspNetPayload.SecretTokenPresent === true, 'ASP.NET example should surface Varlock-backed secret presence.');
assert(aspNetPayload.UserSecretsOnly === '', 'ASP.NET baseline proof should not depend on a user-secrets payload.');

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

  assert(
    aspNetUserSecretsPayload.UserSecretsOnly === 'loaded-from-user-secrets',
    'ASP.NET example should preserve user-secrets-only keys when Varlock is added at startup.',
  );
  assert(
    aspNetUserSecretsPayload.AppName === 'varlock-web',
    'ASP.NET example should let Varlock override overlapping APP_NAME values from User Secrets by provider order.',
  );
  assert(
    aspNetUserSecretsPayload.AppSettingsOnly === 'retained-from-appsettings',
    'ASP.NET user-secrets proof should still retain unrelated appsettings values.',
  );
} finally {
  clearUserSecrets(aspNetProjectDir);
}

// --- Reload proof: successful reload fires configuration change notification ---

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
