import Fastify, { FastifyRequest, FastifyReply } from 'fastify';
import cors from '@fastify/cors';
import compress from '@fastify/compress';
import { StreamableHTTPServerTransport } from '@modelcontextprotocol/sdk/server/streamableHttp.js';
import type { Transport } from '@modelcontextprotocol/sdk/shared/transport.js';
import { createMcpServer } from '../mcp';
import {
  getAllPools,
  getHackMap,
  getProtocolAuditMap,
  getStablecoinPriceMap,
  getStablecoinAddressMap,
} from '../services/pool-enrichment.service';
import { matchHacks } from '../services/hacks.service';
import { matchAuditInfo } from '../services/protocols.service';
import { getContractSecurityForAddresses } from '../services/contract-security.service';
import { checkDepeg } from '../services/depeg.service';
import { filterPoolsByType, getAvailableTypes } from '../services/pools.service';
import { getPoolUrl } from '../services/pool-url.service';
import { getAvailablePoolTypesMetadata } from '../shared';
import { getPoolTypesSchema, getPoolsByNameSchema } from './schemas';
import { cacheWarmerService } from '../services/cache-warmer.service';

const PORT = parseInt(process.env['PORT'] ?? '5000', 10);

const CACHE_CONTROL = 'public, max-age=15, s-maxage=15, stale-while-revalidate=5';

export const start = async (): Promise<void> => {
  const fastify = Fastify({ logger: true });

  await fastify.register(compress, { global: true });
  await fastify.register(cors, { origin: true });

  // ── MCP endpoint (Streamable HTTP, stateless) ──────────────────────────
  // sessionIdGenerator omitted → stateless mode (no session tracking).
  const mcpTransport = new StreamableHTTPServerTransport({});
  const mcpServer = createMcpServer();
  // StreamableHTTPServerTransport satisfies the Transport interface at runtime but its
  // sessionId getter returns `string | undefined` while Transport declares `sessionId?: string`.
  // Under exactOptionalPropertyTypes these differ at the type level only — the cast is safe.
  await mcpServer.connect(mcpTransport as unknown as Transport);

  const handleMcp = async (request: FastifyRequest, reply: FastifyReply) => {
    reply.hijack();
    try {
      await mcpTransport.handleRequest(request.raw, reply.raw, request.body);
    } catch (err) {
      fastify.log.error(err, 'MCP transport error');
      if (!reply.raw.headersSent) {
        reply.raw.writeHead(500, { 'Content-Type': 'application/json' });
      }
      reply.raw.end(JSON.stringify({ error: 'Internal MCP server error' }));
    }
  };

  fastify.post('/mcp', handleMcp);
  fastify.get('/mcp', handleMcp);
  fastify.delete('/mcp', handleMcp);

  fastify.get('/api/pools', { schema: getPoolTypesSchema }, async (_request: FastifyRequest, reply: FastifyReply) => {
    try {
      reply.header('Cache-Control', CACHE_CONTROL);
      return { status: 'ok', data: getAvailablePoolTypesMetadata() };
    } catch {
      return reply.code(500).send({ error: 'Failed to fetch available pool types' });
    }
  });

  fastify.get(
    '/api/pools/:poolName',
    { schema: getPoolsByNameSchema },
    async (request: FastifyRequest<{ Params: { poolName: string } }>, reply: FastifyReply) => {
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
          .map((pool) => {
            const underlyingTokens = pool['underlyingTokens'] as string[] | undefined;
            return {
              ...pool,
              underlyingTokens,
              url: getPoolUrl(pool),
              hacks: matchHacks(pool.project, hackMap),
              depegAlerts: checkDepeg(pool.symbol, priceMap, underlyingTokens ?? null, addressMap),
              auditInfo: matchAuditInfo(pool.project, protocolAuditMap),
            };
          })
          .filter((pool) => pool.depegAlerts.length === 0);

        // Enrich each pool with contract security info (DB-cached, 24h TTL)
        const enriched = await Promise.all(
          pools.map(async (pool) => ({
            ...pool,
            contractSecurity: await getContractSecurityForAddresses(pool.chain, pool.underlyingTokens ?? []),
          })),
        );

        reply.header('Cache-Control', CACHE_CONTROL);
        return { status: 'ok', data: enriched };
      } catch {
        return reply.code(500).send({ error: 'Failed to fetch pools' });
      }
    },
  );

  await fastify.listen({ port: PORT, host: '0.0.0.0' });
  cacheWarmerService.warmCache(fastify.log);
};

start()
  .then(() => console.log(`Server listening on port ${PORT}`))
  .catch((err: unknown) => {
    console.error(err);
    process.exit(1);
  });
