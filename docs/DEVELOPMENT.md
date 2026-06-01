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

The server listens on `http://localhost:5000` (plain HTTP) when no cert is present.

To test HTTPS locally, generate a self-signed PFX first:

```sh
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /tmp/server.key -out /tmp/server.crt -subj "/CN=localhost"
openssl pkcs12 -export -in /tmp/server.crt -inkey /tmp/server.key \
  -out /tmp/server.pfx -passout pass:
CERT_PATH=/tmp/server.pfx dotnet run --project Credfeto.Defi.Server
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
  Credfeto.Defi.Server/
    ApiClients/     HTTP clients (DefiLlama, Pendle, CoinGecko, GoPlus)
    Cache/          SQLite cache service
    Config/         Configuration records
    Endpoints/      REST + health-check route handlers
    Helpers/        Kestrel TLS / HTTP/3 setup
    Json/           Source-generated JSON context
    Mcp/            MCP server tools
    Models/         Domain models
    Services/       Business logic
    Utils/          Utilities
  Credfeto.Defi.Server.Tests/
    268 xunit v3 unit tests
```

## Environment Variables

| Variable | Default | Description |
| --- | --- | --- |
| `CERT_PATH` | `/app/data/server.pfx` | PFX cert path; HTTPS disabled if absent |
| `Cache__DbDirectory` | `/app/data` | SQLite cache directory |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `Rpc__Ethereum` | _(empty)_ | Ethereum RPC endpoint |
| `Rpc__Arbitrum` | _(empty)_ | Arbitrum RPC endpoint |

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

1. Generates `server.pfx` at `$CERT_PATH` on first start
2. Listens on 8081 (HTTPS, HTTP/1.1 + HTTP/2 + HTTP/3)
3. Exposes as 443/tcp and 443/udp via docker-compose

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
lsof -ti:5000 | xargs kill -9
```
