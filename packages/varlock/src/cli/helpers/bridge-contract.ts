import path from 'node:path';

import { type EnvGraph, type SerializedEnvGraph, VarlockError } from '../../env-graph';

export const LOAD_BRIDGE_CONTRACT_VERSION = 1;

export type LoadBridgeFailureCategory = | 'bridge-internal-error'
  | 'executable-version-mismatch'
  | 'plugin-load-failed'
  | 'resolution-failed'
  | 'schema-invalid'
  | 'schema-missing';

type LoadBridgeBasePayload = {
  contractVersion: typeof LOAD_BRIDGE_CONTRACT_VERSION;
  cliVersion: string;
  command: 'load';
  format: 'json-full';
};

export type LoadBridgeLocation = {
  file: string;
  line: number;
  column: number;
};

export type LoadBridgeSuccessPayload = LoadBridgeBasePayload & {
  ok: true;
  graph: SerializedEnvGraph;
};

export type LoadBridgeFailurePayload = LoadBridgeBasePayload & {
  ok: false;
  category: LoadBridgeFailureCategory;
  message: string;
  key?: string;
  location?: LoadBridgeLocation;
  requestedContractVersion?: string;
  supportedContractVersion?: typeof LOAD_BRIDGE_CONTRACT_VERSION;
  source?: string;
};

export type LoadBridgePayload = LoadBridgeSuccessPayload | LoadBridgeFailurePayload;

function createBasePayload(cliVersion: string): LoadBridgeBasePayload {
  return {
    contractVersion: LOAD_BRIDGE_CONTRACT_VERSION,
    cliVersion,
    command: 'load',
    format: 'json-full',
  };
}

function normalizeLocationFile(fileId: string): string {
  if (!path.isAbsolute(fileId)) return fileId;

  const relativePath = path.relative(process.cwd(), fileId);
  if (!relativePath || relativePath === '') return path.basename(fileId);
  return relativePath;
}

function getErrorLocation(error: Error): LoadBridgeLocation | undefined {
  if (!(error instanceof VarlockError) || !error.location) return undefined;

  return {
    file: normalizeLocationFile(error.location.id),
    line: error.location.lineNumber,
    column: error.location.colNumber,
  };
}

function isPluginFailure(error: Error, sourceLabel?: string): boolean {
  const searchableText = [sourceLabel, error.message, error.stack]
    .filter(Boolean)
    .join('\n')
    .toLowerCase();

  return searchableText.includes('plugin');
}

function createFailurePayload(
  cliVersion: string,
  category: LoadBridgeFailureCategory,
  message: string,
  opts?: {
    error?: Error;
    key?: string;
    requestedContractVersion?: string;
    source?: string;
    supportedContractVersion?: typeof LOAD_BRIDGE_CONTRACT_VERSION;
  },
): LoadBridgeFailurePayload {
  const payload: LoadBridgeFailurePayload = {
    ...createBasePayload(cliVersion),
    ok: false,
    category,
    message,
  };

  if (opts?.error) {
    const location = getErrorLocation(opts.error);
    if (location) payload.location = location;
  }

  if (opts?.key) payload.key = opts.key;
  if (opts?.requestedContractVersion) {
    payload.requestedContractVersion = opts.requestedContractVersion;
  }
  if (opts?.source) payload.source = opts.source;
  if (opts?.supportedContractVersion) {
    payload.supportedContractVersion = opts.supportedContractVersion;
  }

  return payload;
}

export function getLoadBridgeVersionMismatchPayload(
  cliVersion: string,
  requestedContractVersion: string,
): LoadBridgeFailurePayload {
  return createFailurePayload(
    cliVersion,
    'executable-version-mismatch',
    `Requested bridge contract version "${requestedContractVersion}" is not supported by this varlock executable`,
    {
      requestedContractVersion,
      supportedContractVersion: LOAD_BRIDGE_CONTRACT_VERSION,
    },
  );
}

export function getLoadBridgeUnexpectedFailurePayload(
  cliVersion: string,
  error: unknown,
): LoadBridgeFailurePayload {
  const normalizedError = error instanceof Error ? error : new Error(String(error));

  return createFailurePayload(
    cliVersion,
    'bridge-internal-error',
    normalizedError.message,
    { error: normalizedError },
  );
}

export function getLoadBridgeFailurePayload(
  envGraph: EnvGraph,
  cliVersion: string,
): LoadBridgeFailurePayload | undefined {
  const onlyHasEmptyDirectoryRoot = envGraph.sortedDataSources.length === 1
    && envGraph.rootDataSource?.typeLabel === 'directory'
    && envGraph.rootDataSource.children.length === 0;

  if (onlyHasEmptyDirectoryRoot) {
    return createFailurePayload(
      cliVersion,
      'schema-missing',
      'No .env or .env.schema files found',
    );
  }

  for (const source of envGraph.sortedDataSources) {
    if (source.loadingError) {
      const category = isPluginFailure(source.loadingError, source.label)
        ? 'plugin-load-failed'
        : 'schema-invalid';

      return createFailurePayload(cliVersion, category, source.loadingError.message, {
        error: source.loadingError,
        source: source.label,
      });
    }

    const schemaError = source.schemaErrors[0];
    if (schemaError) {
      const category = isPluginFailure(schemaError, source.label)
        ? 'plugin-load-failed'
        : 'schema-invalid';

      return createFailurePayload(cliVersion, category, schemaError.message, {
        error: schemaError,
        source: source.label,
      });
    }
  }

  for (const source of envGraph.sortedDataSources) {
    const resolutionError = source.resolutionErrors[0];
    if (resolutionError) {
      return createFailurePayload(cliVersion, 'resolution-failed', resolutionError.message, {
        error: resolutionError,
        source: source.label,
      });
    }
  }

  const firstFailingItem = envGraph.sortedConfigKeys
    .map((itemKey) => envGraph.configSchema[itemKey])
    .find((item) => item.validationState === 'error');

  if (!firstFailingItem) return undefined;

  const itemError = firstFailingItem.errors.find((error) => !error.isWarning)
    || firstFailingItem.errors[0];
  if (!itemError) {
    return createFailurePayload(
      cliVersion,
      'resolution-failed',
      `Configuration item "${firstFailingItem.key}" failed validation`,
      { key: firstFailingItem.key },
    );
  }

  return createFailurePayload(cliVersion, 'resolution-failed', itemError.message, {
    error: itemError,
    key: firstFailingItem.key,
  });
}

export function getLoadBridgeSuccessPayload(
  envGraph: EnvGraph,
  cliVersion: string,
): LoadBridgeSuccessPayload {
  return {
    ...createBasePayload(cliVersion),
    ok: true,
    graph: envGraph.getSerializedGraph(),
  };
}
