import { describe, test, expect } from 'vitest';
import { exec } from 'child_process';
import { promisify } from 'util';
import { join } from 'path';

const execAsync = promisify(exec);

const TEST_EXAMPLES_DIR = join(import.meta.url.replace('file://', ''), '../../examples');

describe('dotnet integration', () => {
  test('Console .NET 9 app with varlock', async () => {
    const exampleDir = join(TEST_EXAMPLES_DIR, 'dotnet-console-net9');

    // Build the console app
    const buildResult = await execAsync('dotnet build', { cwd: exampleDir });
    expect(buildResult.stdout).toContain('Build succeeded');
  });

  test('WinForms .NET 4.8 app with varlock', async () => {
    const exampleDir = join(TEST_EXAMPLES_DIR, 'dotnet-winforms-net48');

    // Build the WinForms app (cross-platform)
    const buildResult = await execAsync('dotnet build', { cwd: exampleDir });
    expect(buildResult.stdout).toContain('Build succeeded');

    // Runtime test is Windows-only (.NET 4.8 is Windows-only)
    if (process.platform === 'win32') {
      // Run WinForms app with --dump-config to check environment loading
      const appPath = join(exampleDir, 'bin/Debug/net48/WinformsVarlockExample.exe');
      const runResult = await execAsync(`"${appPath}" --dump-config`, { cwd: exampleDir });
      expect(runResult.stdout).toContain('DATABASE_URL');
      expect(runResult.stdout).toContain('API_KEY');
    }
  });
});
