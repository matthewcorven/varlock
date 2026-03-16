import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..', '..');

export function getRepoRootSync() {
  return repoRoot;
}

export function getRepoTempPath(...segments: Array<string>) {
  return path.join(repoRoot, '.tmp', ...segments);
}

export function ensureRepoTempDirSync(...segments: Array<string>) {
  const directory = getRepoTempPath(...segments);
  fs.mkdirSync(directory, { recursive: true });
  return directory;
}

export function createRepoTempDirSync(prefix: string, ...segments: Array<string>) {
  const parentDirectory = ensureRepoTempDirSync(...segments);
  return fs.mkdtempSync(path.join(parentDirectory, `${prefix}-`));
}
