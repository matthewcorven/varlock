#!/usr/bin/env node
const fs = require('node:fs');

fs.writeFileSync('/Users/core/git/matthewcorven/varlock/varlock dotnet tests repro 054a19cf77f345e0869389999bc62588/.varlock-repo-local-proof', 'executed\n');

const args = process.argv.slice(2);
const bridgeContractIndex = args.indexOf('--bridge-contract');
const bridgeContract = bridgeContractIndex >= 0 ? args[bridgeContractIndex + 1] : null;

if (bridgeContract === '0') {
  console.log(JSON.stringify({
    contractVersion: 1,
    cliVersion: '0.0.0-test',
    command: 'load',
    format: 'json-full',
    ok: false,
    category: 'executable-version-mismatch',
    message: 'Requested bridge contract version "0" is not supported by this varlock executable',
    requestedContractVersion: '0',
    supportedContractVersion: 1,
  }));
  process.exit(1);
}

console.log(JSON.stringify({
  contractVersion: 1,
  cliVersion: '0.0.0-test',
  command: 'load',
  format: 'json-full',
  ok: true,
  graph: {
    basePath: '/Users/core/git/matthewcorven/varlock/varlock dotnet tests repro 054a19cf77f345e0869389999bc62588/app',
    config: {
      APP_NAME: {
        value: 'repo-local-proof',
        isSensitive: false,
      },
    },
    sources: [
      {
        label: '.env.schema',
        enabled: true,
        path: '.env.schema',
      },
    ],
    settings: {
      redactLogs: true,
      preventLeaks: true,
    },
  },
}));
