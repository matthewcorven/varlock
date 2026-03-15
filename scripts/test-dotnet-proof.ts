// @ts-nocheck

import fs from 'node:fs';
import { spawnSync } from 'node:child_process';
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

type ExecutableHarness = {
  markerPath: string;
  cleanup: () => void;
};

type PathExecutableHarness = {
  executablePath: string;
  pathDirectory: string;
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

function createExecutableHarness(
  projectDir: string,
  executableSegments: Array<string>,
  markerFileName: string,
): ExecutableHarness {
  const wrapperPath = join(projectDir, ...executableSegments);
  const markerPath = join(projectDir, markerFileName);
  const upstreamCliPath = join(repoRoot, 'packages', 'varlock', 'bin', 'cli.js');

  const wrapperSource = `#!/usr/bin/env node
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

  fs.mkdirSync(dirname(wrapperPath), { recursive: true });
  fs.writeFileSync(wrapperPath, wrapperSource, 'utf8');
  fs.chmodSync(wrapperPath, 0o755);

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
    fs.symlinkSync(upstreamCliPath, wrapperPath);
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
  return createExecutableHarness(
    projectDir,
    ['node_modules', 'varlock', 'bin', 'cli.js'],
    '.varlock-package-local-proof',
  );
}

function createLocalBinHarness(projectDir: string): ExecutableHarness {
  return createExecutableHarness(
    projectDir,
    ['node_modules', '.bin', 'varlock'],
    '.varlock-local-bin-proof',
  );
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

const consoleProjectDir = join(repoRoot, 'examples', 'dotnet-console-net8');
const aspNetProjectDir = join(repoRoot, 'examples', 'dotnet-aspnet-mvc-net8');
const aspNetGeneratedTypeDir = join(aspNetProjectDir, 'obj', 'Varlock');
const aspNetGeneratedTypePath = join(aspNetGeneratedTypeDir, 'AppConfig.g.cs');

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

function parseReloadProofLines(stdout: string): Map<string, string> {
  const lines = stdout.split('\n').map((l) => l.trim()).filter(Boolean);
  const result = new Map<string, string>();
  for (const line of lines) {
    const colonIndex = line.indexOf(':');
    if (colonIndex > 0 && line.startsWith('RELOAD_')) {
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

const reloadSuccessLines = parseReloadProofLines(reloadSuccessResult.stdout);

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

const reloadFailLines = parseReloadProofLines(reloadFailResult.stdout);

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

console.log('Varlock .NET proof slice passed.');
