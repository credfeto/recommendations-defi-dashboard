const Fastify = require('fastify');
const fastifyCors = require('@fastify/cors');
const axios = require('axios');

const PORT = parseInt(process.env.PORT || '5000', 10);
const CACHE_TTL_MS = 60 * 60 * 1000; // 1 hour
const MIN_TVL = 1_000_000;

// In-memory cache
const cache = new Map();

const LLAMA_POOLS_URL = 'https://yields.llama.fi/pools';
const CACHE_KEY = 'llama_pools';

const filterPools = (poolData) => {
  return poolData.filter(pool => 
    pool.ilRisk === 'no' && 
    pool.tvlUsd >= MIN_TVL && 
    pool.apy > 0
  );
};

const getPoolsByType = (allPools, poolType) => {
  let filteredPools = [];
  
  if (poolType.toUpperCase() === 'ETH') {
    filteredPools = allPools.filter(pool => 
      pool.symbol.toUpperCase().includes('ETH')
    );
  } else if (poolType.toUpperCase() === 'STABLES') {
    filteredPools = allPools.filter(pool => pool.stablecoin);
  } else {
    return null; // Invalid pool type
  }
  
  return filterPools(filteredPools);
};

const start = async () => {
  const fastify = Fastify({ logger: true });

  try {
    await fastify.register(fastifyCors, { origin: true });

    fastify.get('/api/pools/:poolName', async (request, reply) => {
      try {
        const { poolName } = request.params;
        
        // Check if cache exists and is still valid
        const cachedEntry = cache.get(CACHE_KEY);
        let allPools;
        
        if (cachedEntry) {
          const isExpired = Date.now() - cachedEntry.timestamp > CACHE_TTL_MS;
          if (!isExpired) {
            allPools = cachedEntry.data.data || [];
          } else {
            // Cache expired, fetch new data
            const response = await axios.get(LLAMA_POOLS_URL);
            allPools = response.data.data || [];
            cache.set(CACHE_KEY, {
              data: response.data,
              timestamp: Date.now(),
            });
          }
        } else {
          // No cache, fetch from API
          const response = await axios.get(LLAMA_POOLS_URL);
          allPools = response.data.data || [];
          cache.set(CACHE_KEY, {
            data: response.data,
            timestamp: Date.now(),
          });
        }

        // Get pools by type
        const pools = getPoolsByType(allPools, poolName);
        
        if (pools === null) {
          reply.status(400);
          return {
            error: `Invalid pool type: ${poolName}. Valid types are: ETH, STABLES`,
            status: 'error',
            data: []
          };
        }

        return {
          status: 'success',
          data: pools
        };
      } catch (error) {
        fastify.log.error(error);
        reply.status(500);
        return {
          error: 'Failed to fetch pool data',
          status: 'error',
          data: []
        };
      }
    });

    await fastify.listen({ port: PORT, host: '0.0.0.0' });
    console.log(`Server is running on port ${PORT}`);
  } catch (err) {
    fastify.log.error(err);
    process.exit(1);
  }
};

start();
