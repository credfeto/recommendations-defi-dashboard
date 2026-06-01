# DeFi Dashboard

A server-only application that aggregates and filters DeFi liquidity pools from the Llama Yields API, exposes them via a REST API and an MCP server, and ships as a single Docker container with built-in TLS.

## Features

- Browse liquidity pools from [Llama Yields API](https://yields.llama.fi/)
- Filter pools by type: ETH, Stablecoins, High Yield, Blue Chip, and Low TVL
- Stablecoin depeg monitoring with per-token price deviation alerts
- Contract security scoring via GoPlus API
- Hack/exploit risk flagging from the DefiLlama hacks dataset
- Server-side SQLite cache (1-hour TTL) to reduce external API calls
- MCP server exposing `get_pool_types`, `get_pools`, and `check_contract_security` tools
- Self-signed TLS generated at container startup — no nginx required

## Quick Start

### Prerequisites

- Node.js 26.x or higher
- npm 11.x or higher

### Development

```bash
npm install
npm run dev        # server on http://localhost:3000
npm test           # run all tests
npm run test:coverage
```

### Docker

```sh
docker compose up -d
```

The container generates a self-signed cert at startup and listens on port 443 over HTTPS.

Supply your own certificate by mounting key/cert files and overriding the env vars:

```sh
docker run -p 443:443 \
  -e TLS_KEY_PATH=/certs/server.key \
  -e TLS_CERT_PATH=/certs/server.crt \
  -v /path/to/certs:/certs \
  credfeto/defi:latest
```

## API

```text
GET /api/pools              — list available pool types
GET /api/pools/:poolName    — pools for a given type
POST|GET|DELETE /mcp        — MCP Streamable HTTP endpoint
```

Supported pool types: `ETH`, `STABLES`, `HIGH_YIELD`, `LOW_TVL`, `BLUE_CHIP`

See [docs/API.md](./docs/API.md) for full request/response documentation and examples.

## Project Structure

```text
packages/
  server/          Fastify server, services, MCP, SQLite cache
  shared/          Shared TypeScript types
docs/              Architecture, API reference, development guide
```

## Environment Variables

| Variable | Default | Description |
| --- | --- | --- |
| `PORT` | `3000` | HTTP/HTTPS listen port |
| `TLS_KEY_PATH` | _(unset)_ | Path to TLS private key; plain HTTP if unset |
| `TLS_CERT_PATH` | _(unset)_ | Path to TLS certificate; plain HTTP if unset |
| `DB_DIR` | _(cwd)_ | Directory for the SQLite cache database |
| `NODE_ENV` | `development` | Set to `production` in Docker |

## Documentation

- [Architecture](./docs/ARCHITECTURE.md)
- [API Reference](./docs/API.md)
- [Development Guide](./docs/DEVELOPMENT.md)
- [Testing Guide](./docs/TESTING.md)
- [Pool Types Guide](./docs/POOL_TYPES_GUIDE.md)
- [Changelog](./CHANGELOG.md)
- [Contributing](./CONTRIBUTING.md)
- [Security](./SECURITY.md)
