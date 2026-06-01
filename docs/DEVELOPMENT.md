# Development Guide

## Prerequisites

- .NET 10 SDK (`dotnet --version` should show `10.x`)
- Docker (for container builds and deployment)

## Getting Started

### 1. Clone and restore

```sh
git clone https://github.com/credfeto/recommendations-defi-dashboard.git
cd recommendations-defi-dashboard/src
dotnet restore Credfeto.Defi.slnx
```

### 2. Run in development

```sh
cd src
dotnet run --project Credfeto.Defi.Server
```

The server listens on:

- `http://localhost:8080` (plain HTTP, loopback only — used by the health check)
- `https://localhost:8081` (HTTPS/HTTP/2/HTTP/3) when `server.pfx` is present

Ports are hardcoded: HTTP on **8080**, HTTPS on **8081**. The certificate path is hardcoded to `<AppContext.BaseDirectory>/server.pfx` (not configurable via environment variable).

A dev self-signed cert (`server.pfx`) is already committed in `src/Credfeto.Defi.Server/` and is copied to the build output automatically, so `dotnet run` will start HTTPS straight away. To regenerate it:

```sh
openssl req -x509 -nodes -days 3650 -newkey rsa:2048 \
  -keyout /tmp/server.key -out /tmp/server.crt -subj "/CN=localhost"
openssl pkcs12 -export -in /tmp/server.crt -inkey /tmp/server.key \
  -out src/Credfeto.Defi.Server/server.pfx -passout pass:
```

## Commands

```sh
cd src
dotnet build Credfeto.Defi.slnx -c Release
dotnet test Credfeto.Defi.Server.Tests/Credfeto.Defi.Server.Tests.csproj \
  -c Release -p:SolutionDir=$(pwd)/
dotnet buildcheck -Solution Credfeto.Defi.slnx
```

## Project Structure

```text
src/
  Credfeto.Defi.Server/              Minimal executable (Program, KestrelConfig, Endpoints, ServiceRegistration)
  Credfeto.Defi.Data.Models/         Domain models, AppJsonContext, CacheConfig, RpcConfig
  Credfeto.Defi.ApiClients.CoinGecko.Interfaces/   ICoinGeckoStablecoinsClient
  Credfeto.Defi.ApiClients.CoinGecko/              CoinGeckoStablecoinsClient
  Credfeto.Defi.ApiClients.DefiLlama.Interfaces/   IDefiLlama*Client
  Credfeto.Defi.ApiClients.DefiLlama/              DefiLlama*Client
  Credfeto.Defi.ApiClients.GoPlus.Interfaces/      IGoPlusClient
  Credfeto.Defi.ApiClients.GoPlus/                 GoPlusClient
  Credfeto.Defi.ApiClients.Pendle.Interfaces/      IPendleMarketsClient
  Credfeto.Defi.ApiClients.Pendle/                 PendleMarketsClient
  Credfeto.Defi.Database/            SQLite cache services (ApiCacheService, ContractSecurityCacheService)
  Credfeto.Defi.Services/            Business logic (pool enrichment, filtering, hacks, depeg, etc.)
  Credfeto.Defi.Mcp/                 MCP tools (DefiMcpTools) and setup
  Credfeto.Defi.Server.Tests/        268 xunit v3 unit tests
```

## Environment Variables

| Variable | Default | Description |
| --- | --- | --- |
| `Cache__DbDirectory` | `/app/data` | SQLite cache directory |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `Rpc__Ethereum` | _(empty)_ | Ethereum RPC endpoint |
| `Rpc__Arbitrum` | _(empty)_ | Arbitrum RPC endpoint |

Note: HTTP/HTTPS ports (8080/8081) and the cert path (`<AppContext.BaseDirectory>/server.pfx`) are hardcoded and not configurable via environment variables.

## Adding a New Pool Type

See [Pool Types Guide](./POOL_TYPES_GUIDE.md).

Quick summary: add a new entry to `PoolTypeService`'s type map.
The API endpoint is generated automatically from the pool type ID.

## Running Tests

```sh
cd src
dotnet test Credfeto.Defi.Server.Tests/Credfeto.Defi.Server.Tests.csproj \
  -c Release \
  -p:SolutionDir=$(pwd)/
```

See [Testing Guide](./TESTING.md) for coverage and patterns.

## Docker

```sh
docker build -t defi:local .
docker compose up -d
```

The container:

1. Generates `server.pfx` at `<AppContext.BaseDirectory>/server.pfx` on first start (if not already present)
2. Listens on 8080 (HTTP, health check) and 8081 (HTTPS, HTTP/1.1 + HTTP/2 + HTTP/3)
3. Exposes as 8080/tcp, 8081/tcp and 8081/udp (QUIC) via docker-compose

## Troubleshooting

### Build errors

```sh
dotnet buildcheck -Solution src/Credfeto.Defi.slnx
```

### Tests failing

```sh
cd src
dotnet test Credfeto.Defi.Server.Tests/Credfeto.Defi.Server.Tests.csproj --verbosity detailed
```

### Port in use

```sh
lsof -ti:8080 | xargs kill -9
lsof -ti:8081 | xargs kill -9
```
