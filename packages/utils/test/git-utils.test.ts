import {
  describe, test, expect, beforeAll, afterAll,
} from 'vitest';
import {
  mkdirSync, mkdtempSync, writeFileSync, rmSync,
} from 'node:fs';
import { execSync } from 'node:child_process';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { checkIsFileGitIgnored } from '../src/git-utils';
import { createRepoTempDirSync } from '../src/repo-temp';

describe('checkIsFileGitIgnored', () => {
  let suiteTempDir: string;
  let testDir: string;
  let testFile: string;
  let ignoredFile: string;

  beforeAll(() => {
    suiteTempDir = createRepoTempDirSync('git-utils-test');
    testDir = path.join(suiteTempDir, 'git-utils-test-with spaces');
    testFile = path.join(testDir, 'test-file.txt');
    ignoredFile = path.join(testDir, 'ignored-file.txt');

    // Create test directory with spaces in the name
    mkdirSync(testDir, { recursive: true });

    // Initialize git repo
    execSync('git init', { cwd: testDir });
    execSync('git config user.email "test@test.com"', { cwd: testDir });
    execSync('git config user.name "Test User"', { cwd: testDir });

    // Create .gitignore file
    writeFileSync(path.join(testDir, '.gitignore'), 'ignored-file.txt\n');

    // Create test files
    writeFileSync(testFile, 'test content');
    writeFileSync(ignoredFile, 'ignored content');
  });

  afterAll(() => {
    rmSync(suiteTempDir, { recursive: true, force: true });
  });

  test('should return false for non-ignored file in path with spaces', async () => {
    const result = await checkIsFileGitIgnored(testFile);
    expect(result).toBe(false);
  });

  test('should return true for ignored file in path with spaces', async () => {
    const result = await checkIsFileGitIgnored(ignoredFile);
    expect(result).toBe(true);
  });

  test('should return false for non-existent git repo with warning', async () => {
    // This case must stay outside the repo because checkIsFileGitIgnored()
    // shells out with cwd = dirname(path); a repo-local .tmp path would still
    // sit under the enclosing git repository and never hit the warning branch.
    const nonGitDir = mkdtempSync(path.join(tmpdir(), 'non-git-dir-with spaces-'));
    const nonGitPath = path.join(nonGitDir, 'file.txt');
    try {
      writeFileSync(nonGitPath, 'content');

      const result = await checkIsFileGitIgnored(nonGitPath, true);
      expect(result).toBe(false);
    } finally {
      rmSync(nonGitDir, { recursive: true, force: true });
    }
  });
});
