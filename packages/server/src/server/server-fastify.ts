import Fastify from 'fastify';
import cors from '@fastify/cors';
import axios from 'axios';
import { filterPools, getPoolsByType, getAvailableTypes } from './server';
import { fetchPendlePools } from './pendleService';
import { fetchHacks, buildHackMap, matchHacks } from './hacksService';
import { getAvailablePoolTypesMetadata } from '@shared';

const PORT = parseInt(process.env.PORT || '5000', 10);
const CACHE_TTL_MS = 60 * 60 * 1000; // 1 hour

interface CacheEntry {
  data: any[];
  timestamp: number;
}

// In-memory cache
const cache = new Map<string, CacheEntry>();

const LLAMA_POOLS_URL = 'https://yields.llama.fi/pools';
const LLAMA_CACHE_KEY = 'llama_pools';
const PENDLE_CACHE_KEY = 'pendle_pools';
const HACKS_CACHE_KEY = 'hacks';

const getCachedLlamaPools = async (): Promise<any[]> => {
  const now = Date.now();
  const cached = cache.get(LLAMA_CACHE_KEY);

  if (cached && now - cached.timestamp < CACHE_TTL_MS) {
    return cached.data;
  }

  const response = await axios.get(LLAMA_POOLS_URL);
  // Exclude Pendle pools from Llama — the Pendle API is the authoritative source
  const pools = (response.data.data || [])
    .filter((p: any) => p.project !== 'pendle')
    .map((p: any) => ({ ...p, dataSource: 'defillama' }));
  cache.set(LLAMA_CACHE_KEY, { data: pools, timestamp: now });
  return pools;
};

const getCachedPendlePools = async (): Promise<any[]> => {
  const now = Date.now();
  const cached = cache.get(PENDLE_CACHE_KEY);

  if (cached && now - cached.timestamp < CACHE_TTL_MS) {
    return cached.data;
  }

  const pools = (await fetchPendlePools()).map((p: any) => ({ ...p, dataSource: 'pendle' }));
  cache.set(PENDLE_CACHE_KEY, { data: pools, timestamp: now });
  return pools;
};

const getAllPools = async (): Promise<any[]> => {
  const [llamaPools, pendlePools] = await Promise.all([getCachedLlamaPools(), getCachedPendlePools()]);
  return [...llamaPools, ...pendlePools];
};

const getCachedHackMap = async () => {
  const now = Date.now();
  const cached = cache.get(HACKS_CACHE_KEY);

  if (cached && now - cached.timestamp < CACHE_TTL_MS) {
    return cached.data as Map<string, any[]>;
  }

  const hacks = await fetchHacks();
  const hackMap = buildHackMap(hacks);
  cache.set(HACKS_CACHE_KEY, { data: hackMap, timestamp: now });
  return hackMap;
};

export const start = async (): Promise<void> => {
  const fastify: any = Fastify({ logger: true });

  await fastify.register(cors, { origin: true });

  fastify.get('/api/pools', async (request: any, reply: any) => {
    try {
      const poolTypes = getAvailablePoolTypesMetadata();
      return { status: 'ok', data: poolTypes };
    } catch (error) {
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
      const [allPools, hackMap] = await Promise.all([getAllPools(), getCachedHackMap()]);
      const pools = getPoolsByType(allPools, poolName).map((pool: any) => ({
        ...pool,
        hacks: matchHacks(pool.project, hackMap),
      }));
      return { status: 'ok', data: pools };
    } catch (error) {
      return reply.code(500).send({ error: 'Failed to fetch pools' });
    }
  });

  await fastify.listen({ port: PORT, host: '0.0.0.0' });
};

// Always start the server
start()
  .then(() => {
    console.log(`Server listening on port ${PORT}`);
  })
  .catch((err) => {
    console.error(err);
    process.exit(1);
  });
