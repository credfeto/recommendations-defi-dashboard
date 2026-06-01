# DeFi Dashboard

A server-only application that aggregates and filters DeFi liquidity pools from the Llama Yields API, exposes them via a REST API and an MCP server, and ships as a single native-AOT Docker container with built-in TLS and HTTP/3.

## Features

- Browse liquidity pools from [Llama Yields API](https://yields.llama.fi/)
- Filter pools by type: ETH, Stablecoins, High Yield, Blue Chip, and Low TVL
- Stablecoin depeg monitoring with per-token price deviation alerts
- Contract security scoring via GoPlus API
- Hack/exploit risk flagging from the DefiLlama hacks dataset
- Server-side SQLite cache (1-hour TTL) to reduce external API calls
- MCP server exposing `get_pool_types`, `get_pools`, and `check_contract_security` tools
- Self-signed `server.pfx` generated at container startup — HTTPS and HTTP/3 with no nginx

## Quick Start

### Prerequisites

- .NET 10 SDK
- Docker (for container deployment)

### Development

```sh
cd src
dotnet run --project Credfeto.Defi.Server
```

The server listens on `http://localhost:5000` by default (plain HTTP, no cert needed for dev).

To enable HTTPS locally, generate a cert and set `CERT_PATH`:

```sh
CERT_PATH=/tmp/server.pfx dotnet run --project Credfeto.Defi.Server
```

### Docker

```sh
docker compose up -d
```

The container generates a self-signed `server.pfx` at `$CERT_PATH` on first start and listens on port 443 over HTTPS + HTTP/3. Mount a real cert to override:

```sh
docker run -p 443:8081 -p 443:8081/udp \
  -e CERT_PATH=/certs/server.pfx \
  -v /path/to/certs:/certs \
  credfeto/defi:latest
```

## API

```text
GET /api/pools              — list available pool types
GET /api/pools/:poolName    — pools for a given type
POST|GET|DELETE /mcp        — MCP Streamable HTTP endpoint
GET /ping                   — health check -> 200 "pong"
```

Supported pool types: `ETH`, `STABLES`, `HIGH_YIELD`, `LOW_TVL`, `BLUE_CHIP`

See [docs/API.md](./docs/API.md) for full request/response documentation.

## Project Structure

```text
src/
  Credfeto.Defi.Server/          ASP.NET Core server (native AOT)
    ApiClients/                  DefiLlama, CoinGecko, Pendle, GoPlus HTTP clients
    Cache/                       SQLite-backed API cache service
    Config/                      Strongly-typed configuration records
    Endpoints/                   Minimal-API route handlers
    Helpers/                     Kestrel TLS + HTTP/3 configuration
    Mcp/                         MCP server tools and setup
    Models/                      Response and domain models
    Services/                    Pool filtering, depeg, hacks, enrichment
    Utils/                       Contract address and slug utilities
  Credfeto.Defi.Server.Tests/    xunit v3 unit tests (268 tests)
docs/                            Architecture, API reference, development guide
```

## Environment Variables

| Variable | Default | Description |
| --- | --- | --- |
| `CERT_PATH` | `/app/data/server.pfx` | Path to TLS PFX certificate; HTTPS disabled if file absent |
| `Cache__DbDirectory` | `/app/data` | Directory for the SQLite cache database |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment name |

## Documentation

- [Architecture](./docs/ARCHITECTURE.md)
- [API Reference](./docs/API.md)
- [Development Guide](./docs/DEVELOPMENT.md)
- [Testing Guide](./docs/TESTING.md)
- [Pool Types Guide](./docs/POOL_TYPES_GUIDE.md)
- [Changelog](./CHANGELOG.md)
- [Contributing](./CONTRIBUTING.md)
- [Security](./SECURITY.md)
