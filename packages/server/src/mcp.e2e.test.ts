import { spawn, ChildProcessWithoutNullStreams } from 'child_process';
import * as path from 'path';

// Set RUN_E2E=true to run these tests against the live external APIs
const runE2E = process.env['RUN_E2E'] === 'true';

/**
 * Sends a JSON-RPC request to the MCP server over stdin and reads a single
 * newline-delimited JSON response from stdout.
 */
async function sendRequest(proc: ChildProcessWithoutNullStreams, request: object): Promise<unknown> {
  return new Promise((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error('MCP response timeout')), 20_000);

    let buffer = '';
    const onData = (chunk: Buffer) => {
      buffer += chunk.toString();
      const newlineIdx = buffer.indexOf('\n');
      if (newlineIdx !== -1) {
        clearTimeout(timeout);
        proc.stdout.off('data', onData);
        try {
          resolve(JSON.parse(buffer.slice(0, newlineIdx)));
        } catch (e) {
          reject(e);
        }
      }
    };

    proc.stdout.on('data', onData);
    proc.stdin.write(JSON.stringify(request) + '\n');
  });
}

(runE2E ? describe : describe.skip)('MCP server E2E', () => {
  let proc: ChildProcessWithoutNullStreams;
  let requestId = 0;

  const serverRoot = path.resolve(__dirname, '../../..');
  const tsNodeArgs = [
    '-r', 'tsconfig-paths/register',
    '--project', 'packages/server/tsconfig.json',
    'packages/server/src/mcp.ts',
  ];

  const nextRequest = (method: string, params: object = {}) => ({
    jsonrpc: '2.0',
    id: ++requestId,
    method,
    params,
  });

  beforeAll(async () => {
    proc = spawn('npx', ['ts-node', ...tsNodeArgs], {
      cwd: serverRoot,
      stdio: ['pipe', 'pipe', 'pipe'],
    });

    proc.stderr.on('data', () => { /* suppress ts-node startup noise */ });

    // MCP initialise handshake
    await sendRequest(proc, nextRequest('initialize', {
      protocolVersion: '2024-11-05',
      capabilities: {},
      clientInfo: { name: 'e2e-test', version: '1.0.0' },
    }));

    proc.stdin.write(JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized' }) + '\n');
  });

  afterAll(() => {
    proc.kill();
  });

  test('lists available tools', async () => {
    const response = await sendRequest(proc, nextRequest('tools/list')) as {
      result: { tools: Array<{ name: string }> };
    };

    const names = response.result.tools.map((t) => t.name).sort();
    expect(names).toEqual(['check_contract_security', 'get_pool_types', 'get_pools']);
  });

  test('get_pool_types returns five categories', async () => {
    const response = await sendRequest(proc, nextRequest('tools/call', {
      name: 'get_pool_types',
      arguments: {},
    })) as { result: { content: Array<{ text: string }> } };

    const types = JSON.parse(response.result.content[0].text) as Array<{ id: string }>;
    expect(types.map((t) => t.id).sort()).toEqual(['BLUE_CHIP', 'ETH', 'HIGH_YIELD', 'LOW_TVL', 'STABLES']);
  });

  test('get_pools returns enriched pools from live APIs', async () => {
    const response = await sendRequest(proc, nextRequest('tools/call', {
      name: 'get_pools',
      arguments: { poolType: 'STABLES', limit: 3 },
    })) as { result: { content: Array<{ text: string }>; isError?: boolean } };

    expect(response.result.isError).toBeFalsy();
    const pools = JSON.parse(response.result.content[0].text) as Array<{
      apy: number;
      tvlUsd: number;
      depegAlerts: unknown[];
      contractSecurity: unknown[];
    }>;

    expect(Array.isArray(pools)).toBe(true);
    expect(pools.length).toBeGreaterThan(0);
    expect(pools.length).toBeLessThanOrEqual(3);
    expect(pools.every((p) => p.depegAlerts.length === 0)).toBe(true);
    expect(pools.every((p) => p.apy > 0)).toBe(true);
    expect(pools[0]).toHaveProperty('contractSecurity');
  });

  test('check_contract_security returns data for a known Ethereum token', async () => {
    // stETH on Ethereum
    const STETH = '0xae7ab96520de3a18e5e111b5eaab095312d7fe84';
    const response = await sendRequest(proc, nextRequest('tools/call', {
      name: 'check_contract_security',
      arguments: { chain: 'Ethereum', addresses: [STETH] },
    })) as { result: { content: Array<{ text: string }>; isError?: boolean } };

    expect(response.result.isError).toBeFalsy();
    const results = JSON.parse(response.result.content[0].text) as Array<{
      address: string;
      isOpenSource: number;
      isHoneypot: number;
    }>;

    expect(results.length).toBeGreaterThan(0);
    const main = results.find((r) => r.address === STETH.toLowerCase());
    expect(main).toBeDefined();
    expect(main!.isOpenSource).toBe(1);
    expect(main!.isHoneypot).toBe(0);
  });
});
