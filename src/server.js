const Fastify = require('fastify');
const fastifyCors = require('@fastify/cors');
const axios = require('axios');

const PORT = parseInt(process.env.PORT || '5000', 10);
const CACHE_TTL_MS = 60 * 60 * 1000; // 1 hour

// In-memory cache
const cache = new Map();

const LLAMA_POOLS_URL = 'https://yields.llama.fi/pools';
const CACHE_KEY = 'llama_pools';

const start = async () => {
  const fastify = Fastify({ logger: true });

  try {
    await fastify.register(fastifyCors, { origin: true });

    fastify.get('/api/pools', async (request, reply) => {
      try {
        // Check if cache exists and is still valid
        const cachedEntry = cache.get(CACHE_KEY);
        if (cachedEntry) {
          const isExpired = Date.now() - cachedEntry.timestamp > CACHE_TTL_MS;
          if (!isExpired) {
            return cachedEntry.data;
          }
        }

        // Fetch fresh data from API
        const response = await axios.get(LLAMA_POOLS_URL);
        
        // Cache the response
        cache.set(CACHE_KEY, {
          data: response.data,
          timestamp: Date.now(),
        });

        return response.data;
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
