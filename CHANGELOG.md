# Changelog

All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]

### Security

### Added

- **Data source column** — each pool row now shows whether data originates from DefiLlama or Pendle
- **Exploit risk column** — pools are matched against the DefiLlama hacks dataset; affected protocols display an amber ⚠️ badge with incident count and a hover tooltip showing name, year, amount stolen, and technique
- **`HackInfo` shared type** — new interface in `@shared` for exploit incident data

### Fixed

- Server endpoints returning 500 when the DefiLlama hacks API responded with HTTP 429 (rate limit); now degrades gracefully to an empty hack map so pool data still loads

### Changed

- **Server restructured** into distinct layers:
  - `api/` — third-party API clients (`defillama.pools.api.service.ts`, `defillama.hacks.api.service.ts`, `pendle.markets.api.service.ts`)
  - `db/` — SQLite persistent cache (`cache.db.ts`) via `better-sqlite3`
  - `services/` — internal business logic (`pools.service.ts`, `hacks.service.ts`)
  - `utils/` — shared utilities (`slug.utils.ts`)
  - `server/` — HTTP routing only (`server-fastify.ts`)
- **Persistent API cache** — third-party data is cached in SQLite (`packages/server/data/cache.db`) and survives server restarts; data fresher than 1 hour is served directly from cache; stale data (1–2 hours) is used as fallback if a live fetch fails; data older than 2 hours is always refetched
- Sorting by APY descending then TVL

### Removed

### Deployment Changes

- `better-sqlite3` added as a server dependency (requires native build tools)

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [0.0.0] - Project created
