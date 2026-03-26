# Changelog

All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]

### Security

### Added

- **Stablecoin depeg monitoring** — for pools marked as stablecoins, individual token symbols are looked up against the CoinGecko stablecoins market data; pools whose tokens deviate >0.5% from their $1 peg show a ⚠️ warning badge; deviations >2% show a 🚨 critical badge with token symbol, live price, and percentage deviation in the tooltip
- **CoinGecko stablecoins API service** — new `coingecko.stablecoins.api.service.ts` fetches all stablecoin prices (paginated) and is cached in SQLite for up to 1 hour following the same cache policy as other third-party data sources
- **`DepegAlert` shared type** — new interface in `@shared` exposing `symbol`, `currentPrice`, `pegPrice`, `deviation`, and `severity`
- **Depeg service** — `depeg.service.ts` provides `buildStablecoinPriceMap()` and `checkDepeg()` for symbol-to-price lookup and threshold evaluation
- **Data source column** — each pool row now shows whether data originates from DefiLlama or Pendle
- **Exploit risk column** — pools are matched against the DefiLlama hacks dataset; affected protocols display an amber ⚠️ badge with incident count and a hover tooltip showing name, year, amount stolen, and technique
- **Protocol page links** — the Project column links directly to the pool on its source platform (DefiLlama yield page or Pendle market page), generated server-side
- **`HackInfo` shared type** — new interface in `@shared` for exploit incident data

### Fixed

- Server endpoints returning 500 when the DefiLlama hacks API responded with HTTP 429 (rate limit); now degrades gracefully to an empty hack map so pool data still loads

### Changed

- **Server restructured** into distinct layers:
  - `api/` — third-party API clients (`defillama.pools.api.service.ts`, `defillama.hacks.api.service.ts`, `pendle.markets.api.service.ts`)
  - `db/` — SQLite persistent cache (`cache.db.ts`) via `better-sqlite3`
  - `services/` — internal business logic (`pools.service.ts`, `hacks.service.ts`, `pool-url.service.ts`)
  - `utils/` — shared utilities (`slug.utils.ts`)
  - `server/` — HTTP routing only (`server-fastify.ts`)
- **Pool URL generation moved to server** — the `url` field is now computed server-side in `pool-url.service.ts` and included in the API response; the client no longer needs any knowledge of data sources to render links
- **Persistent API cache** — third-party data is cached in SQLite (`packages/server/data/cache.db`) and survives server restarts; data fresher than 1 hour is served directly from cache; stale data (1–2 hours) is used as fallback if a live fetch fails; data older than 2 hours is always refetched
- Sorting by APY descending then TVL

### Removed

### Deployment Changes

- `better-sqlite3` added as a server dependency (requires native build tools)

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [0.0.0] - Project created
