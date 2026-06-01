# System Architecture

## Overview

The DeFi Dashboard is a native-AOT .NET 10 application. It aggregates liquidity pool data from external APIs, enriches it with hack/audit/depeg/contract-security information, and exposes the results via a REST API and an MCP server. A SQLite cache reduces external API calls. Kestrel handles TLS termination and HTTP/3 directly — no reverse proxy required.

## Architecture Diagram

```text
Client / MCP Host
      |
      | HTTPS :443 (self-signed server.pfx generated at startup)
      | HTTP/3 (QUIC) :443/udp
      v
+-------------------------------------------------------+
|              Kestrel (HTTP/1.1 + HTTP/2 + HTTP/3)     |
|              port 8081 inside container               |
|                                                       |
|  GET  /api/pools            -- list pool types        |
|  GET  /api/pools/:name      -- enriched pool data     |
|  POST|GET|DELETE /mcp       -- MCP Streamable HTTP    |
|  GET  /ping                 -- health check           |
|                                                       |
|  Services/                                            |
|    PoolFilterService        filter by type            |
|    HacksService             exploit risk              |
|    ProtocolsService         audit info                |
|    DepegService             stablecoin deviation      |
|    PoolAccessService        KYC / liquidity metadata  |
|    ContractSecurityService  GoPlus API                |
|    CacheWarmerService       background prefetch       |
|                                                       |
|  Cache/ApiCacheService  (SQLite, 1-hour TTL)          |
+---------------------------+---------------------------+
                            |
          +-----------------+-----------------+
          v                 v                 v
   DefiLlama API      Pendle API       CoinGecko API
   (pools, hacks,    (markets)        (stablecoins,
    protocols)                         coin list)
```

## Module Organisation

```text
src/Credfeto.Defi.Server/
  ApiClients/   HTTP clients for DefiLlama, Pendle, CoinGecko, GoPlus
                Each has a LoggingExtensions/ sub-namespace with
                source-generated LoggerMessage methods
  Cache/        SQLite cache (Microsoft.Data.Sqlite)
  Config/       Strongly-typed config records (CacheConfig, RpcConfig)
  Endpoints/    Minimal-API handlers (PoolsEndpoints, HealthEndpoints)
  Helpers/      KestrelConfig — HTTPS + HTTP/3 setup
  Json/         AppJsonContext (source-generated System.Text.Json)
  Mcp/          MCP server tools (DefiMcpTools) and setup
  Models/       Domain models (Pool, HackInfo, DepegAlert, etc.)
  Services/     Business logic — pool filtering, enrichment, depeg
  Utils/        ContractAddressUtils, SlugUtils

src/Credfeto.Defi.Server.Tests/
  268 xunit v3 unit tests using FunFair.Test.Common
```

## Data Flow — Pool Request

```text
1. GET /api/pools/:poolName
      v
2. Validate pool name against known types
      v
3. Fetch in parallel:
     GetAllPoolsAsync()           -- DefiLlama (SQLite-cached 1 h)
     GetHackMapAsync()            -- DefiLlama hacks (SQLite-cached)
     GetStablecoinPriceMapAsync() -- CoinGecko (SQLite-cached)
     GetStablecoinAddressMapAsync()
     GetProtocolAuditMapAsync()
      v
4. Enrich each pool:
     Url, Hacks, DepegAlerts, AuditInfo, AccessInfo,
     ContractAddresses, ContractSecurity (GoPlus, 24h cached)
      v
5. Filter out pools with depeg alerts
      v
6. Return JSON (source-generated serialisation, AOT-safe)
```

## TLS and HTTP/3

The `docker-entrypoint.sh` generates a self-signed PKCS12 cert (`server.pfx`, no password) at `$CERT_PATH` on first container start; subsequent starts reuse it. A real cert can be substituted by mounting it and overriding `CERT_PATH`.

`Helpers/KestrelConfig.cs` loads the PFX via `X509CertificateLoader.LoadPkcs12FromFile` with `EphemeralKeySet` and configures `HttpProtocols.Http1AndHttp2AndHttp3` on port 8081. HTTP/3 uses the MsQuic QUIC implementation (`libmsquic`) installed at runtime.

## Cache

SQLite at `$Cache__DbDirectory/cache.db`.

| Entry | TTL |
| --- | --- |
| DefiLlama pools | 1 h (stale-while-revalidate up to 2 h) |
| DefiLlama hacks | 1 h |
| Protocol audit map | 1 h |
| CoinGecko stablecoins | 1 h |
| CoinGecko coin list | 1 h |
| GoPlus contract security | 24 h |

The cache warmer pre-fetches all entries in the background at startup.

## Technology Stack

| Layer | Technology |
| --- | --- |
| Runtime | .NET 10, native AOT (`linux-x64`) |
| HTTP server | ASP.NET Core / Kestrel |
| TLS | PFX cert via `X509CertificateLoader` |
| HTTP/3 | MsQuic (`libmsquic`) |
| Serialisation | `System.Text.Json` source generation |
| Database | SQLite via `Microsoft.Data.Sqlite` |
| MCP | `ModelContextProtocol.AspNetCore` (Streamable HTTP, stateless) |
| Logging | Serilog with source-generated `LoggerMessage` extensions |
| Testing | xunit v3 + `FunFair.Test.Common` + `NSubstitute` |
| Container | `mcr.microsoft.com/dotnet/runtime-deps:10.0-noble` |
