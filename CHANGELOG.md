# Changelog

All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]

### Security

### Added

- **`docs/api.http`** — VS Code REST Client examples for all REST API endpoints (`GET /api/pools` and `GET /api/pools/:poolName` for all five pool types: ETH, STABLES, HIGH_YIELD, LOW_TVL, BLUE_CHIP)

- **`docker-compose.yml`** — local deployment config bringing up the `defi` service (image `credfeto/defi:latest`, port `443:443`, `./data:/app/data` volume for SQLite persistence) alongside `watchtower` (image `nickfedor/watchtower:latest`, 15-minute poll interval, monitors only the `defi` container via `WATCHTOWER_CONTAINER_LIST`)
- **Dockerfile** — multi-stage Docker build: Stage 1 builds the React client with Vite; Stage 2 produces a runtime image combining nginx (reverse proxy with self-signed TLS for `defi.local`/`localhost`) and Node/ts-node for the Fastify server; nginx serves static assets on port 443, proxies `/api/*` to port 5000, and applies appropriate `Cache-Control` headers (1h for regular assets, immutable for hashed bundles)
- **GitHub Actions docker.yml workflow** — builds the Docker image and pushes `credfeto/defi:latest` to the configured registry on every push to `main`; registry credentials are read from `DOCKER_REGISTRY_URL`, `DOCKER_REGISTRY_USERNAME`, and `DOCKER_REGISTRY_PASSWORD` secrets
- **`DB_DIR` environment variable** — the SQLite cache database path is now configurable via `DB_DIR` (defaults to the existing relative path); set `DB_DIR=/app/data` in production to isolate the DB in a dedicated directory that can be bind-mounted

- **Stablecoin depeg monitoring** — for pools marked as stablecoins, individual token symbols are looked up against the CoinGecko stablecoins market data; pools whose tokens deviate >0.5% from their $1 peg show a ⚠️ warning badge; deviations >2% show a 🚨 critical badge with token symbol, live price, and percentage deviation in the tooltip
- **Depeg check extended to all pools via underlyingTokens** — depeg checking now runs on all pools; a CoinGecko coin list (address→stablecoin mapping) is fetched and cached, allowing `underlyingTokens` contract addresses to be resolved to stablecoin prices and checked for depeg; pools with any depeg alert are excluded from all responses
- **CoinGecko stablecoins API service** — new `coingecko.stablecoins.api.service.ts` fetches all stablecoin prices (paginated) and is cached in SQLite for up to 1 hour following the same cache policy as other third-party data sources
- **`DepegAlert` shared type** — new interface in `@shared` exposing `symbol`, `currentPrice`, `pegPrice`, `deviation`, and `severity`
- **Depeg service** — `depeg.service.ts` provides `buildStablecoinPriceMap()` and `checkDepeg()` for symbol-to-price lookup and threshold evaluation
- **Data source column** — each pool row now shows whether data originates from DefiLlama or Pendle
- **Exploit risk column** — pools are matched against the DefiLlama hacks dataset; affected protocols display an amber ⚠️ badge with incident count and a hover tooltip showing name, year, amount stolen, and technique
- **Protocol page links** — the Project column links directly to the pool on its source platform (DefiLlama yield page or Pendle market page), generated server-side
- **`HackInfo` shared type** — new interface in `@shared` for exploit incident data
- JSON Schema validation and response serialization for all Fastify API routes via schemas.ts
- GitHub Actions CI workflows for tests, build, and code quality checks
- PR description template and automated maintenance workflow to initialize and update descriptions for draft pull requests
- Auto-generate PR descriptions using GitHub Models API (gpt-4o-mini) when the Description section is empty; description is maintained by regenerating on new commits unless manually edited
- Warm API cache on startup — missing or stale cache entries are fetched in the background when the server starts; errors are logged per entry and do not block other fetches or server startup
- Add MCP server exposing get_pool_types, get_pools, and check_contract_security tools
- Add unit tests, e2e tests, and JSON-RPC examples for the MCP server

### Fixed

- Docker runtime stage no longer errors with `sh: husky: not found` when installing production-only dependencies; the root `prepare` script now guards the `husky` call with `[ -d .git ]` so it is a no-op in Docker (no `.git` directory) while continuing to set up git hooks normally in development
- TypeScript strict mode errors in `packages/server`: index-access safety for `RPC_ENV` in `rpc.config.ts`, optional-chaining for `process.env['PORT']` in `server-fastify.ts`, and explicit `underlyingTokens` type casts in `server-fastify.ts` and `poolTypesConfig.ts`
- Server endpoints returning 500 when the DefiLlama hacks API responded with HTTP 429 (rate limit); now degrades gracefully to an empty hack map so pool data still loads
- Reduced Docker image size by using multi-stage build to compile TypeScript and installing only production dependencies in the final image

### Changed

- Bump electron-to-chromium from 1.5.330 to 1.5.331
- Bump node-releases from 2.0.36 to 2.0.37
- Bump @sinonjs/fake-timers from 15.2.1 to 15.3.0
- Bump ts-jest from 29.4.6 to 29.4.9
- Bump @emnapi/core from 1.9.1 to 1.9.2
- Bump @emnapi/runtime from 1.9.1 to 1.9.2
- Bump caniuse-lite from 1.0.30001782 to 1.0.30001784
- **Server restructured** into distinct layers:
  - `api/` — third-party API clients (`defillama.pools.api.service.ts`, `defillama.hacks.api.service.ts`, `pendle.markets.api.service.ts`)
  - `db/` — SQLite persistent cache (`cache.db.ts`) via `better-sqlite3`
  - `services/` — internal business logic (`pools.service.ts`, `hacks.service.ts`, `pool-url.service.ts`)
  - `utils/` — shared utilities (`slug.utils.ts`)
  - `server/` — HTTP routing only (`server-fastify.ts`)
- **Pool URL generation moved to server** — the `url` field is now computed server-side in `pool-url.service.ts` and included in the API response; the client no longer needs any knowledge of data sources to render links
- **Persistent API cache** — third-party data is cached in SQLite (`packages/server/data/cache.db`) and survives server restarts; data fresher than 1 hour is served directly from cache; stale data (1–2 hours) is used as fallback if a live fetch fails; data older than 2 hours is always refetched
- Sorting by APY descending then TVL
- Enable HTTP response caching (15s TTL with stale-while-revalidate) and brotli/gzip compression via @fastify/compress
- Replace react-scripts (CRA) and craco with Vite, eliminating webpack-dev-server deprecation warnings
- MCP server mounted on existing Fastify server at POST /mcp (Streamable HTTP, stateless)

### Removed

### Deployment Changes

- `better-sqlite3` added as a server dependency (requires native build tools)

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [0.0.0] - Project created