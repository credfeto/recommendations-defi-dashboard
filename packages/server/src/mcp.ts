import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { getCachedOrFetch } from './db/cache.db';
import { defiLlamaPoolsApiService } from './api/defillama.pools.api.service';
import { defiLlamaHacksApiService } from './api/defillama.hacks.api.service';
import { pendleMarketsApiService } from './api/pendle.markets.api.service';
import { coinGeckoStablecoinsApiService } from './api/coingecko.stablecoins.api.service';
import { defiLlamaProtocolsApiService } from './api/defillama.protocols.api.service';
import { buildHackMap, matchHacks } from './services/hacks.service';
import { buildProtocolAuditMap, matchAuditInfo } from './services/protocols.service';
import { getContractSecurityForAddresses } from './services/contract-security.service';
import { buildStablecoinPriceMap, buildStablecoinAddressMap, checkDepeg } from './services/depeg.service';
import { filterPoolsByType, getAvailableTypes } from './services/pools.service';
import { getPoolUrl } from './services/pool-url.service';
import { CACHE_KEYS } from './services/cache-warmer.service';

// ── helpers ────────────────────────────────────────────────────────────────

const getAllPools = async () => {
  const [llamaPools, pendlePools] = await Promise.all([
    getCachedOrFetch(CACHE_KEYS.LLAMA_POOLS, () => defiLlamaPoolsApiService.fetchPools()),
    getCachedOrFetch(CACHE_KEYS.PENDLE_POOLS, () => pendleMarketsApiService.fetchMarkets()),
  ]);
  return [...llamaPools, ...pendlePools];
};

const getHackMap = async () => {
  try {
    const hacks = await getCachedOrFetch(CACHE_KEYS.HACKS, () => defiLlamaHacksApiService.fetchHacks());
    return buildHackMap(hacks);
  } catch {
    return new Map();
  }
};

const getProtocolAuditMap = async () => {
  try {
    const protocols = await getCachedOrFetch(CACHE_KEYS.PROTOCOLS, () => defiLlamaProtocolsApiService.fetchProtocols());
    return buildProtocolAuditMap(protocols);
  } catch {
    return new Map();
  }
};

const getStablecoinPriceMap = async () => {
  try {
    const coins = await getCachedOrFetch(CACHE_KEYS.STABLECOINS, () =>
      coinGeckoStablecoinsApiService.fetchStablecoins(),
    );
    return buildStablecoinPriceMap(coins);
  } catch {
    return new Map<string, number>();
  }
};

const getStablecoinAddressMap = async () => {
  try {
    const [coins, coinList] = await Promise.all([
      getCachedOrFetch(CACHE_KEYS.STABLECOINS, () => coinGeckoStablecoinsApiService.fetchStablecoins()),
      getCachedOrFetch(CACHE_KEYS.COIN_LIST, () => coinGeckoStablecoinsApiService.fetchCoinList()),
    ]);
    return buildStablecoinAddressMap(coins, coinList);
  } catch {
    return new Map<string, string>();
  }
};

// ── server factory ─────────────────────────────────────────────────────────

export function createMcpServer(): McpServer {
  const server = new McpServer({ name: 'defi-dashboard', version: '1.0.0' });

  server.registerTool(
    'get_pool_types',
    { description: 'List the available DeFi pool categories that can be queried' },
    async () => {
      const types = getAvailableTypes().map((t) => ({ id: t.id, name: t.name, description: t.description }));
      return { content: [{ type: 'text', text: JSON.stringify(types, null, 2) }] };
    },
  );

  server.registerTool(
    'get_pools',
    {
      description:
        'Fetch enriched DeFi pool recommendations for a given category. Returns pools with APY, TVL, hack history, depeg alerts, audit info, and contract security.',
      inputSchema: {
        poolType: z
          .enum(['ETH', 'STABLES', 'HIGH_YIELD', 'LOW_TVL', 'BLUE_CHIP'])
          .describe('The pool category to fetch'),
        limit: z
          .number()
          .int()
          .min(1)
          .max(50)
          .optional()
          .default(10)
          .describe('Maximum number of pools to return (default 10, max 50)'),
      },
    },
    async ({ poolType, limit }) => {
      const [allPools, hackMap, priceMap, addressMap, protocolAuditMap] = await Promise.all([
        getAllPools(),
        getHackMap(),
        getStablecoinPriceMap(),
        getStablecoinAddressMap(),
        getProtocolAuditMap(),
      ]);

      const pools = filterPoolsByType(allPools, poolType)
        .map((pool) => ({
          ...pool,
          underlyingTokens: (pool['underlyingTokens'] ?? []) as string[],
          url: getPoolUrl(pool),
          hacks: matchHacks(pool.project, hackMap),
          depegAlerts: checkDepeg(pool.symbol, priceMap, pool['underlyingTokens'] ?? null, addressMap),
          auditInfo: matchAuditInfo(pool.project, protocolAuditMap),
        }))
        .filter((pool) => pool.depegAlerts.length === 0);

      const enriched = await Promise.all(
        pools
          .slice(0, limit)
          .map(async (pool) => ({
            ...pool,
            contractSecurity: await getContractSecurityForAddresses(pool.chain, pool.underlyingTokens),
          })),
      );

      return { content: [{ type: 'text', text: JSON.stringify(enriched, null, 2) }] };
    },
  );

  server.registerTool(
    'check_contract_security',
    {
      description:
        'Check GoPlus contract security info for one or more token addresses on a given chain. Returns honeypot status, tax info, proxy detection, and open-source status.',
      inputSchema: {
        chain: z.string().describe('The chain name, e.g. "Ethereum", "Arbitrum", "Base"'),
        addresses: z.array(z.string()).min(1).max(10).describe('Contract addresses to check (0x...)'),
      },
    },
    async ({ chain, addresses }) => {
      const results = await getContractSecurityForAddresses(chain, addresses);
      return { content: [{ type: 'text', text: JSON.stringify(results, null, 2) }] };
    },
  );

  return server;
}

