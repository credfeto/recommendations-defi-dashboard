# System Architecture

## Overview

The DeFi Dashboard is a server-only TypeScript application. It aggregates liquidity pool data from external APIs, enriches it with hack/audit/depeg/contract-security information, and exposes the results via a REST API and an MCP server. A SQLite cache reduces external API calls. The container handles TLS termination directly — no reverse proxy required.

## Architecture Diagram

```text
Client / MCP Host
      │
      │ HTTPS :443 (self-signed cert generated at startup)
      ▼
┌─────────────────────────────────────────────────────────┐
│                  Fastify Server                          │
│                                                         │
│  GET  /api/pools            — list pool types           │
│  GET  /api/pools/:name      — enriched pool data        │
│  POST|GET|DELETE /mcp       — MCP Streamable HTTP       │
│                                                         │
│  services/                                              │
│    pools.service          filter by type                │
│    hacks.service          exploit risk                  │
│    protocols.service      audit info                    │
│    depeg.service          stablecoin price deviation    │
│    pool-access.service    KYC / liquidity metadata      │
│    contract-security.service  GoPlus API                │
│    cache-warmer.service   background prefetch           │
│                                                         │
│  db/cache.db (SQLite, 1-hour TTL)                       │
└────────────────────────────┬────────────────────────────┘
                             │
             ┌───────────────┼──────────────────┐
             ▼               ▼                  ▼
      DefiLlama API     Pendle API       CoinGecko API
      (pools, hacks,   (markets)        (stablecoins,
       protocols)                        coin list)
```

## Module Organisation

```text
packages/server/src/
  api/        HTTP clients for DefiLlama, Pendle, CoinGecko, GoPlus
  db/         SQLite cache (better-sqlite3)
  mcp/        MCP server — tool definitions and handlers
  server/     Fastify setup, route handlers, JSON schemas
  services/   Business logic (pools, hacks, depeg, access, security)
  types/      Pool type configuration and derived metadata
  utils/      Contract address helpers, slug utilities

packages/shared/src/
  types/      Shared TypeScript type declarations (no runtime code)
              Imported via @shared path alias
```

## Data Flow — Pool Request

```text
1. GET /api/pools/:poolName
      ▼
2. Validate pool name
      ▼
3. Fetch in parallel:
     getAllPools()          — DefiLlama (SQLite-cached 1 h)
     getHackMap()           — DefiLlama hacks (SQLite-cached)
     getStablecoinPriceMap() — CoinGecko (SQLite-cached)
     getStablecoinAddressMap()
     getProtocolAuditMap()
      ▼
4. Enrich each pool:
     url, hacks, depegAlerts, auditInfo, accessInfo,
     contractAddresses, contractSecurity (GoPlus, SQLite-cached)
      ▼
5. Filter out pools with depeg alerts
      ▼
6. Return JSON
```

## Cache

SQLite at `$DB_DIR/cache.db`.

| Entry | TTL |
| --- | --- |
| DefiLlama pools | 1 h (stale-while-revalidate up to 2 h) |
| DefiLlama hacks | 1 h |
| Protocol audit map | 1 h |
| CoinGecko stablecoins | 1 h |
| CoinGecko coin list | 1 h |
| GoPlus contract security | 24 h |

The cache warmer pre-fetches all entries in the background at startup.

## TLS

The entrypoint script generates a self-signed cert at `$TLS_KEY_PATH`/`$TLS_CERT_PATH` on first start (skipped if files already exist). Fastify uses `serverFactory` to create an `https.Server` when the paths are set, or a plain `http.Server` otherwise (development).

## Technology Stack

| Layer | Technology |
| --- | --- |
| HTTP server | Fastify 5 |
| TLS | Node.js `https.Server` (self-signed cert via openssl) |
| Language | TypeScript 6, Node.js 26 |
| Database | SQLite via better-sqlite3 |
| MCP | @modelcontextprotocol/sdk (Streamable HTTP, stateless) |
| Testing | Jest 30 + ts-jest |
| Container | node:26-alpine |
