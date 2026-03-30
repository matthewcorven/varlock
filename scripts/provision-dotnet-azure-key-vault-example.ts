import { execFileSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const MONOREPO_ROOT = path.resolve(path.dirname(__filename), '..');
const EXAMPLE_DIR = path.join(MONOREPO_ROOT, 'examples', 'dotnet-console-azure-key-vault');
const ENV_LOCAL_PATH = path.join(EXAMPLE_DIR, '.env.local');

function hasFlag(flag: string) {
  return process.argv.includes(flag);
}

function getArgValue(flag: string) {
  const index = process.argv.indexOf(flag);
  if (index === -1) return undefined;

  const value = process.argv[index + 1];
  if (!value || value.startsWith('--')) {
    throw new Error(`Missing value for ${flag}`);
  }

  return value;
}

function runAz(args: Array<string>) {
  return execFileSync('az', args, {
    cwd: MONOREPO_ROOT,
    encoding: 'utf-8',
    stdio: ['ignore', 'pipe', 'pipe'],
  }).trim();
}

function ensureKeyVaultName(name: string) {
  const normalized = name.toLowerCase().replace(/[^a-z0-9-]/g, '');
  if (!/^[a-z][a-z0-9-]{1,22}[a-z0-9]$/.test(normalized)) {
    throw new Error(
      `Invalid Key Vault name "${name}". Use 3-24 chars, start with a letter, and keep only lowercase letters, numbers, or hyphens.`,
    );
  }
  return normalized;
}

const dryRun = hasFlag('--dry-run');
const suffix = randomBytes(4).toString('hex');

const subscription = getArgValue('--subscription');
const location = getArgValue('--location') ?? 'eastus2';
const resourceGroup = getArgValue('--resource-group') ?? `rg-varlock-dotnet-proof-${suffix}`;
const vaultName = ensureKeyVaultName(getArgValue('--vault-name') ?? `varlockdnproof${suffix}`);
const objectId = getArgValue('--object-id');
const databaseUrl = getArgValue('--database-url')
  ?? 'Server=tcp:example.database.windows.net,1433;Initial Catalog=varlock;User ID=demo-user;Password=not-a-real-password;';
const stripeSecretKey = getArgValue('--stripe-secret-key') ?? 'example-stripe-placeholder-key';

const initialAccount = JSON.parse(runAz(['account', 'show', '--output', 'json'])) as {
  id: string;
  name: string;
};

if (subscription && subscription !== initialAccount.id) {
  console.log(`Switching Azure CLI subscription to ${subscription}`);
  if (!dryRun) runAz(['account', 'set', '--subscription', subscription]);
}

const activeAccount = JSON.parse(runAz(['account', 'show', '--output', 'json'])) as {
  id: string;
  name: string;
};

const signedInObjectId = objectId ?? runAz(['ad', 'signed-in-user', 'show', '--query', 'id', '--output', 'tsv']);
const vaultUrl = `https://${vaultName}.vault.azure.net/`;

console.log('Provisioning Azure Key Vault example with:');
console.log(`  subscription:   ${activeAccount.name} (${activeAccount.id})`);
console.log(`  location:       ${location}`);
console.log(`  resource group: ${resourceGroup}`);
console.log(`  key vault:      ${vaultName}`);
console.log(`  vault url:      ${vaultUrl}`);
console.log(`  object id:      ${signedInObjectId}`);

if (dryRun) {
  console.log('\nDry run only. No Azure resources were created.');
  process.exit(0);
}

runAz([
  'group',
  'create',
  '--name',
  resourceGroup,
  '--location',
  location,
  '--tags',
  'app=varlock',
  'surface=dotnet',
  'example=azure-key-vault',
]);

runAz([
  'keyvault',
  'create',
  '--name',
  vaultName,
  '--resource-group',
  resourceGroup,
  '--location',
  location,
  '--enable-rbac-authorization',
  'false',
]);

runAz([
  'keyvault',
  'set-policy',
  '--name',
  vaultName,
  '--object-id',
  signedInObjectId,
  '--secret-permissions',
  'get',
  'list',
  'set',
]);

runAz([
  'keyvault',
  'secret',
  'set',
  '--vault-name',
  vaultName,
  '--name',
  'database-url',
  '--value',
  databaseUrl,
]);

runAz([
  'keyvault',
  'secret',
  'set',
  '--vault-name',
  vaultName,
  '--name',
  'stripe-secret-key',
  '--value',
  stripeSecretKey,
]);

fs.writeFileSync(ENV_LOCAL_PATH, `AZURE_KEY_VAULT_URL=${vaultUrl}\n`, 'utf-8');

console.log(`\nWrote ${path.relative(MONOREPO_ROOT, ENV_LOCAL_PATH)}`);
console.log('Run the example with:');
console.log('  dotnet run --project examples/dotnet-console-azure-key-vault');
console.log('Clean up the throwaway resource group with:');
console.log(`  az group delete --name ${resourceGroup} --yes --no-wait`);
