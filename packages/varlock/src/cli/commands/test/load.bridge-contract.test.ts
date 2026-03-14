import fs from 'node:fs';
import path from 'node:path';

import outdent from 'outdent';
import {
  afterEach, describe, expect, test, vi,
} from 'vitest';

import {
  DirectoryDataSource,
  DotEnvFileDataSource,
  EnvGraph,
} from '../../../env-graph';
import {
  getLoadBridgeFailurePayload,
  getLoadBridgeSuccessPayload,
  getLoadBridgeVersionMismatchPayload,
} from '../../helpers/bridge-contract';
import packageJson from '../../../../package.json';

function getCliVersion(): string {
  return packageJson.version;
}

function readFixture(name: string, cwd: string) {
  const fixturePath = path.join(path.dirname(expect.getState().testPath!), 'fixtures', 'load-bridge', name);
  const rawFixture = fs.readFileSync(fixturePath, 'utf8')
    .replaceAll('__CLI_VERSION__', getCliVersion())
    .replaceAll('__CWD__', cwd);

  return JSON.parse(rawFixture);
}

async function loadGraph(spec: {
  envFile?: string;
  files?: Record<string, string>;
}) {
  const cwd = path.dirname(expect.getState().testPath!);
  vi.spyOn(process, 'cwd').mockReturnValue(cwd);

  const graph = new EnvGraph();
  graph.basePath = cwd;

  if (spec.files) {
    graph.setVirtualImports(cwd, spec.files);
    await graph.setRootDataSource(new DirectoryDataSource(cwd));
  } else if (spec.envFile) {
    await graph.setRootDataSource(new DotEnvFileDataSource(path.join(cwd, '.env.schema'), {
      overrideContents: spec.envFile,
    }));
  } else {
    await graph.setRootDataSource(new DirectoryDataSource(cwd));
  }

  await graph.finishLoad();
  return { cwd, graph };
}

afterEach(() => {
  vi.restoreAllMocks();
});

describe('load bridge contract', () => {
  test('wraps successful json-full output in a versioned envelope', async () => {
    const { cwd, graph } = await loadGraph({
      files: {
        '.env.schema': 'FOO=bar\n',
      },
    });

    await graph.generateTypesIfNeeded();
    await graph.resolveEnvValues();

    expect(getLoadBridgeSuccessPayload(graph, getCliVersion())).toEqual(readFixture('success.json', cwd));
  });

  test('returns schema-missing when no env files are present', async () => {
    const { cwd, graph } = await loadGraph({});

    expect(getLoadBridgeFailurePayload(graph, getCliVersion())).toEqual(readFixture('schema-missing.json', cwd));
  });

  test('returns schema-invalid for invalid schema definitions', async () => {
    const { cwd, graph } = await loadGraph({
      envFile: outdent`
        # @defaultSensitive("nope")
        # ---
        FOO=bar
      `,
    });

    expect(getLoadBridgeFailurePayload(graph, getCliVersion())).toEqual(readFixture('schema-invalid.json', cwd));
  });

  test('returns schema-invalid with location for malformed schema parse failures', async () => {
    const { cwd, graph } = await loadGraph({
      envFile: outdent`
        # @defaultSensitive(
        # ---
        FOO=bar
      `,
    });

    expect(getLoadBridgeFailurePayload(graph, getCliVersion())).toEqual(readFixture('schema-invalid-location.json', cwd));
  });

  test('returns resolution-failed for invalid resolved config', async () => {
    const { cwd, graph } = await loadGraph({
      envFile: outdent`
        API_KEY=
      `,
    });

    await graph.generateTypesIfNeeded();
    await graph.resolveEnvValues();

    expect(getLoadBridgeFailurePayload(graph, getCliVersion())).toEqual(readFixture('resolution-failed.json', cwd));
  });

  test('returns plugin-load-failed for plugin loading errors', async () => {
    const { cwd, graph } = await loadGraph({
      envFile: outdent`
        # @plugin(../../../env-graph/test/plugins/test-plugin-no-package-json)
        # ---
      `,
    });

    expect(getLoadBridgeFailurePayload(graph, getCliVersion())).toEqual(readFixture('plugin-load-failed.json', cwd));
  });

  test('returns executable-version-mismatch for unsupported bridge versions', async () => {
    const cwd = path.dirname(expect.getState().testPath!);

    expect(getLoadBridgeVersionMismatchPayload(getCliVersion(), '2')).toEqual(
      readFixture('executable-version-mismatch.json', cwd),
    );
  });
});
