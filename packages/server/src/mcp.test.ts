import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { InMemoryTransport } from '@modelcontextprotocol/sdk/inMemory.js';
import { createMcpServer } from './mcp';

// Mock every external dependency so no network or DB calls happen
jest.mock('./db/cache.db');
jest.mock('./api/defillama.pools.api.service');
jest.mock('./api/defillama.hacks.api.service');
jest.mock('./api/defillama.protocols.api.service');
jest.mock('./api/pendle.markets.api.service');
jest.mock('./api/coingecko.stablecoins.api.service');
jest.mock('./services/contract-security.service');

import { getCachedOrFetch } from './db/cache.db';
import { getContractSecurityForAddresses } from './services/contract-security.service';
import { CACHE_KEYS } from './services/cache-warmer.service';

const mockGetCachedOrFetch = getCachedOrFetch as jest.MockedFunction<typeof getCachedOrFetch>;
const mockGetContractSecurity = getContractSecurityForAddresses as jest.MockedFunction<
  typeof getContractSecurityForAddresses
>;

// Minimal PoolData shape that passes applyBaseFilters
const makePool = (overrides: Record<string, unknown> = {}) => ({
  chain: 'Ethereum',
  project: 'test-protocol',
  symbol: 'USDC-ETH',
  tvlUsd: 5_000_000,
  apy: 10,
  ilRisk: 'no',
  stablecoin: false,
  pool: 'pool-uuid',
  dataSource: 'defillama',
  underlyingTokens: [],
  ...overrides,
});

// Returns pools only for the llama pools key; everything else returns []
const mockPoolsOnly = (pools: ReturnType<typeof makePool>[]) => {
  mockGetCachedOrFetch.mockImplementation(async (key: string) => {
    if (key === CACHE_KEYS.LLAMA_POOLS) return pools;
    return [];
  });
};

// Spin up an in-process client↔server pair for each test
async function createTestClient() {
  const server = createMcpServer();
  const [clientTransport, serverTransport] = InMemoryTransport.createLinkedPair();
  await server.connect(serverTransport);

  const client = new Client({ name: 'test-client', version: '1.0.0' });
  await client.connect(clientTransport);

  return { client };
}

beforeEach(() => {
  jest.clearAllMocks();
  mockGetCachedOrFetch.mockResolvedValue([]);
  mockGetContractSecurity.mockResolvedValue([]);
});

// ── get_pool_types ──────────────────────────────────────────────────────────

describe('get_pool_types tool', () => {
  test('lists the five standard pool categories', async () => {
    const { client } = await createTestClient();
    const result = await client.callTool({ name: 'get_pool_types', arguments: {} });

    expect(result.isError).toBeFalsy();
    const text = (result.content as Array<{ type: string; text: string }>)[0].text;
    const types = JSON.parse(text) as Array<{ id: string; name: string; description: string }>;

    expect(types.map((t) => t.id).sort()).toEqual(['BLUE_CHIP', 'ETH', 'HIGH_YIELD', 'LOW_TVL', 'STABLES']);
    expect(types[0]).toHaveProperty('name');
    expect(types[0]).toHaveProperty('description');
  });
});

// ── get_pools ───────────────────────────────────────────────────────────────

describe('get_pools tool', () => {
  test('returns an array of enriched pools for STABLES', async () => {
    mockPoolsOnly([makePool({ stablecoin: true })]);

    const { client } = await createTestClient();
    const result = await client.callTool({ name: 'get_pools', arguments: { poolType: 'STABLES' } });

    expect(result.isError).toBeFalsy();
    const text = (result.content as Array<{ type: string; text: string }>)[0].text;
    const pools = JSON.parse(text) as unknown[];
    expect(Array.isArray(pools)).toBe(true);
    expect(pools).toHaveLength(1);
  });

  test('respects the limit parameter', async () => {
    mockPoolsOnly(
      Array.from({ length: 20 }, (_, i) => makePool({ pool: `pool-${i}`, apy: 10 + i, stablecoin: true })),
    );

    const { client } = await createTestClient();
    const result = await client.callTool({ name: 'get_pools', arguments: { poolType: 'STABLES', limit: 3 } });

    const text = (result.content as Array<{ type: string; text: string }>)[0].text;
    expect(JSON.parse(text)).toHaveLength(3);
  });

  test('uses default limit of 10 when not specified', async () => {
    mockPoolsOnly(
      Array.from({ length: 15 }, (_, i) => makePool({ pool: `pool-${i}`, apy: 10 + i, stablecoin: true })),
    );

    const { client } = await createTestClient();
    const result = await client.callTool({ name: 'get_pools', arguments: { poolType: 'STABLES' } });

    const text = (result.content as Array<{ type: string; text: string }>)[0].text;
    expect(JSON.parse(text)).toHaveLength(10);
  });

  test('each pool has contractSecurity field', async () => {
    mockPoolsOnly([makePool({ stablecoin: true })]);

    const { client } = await createTestClient();
    const result = await client.callTool({ name: 'get_pools', arguments: { poolType: 'STABLES' } });

    const text = (result.content as Array<{ type: string; text: string }>)[0].text;
    const returned = JSON.parse(text) as Array<{ contractSecurity: unknown }>;
    expect(returned[0]).toHaveProperty('contractSecurity');
  });

  test('returns isError for invalid poolType', async () => {
    const { client } = await createTestClient();
    const result = await client.callTool({ name: 'get_pools', arguments: { poolType: 'INVALID' } });
    expect(result.isError).toBe(true);
  });
});

// ── check_contract_security ─────────────────────────────────────────────────

describe('check_contract_security tool', () => {
  const ADDR = '0xae7ab96520de3a18e5e111b5eaab095312d7fe84';

  const securityInfo = {
    chain: 'Ethereum',
    address: ADDR,
    parentAddress: null,
    isOpenSource: 1,
    isHoneypot: 0,
    isProxy: 0,
    buyTax: 0,
    sellTax: 0,
    transferTax: 0,
    cannotBuy: 0,
    honeypotWithSameCreator: 0,
    tokenName: 'stETH',
    tokenSymbol: 'stETH',
  };

  test('returns security info for given address', async () => {
    mockGetContractSecurity.mockResolvedValue([securityInfo]);

    const { client } = await createTestClient();
    const result = await client.callTool({
      name: 'check_contract_security',
      arguments: { chain: 'Ethereum', addresses: [ADDR] },
    });

    expect(result.isError).toBeFalsy();
    const text = (result.content as Array<{ type: string; text: string }>)[0].text;
    const results = JSON.parse(text) as typeof securityInfo[];
    expect(results).toHaveLength(1);
    expect(results[0].tokenSymbol).toBe('stETH');
    expect(results[0].isHoneypot).toBe(0);
    expect(mockGetContractSecurity).toHaveBeenCalledWith('Ethereum', [ADDR]);
  });

  test('returns empty array when no security data found', async () => {
    const { client } = await createTestClient();
    const result = await client.callTool({
      name: 'check_contract_security',
      arguments: { chain: 'Ethereum', addresses: [ADDR] },
    });

    const text = (result.content as Array<{ type: string; text: string }>)[0].text;
    expect(JSON.parse(text)).toEqual([]);
  });

  test('returns isError for empty addresses array', async () => {
    const { client } = await createTestClient();
    const result = await client.callTool({
      name: 'check_contract_security',
      arguments: { chain: 'Ethereum', addresses: [] },
    });
    expect(result.isError).toBe(true);
  });

  test('returns isError for more than 10 addresses', async () => {
    const { client } = await createTestClient();
    const addresses = Array.from({ length: 11 }, (_, i) => `0x${i.toString().padStart(40, '0')}`);
    const result = await client.callTool({
      name: 'check_contract_security',
      arguments: { chain: 'Ethereum', addresses },
    });
    expect(result.isError).toBe(true);
  });
});
