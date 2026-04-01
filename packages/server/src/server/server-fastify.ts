import Fastify, { FastifyRequest, FastifyReply } from 'fastify';
import cors from '@fastify/cors';
import compress from '@fastify/compress';
import { getCachedOrFetch } from '../db/cache.db';
import { defiLlamaPoolsApiService } from '../api/defillama.pools.api.service';
import { defiLlamaHacksApiService } from '../api/defillama.hacks.api.service';
import { pendleMarketsApiService } from '../api/pendle.markets.api.service';
import { coinGeckoStablecoinsApiService } from '../api/coingecko.stablecoins.api.service';
import { defiLlamaProtocolsApiService } from '../api/defillama.protocols.api.service';
import { buildHackMap, matchHacks } from '../services/hacks.service';
import { buildProtocolAuditMap, matchAuditInfo } from '../services/protocols.service';
import { buildStablecoinPriceMap, buildStablecoinAddressMap, checkDepeg } from '../services/depeg.service';
import { filterPoolsByType, getAvailableTypes } from '../services/pools.service';
import { getPoolUrl } from '../services/pool-url.service';
import { getAvailablePoolTypesMetadata } from '@shared';
import { getPoolTypesSchema, getPoolsByNameSchema } from './schemas';
import { CACHE_KEYS, cacheWarmerService } from '../services/cache-warmer.service';

const PORT = parseInt(process.env.PORT || '5000', 10);

const CACHE_CONTROL = 'public, max-age=15, s-maxage=15, stale-while-revalidate=5';

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
    const protocols = await getCachedOrFetch(CACHE_KEYS.PROTOCOLS, () =>
      defiLlamaProtocolsApiService.fetchProtocols(),
    );
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

export const start = async (): Promise<void> => {
  const fastify = Fastify({ logger: true });

  await fastify.register(compress, { global: true });
  await fastify.register(cors, { origin: true });

  fastify.get('/api/pools', { schema: getPoolTypesSchema }, async (_request: FastifyRequest, reply: FastifyReply) => {
    try {
      reply.header('Cache-Control', CACHE_CONTROL);
      return { status: 'ok', data: getAvailablePoolTypesMetadata() };
    } catch {
      return reply.code(500).send({ error: 'Failed to fetch available pool types' });
    }
  });

  fastify.get('/api/pools/:poolName', { schema: getPoolsByNameSchema }, async (request: FastifyRequest<{ Params: { poolName: string } }>, reply: FastifyReply) => {
    const { poolName } = request.params;

    const validPoolTypes = getAvailableTypes().map((pt) => pt.id);
    if (!validPoolTypes.includes(poolName.toUpperCase())) {
      return reply.code(400).send({ error: `Invalid pool name. Valid options: ${validPoolTypes.join(', ')}` });
    }

    try {
      const [allPools, hackMap, priceMap, addressMap, protocolAuditMap] = await Promise.all([
        getAllPools(),
        getHackMap(),
        getStablecoinPriceMap(),
        getStablecoinAddressMap(),
        getProtocolAuditMap(),
      ]);
      const pools = filterPoolsByType(allPools, poolName)
        .map((pool) => ({
          ...pool,
          url: getPoolUrl(pool),
          hacks: matchHacks(pool.project, hackMap),
          depegAlerts: checkDepeg(pool.symbol, priceMap, pool.underlyingTokens ?? null, addressMap),
          auditInfo: matchAuditInfo(pool.project, protocolAuditMap),
        }))
        .filter((pool) => pool.depegAlerts.length === 0);
      reply.header('Cache-Control', CACHE_CONTROL);
      return { status: 'ok', data: pools };
    } catch {
      return reply.code(500).send({ error: 'Failed to fetch pools' });
    }
  });

  await fastify.listen({ port: PORT, host: '0.0.0.0' });
  cacheWarmerService.warmCache(fastify.log);
};

start()
  .then(() => console.log(`Server listening on port ${PORT}`))
  .catch((err: unknown) => {
    console.error(err);
    process.exit(1);
  });
