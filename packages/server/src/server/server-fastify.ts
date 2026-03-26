import Fastify from 'fastify';
import cors from '@fastify/cors';
import compress from '@fastify/compress';
import { getCachedOrFetch } from '../db/cache.db';
import { fetchDefiLlamaPools } from '../api/defillama.pools.api.service';
import { fetchDefiLlamaHacks } from '../api/defillama.hacks.api.service';
import { fetchPendleMarkets } from '../api/pendle.markets.api.service';
import { fetchCoinGeckoStablecoins, fetchCoinGeckoCoinList } from '../api/coingecko.stablecoins.api.service';
import { buildHackMap, matchHacks } from '../services/hacks.service';
import { buildStablecoinPriceMap, buildStablecoinAddressMap, checkDepeg } from '../services/depeg.service';
import { filterPoolsByType, getAvailableTypes } from '../services/pools.service';
import { getPoolUrl } from '../services/pool-url.service';
import { getAvailablePoolTypesMetadata } from '@shared';

const PORT = parseInt(process.env.PORT || '5000', 10);

const CACHE_KEYS = {
  LLAMA_POOLS: 'defillama_pools',
  PENDLE_POOLS: 'pendle_pools',
  HACKS: 'defillama_hacks',
  STABLECOINS: 'coingecko_stablecoins',
  COIN_LIST: 'coingecko_coin_list',
};

const getAllPools = async (): Promise<any[]> => {
  const [llamaPools, pendlePools] = await Promise.all([
    getCachedOrFetch(CACHE_KEYS.LLAMA_POOLS, fetchDefiLlamaPools),
    getCachedOrFetch(CACHE_KEYS.PENDLE_POOLS, fetchPendleMarkets),
  ]);
  return [...llamaPools, ...pendlePools];
};

const getHackMap = async () => {
  try {
    const hacks = await getCachedOrFetch(CACHE_KEYS.HACKS, fetchDefiLlamaHacks);
    return buildHackMap(hacks);
  } catch {
    return new Map();
  }
};

const getStablecoinPriceMap = async () => {
  try {
    const coins = await getCachedOrFetch(CACHE_KEYS.STABLECOINS, fetchCoinGeckoStablecoins);
    return buildStablecoinPriceMap(coins);
  } catch {
    return new Map<string, number>();
  }
};

const getStablecoinAddressMap = async () => {
  try {
    const [coins, coinList] = await Promise.all([
      getCachedOrFetch(CACHE_KEYS.STABLECOINS, fetchCoinGeckoStablecoins),
      getCachedOrFetch(CACHE_KEYS.COIN_LIST, fetchCoinGeckoCoinList),
    ]);
    return buildStablecoinAddressMap(coins, coinList);
  } catch {
    return new Map<string, number>();
  }
};

export const start = async (): Promise<void> => {
  const fastify: any = Fastify({ logger: true });

  await fastify.register(compress, { global: true });
  await fastify.register(cors, { origin: true });

  const CACHE_CONTROL = 'public, max-age=15, s-maxage=15, stale-while-revalidate=5';

  fastify.get('/api/pools', async (_request: any, reply: any) => {
    try {
      reply.header('Cache-Control', CACHE_CONTROL);
      return { status: 'ok', data: getAvailablePoolTypesMetadata() };
    } catch {
      return reply.code(500).send({ error: 'Failed to fetch available pool types' });
    }
  });

  fastify.get('/api/pools/:poolName', async (request: any, reply: any) => {
    const { poolName } = request.params as { poolName: string };

    const validPoolTypes = getAvailableTypes().map((pt) => pt.id);
    if (!validPoolTypes.includes(poolName.toUpperCase())) {
      return reply.code(400).send({ error: `Invalid pool name. Valid options: ${validPoolTypes.join(', ')}` });
    }

    try {
      const [allPools, hackMap, priceMap, addressMap] = await Promise.all([
        getAllPools(),
        getHackMap(),
        getStablecoinPriceMap(),
        getStablecoinAddressMap(),
      ]);
      const pools = filterPoolsByType(allPools, poolName)
        .map((pool: any) => ({
          ...pool,
          url: getPoolUrl(pool),
          hacks: matchHacks(pool.project, hackMap),
          depegAlerts: checkDepeg(pool.symbol, priceMap, pool.underlyingTokens ?? null, addressMap),
        }))
        .filter((pool: any) => pool.depegAlerts.length === 0);
      reply.header('Cache-Control', CACHE_CONTROL);
      return { status: 'ok', data: pools };
    } catch (error) {
      return reply.code(500).send({ error: 'Failed to fetch pools' });
    }
  });

  await fastify.listen({ port: PORT, host: '0.0.0.0' });
};

start()
  .then(() => console.log(`Server listening on port ${PORT}`))
  .catch((err) => {
    console.error(err);
    process.exit(1);
  });
