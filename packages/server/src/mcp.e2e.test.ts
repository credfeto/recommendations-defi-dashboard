import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StreamableHTTPClientTransport } from '@modelcontextprotocol/sdk/client/streamableHttp.js';

// Set RUN_E2E=true to run these tests against a locally running server (npm run dev)
const runE2E = process.env['RUN_E2E'] === 'true';

const MCP_URL = `http://localhost:${process.env['PORT'] ?? '5000'}/mcp`;

async function createHttpClient(): Promise<{ client: Client; close: () => Promise<void> }> {
  const transport = new StreamableHTTPClientTransport(new URL(MCP_URL));
  const client = new Client({ name: 'e2e-test', version: '1.0.0' });
  await client.connect(transport);
  return { client, close: () => client.close() };
}

(runE2E ? describe : describe.skip)('MCP server E2E (HTTP)', () => {
  test('lists the three available tools', async () => {
    const { client, close } = await createHttpClient();
    try {
      const { tools } = await client.listTools();
      expect(tools.map((t) => t.name).sort()).toEqual(['check_contract_security', 'get_pool_types', 'get_pools']);
    } finally {
      await close();
    }
  });

  test('get_pool_types returns five categories', async () => {
    const { client, close } = await createHttpClient();
    try {
      const result = await client.callTool({ name: 'get_pool_types', arguments: {} });
      expect(result.isError).toBeFalsy();
      const text = (result.content as Array<{ type: string; text: string }>)[0].text;
      const types = JSON.parse(text) as Array<{ id: string }>;
      expect(types.map((t) => t.id).sort()).toEqual(['BLUE_CHIP', 'ETH', 'HIGH_YIELD', 'LOW_TVL', 'STABLES']);
    } finally {
      await close();
    }
  });

  test('get_pools returns enriched pools from live APIs', async () => {
    const { client, close } = await createHttpClient();
    try {
      const result = await client.callTool({ name: 'get_pools', arguments: { poolType: 'STABLES', limit: 3 } });
      expect(result.isError).toBeFalsy();
      const text = (result.content as Array<{ type: string; text: string }>)[0].text;
      const pools = JSON.parse(text) as Array<{
        apy: number;
        depegAlerts: unknown[];
        contractSecurity: unknown;
      }>;
      expect(Array.isArray(pools)).toBe(true);
      expect(pools.length).toBeGreaterThan(0);
      expect(pools.length).toBeLessThanOrEqual(3);
      expect(pools.every((p) => p.depegAlerts.length === 0)).toBe(true);
      expect(pools.every((p) => p.apy > 0)).toBe(true);
      expect(pools[0]).toHaveProperty('contractSecurity');
    } finally {
      await close();
    }
  });

  test('check_contract_security returns data for a known Ethereum token', async () => {
    const { client, close } = await createHttpClient();
    try {
      // stETH on Ethereum
      const STETH = '0xae7ab96520de3a18e5e111b5eaab095312d7fe84';
      const result = await client.callTool({
        name: 'check_contract_security',
        arguments: { chain: 'Ethereum', addresses: [STETH] },
      });
      expect(result.isError).toBeFalsy();
      const text = (result.content as Array<{ type: string; text: string }>)[0].text;
      const results = JSON.parse(text) as Array<{ address: string; isOpenSource: number; isHoneypot: number }>;
      expect(results.length).toBeGreaterThan(0);
      const main = results.find((r) => r.address === STETH.toLowerCase());
      expect(main).toBeDefined();
      expect(main!.isOpenSource).toBe(1);
      expect(main!.isHoneypot).toBe(0);
    } finally {
      await close();
    }
  });
});
