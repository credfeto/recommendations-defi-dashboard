# Development Guide

## Prerequisites

- Node.js 26.x or higher
- npm 11.x or higher
- Git

## Getting Started

### 1. Clone and Install Dependencies

```bash
git clone https://github.com/credfeto/recommendations-defi-dashboard.git
cd recommendations-defi-dashboard
npm install
```

### 2. Development Mode

Start the server:

```bash
npm run dev
```

Server listens on port 3000 (HTTP) by default.

### 3. Individual Commands

```sh
npm run dev             # Start server (port 3000)
npm test                # Run all tests
npm run test:coverage   # Run tests with coverage report
npm run test:watch      # Watch mode
```

## Project Structure

```text
packages/
  server/
    src/
      api/        Third-party API clients (DefiLlama, Pendle, CoinGecko)
      db/         SQLite persistent cache
      mcp/        MCP server tools
      server/     Fastify HTTP routing (server-fastify.ts)
      services/   Business logic (pools, hacks, depeg, etc.)
      types/      Derived types and metadata
      utils/      Shared utilities
  shared/
    src/          Shared TypeScript type declarations (@shared alias)
```

## Environment Variables

| Variable | Default | Description |
| --- | --- | --- |
| `PORT` | `3000` | Listen port |
| `TLS_KEY_PATH` | _(unset)_ | Path to TLS private key; plain HTTP if unset |
| `TLS_CERT_PATH` | _(unset)_ | Path to TLS certificate; plain HTTP if unset |
| `DB_DIR` | _(cwd)_ | Directory for the SQLite cache database |
| `NODE_ENV` | `development` | Set to `production` in Docker |

## Adding a New Pool Type

See [Pool Types Guide](./POOL_TYPES_GUIDE.md) for detailed instructions.

Quick summary: edit `packages/server/src/types/poolTypesConfig.ts` and add an entry to `POOL_TYPES_CONFIG`. The API endpoint is generated automatically.

## Running Tests

```bash
npm test                          # all tests
npm run test:coverage             # with coverage
npm run test:watch                # watch mode
```

## Code Style

- **TypeScript** strict mode throughout
- **Prettier** for formatting
- **ESLint** for linting

Run formatters via pre-commit hooks automatically, or:

```bash
npx prettier --write .
```

## Debugging

The Fastify server logs to stdout in JSON format. Set `NODE_ENV=development` for pretty logs.

```bash
npm run dev
```

## Building for Production

```bash
npm --workspace=@defi-dashboard/server run build
# Output: packages/server/dist/
```

## Deployment

### Docker (Recommended)

```sh
docker compose up -d
```

The container:

1. Generates a self-signed TLS cert at `/etc/ssl/defi/` on first start
2. Starts the server on port 443

Supply your own cert by mounting files and overriding `TLS_KEY_PATH`/`TLS_CERT_PATH`.

### Manual

```sh
npm --workspace=@defi-dashboard/server run build
PORT=3000 node packages/server/dist/server/server-fastify.js
```

## Troubleshooting

### Port Already in Use

```bash
lsof -ti:3000 | xargs kill -9
```

### Dependencies Issues

```bash
rm -rf node_modules package-lock.json
npm install
```

### TypeScript Errors

```bash
npx tsc --noEmit
```

### Tests Failing

```bash
npm test -- --clearCache
npm test -- --verbose
```
