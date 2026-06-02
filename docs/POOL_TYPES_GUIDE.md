# Pool Types Guide

## Overview

Pool types are defined in `PoolTypeService` inside `src/Credfeto.Defi.Server/Services/PoolTypeService.cs`. Each type has an ID, a display name, and a predicate function that decides whether a given pool belongs to that type.

## Built-in Pool Types

| ID | Display Name | Criteria |
| --- | --- | --- |
| `ETH` | Ethereum | Pool symbol contains ETH or a liquid-staking derivative |
| `STABLES` | Stablecoins | Pool is marked as stablecoin |
| `HIGH_YIELD` | High Yield | APY above threshold |
| `LOW_TVL` | Emerging | TVL below threshold |
| `BLUE_CHIP` | Blue Chip | TVL above threshold |

## Adding a New Pool Type

1. Open `src/Credfeto.Defi.Server/Services/PoolTypeService.cs`.
2. Add a new entry to the pool type map with the desired ID, name, and predicate:

```csharp
new PoolTypeDefinition(
    Id: "BITCOIN",
    DisplayName: "Bitcoin",
    Matches: static pool =>
        pool.Symbol.Contains("WBTC", StringComparison.OrdinalIgnoreCase) ||
        pool.Symbol.Contains("BTC", StringComparison.OrdinalIgnoreCase)
)
```

1. The `GET /api/pools/BITCOIN` endpoint is created automatically — no changes to routing needed.

## Core Concepts

### Base Filters

All pool types automatically exclude:

- Pools with any depeg alert (stablecoin price deviation)
- Pools returned by the enrichment pipeline with missing data

### Pool Enrichment

Every pool is enriched with:

- `Hacks` — matched against the DefiLlama hacks dataset
- `DepegAlerts` — checked against CoinGecko stablecoin prices
- `AuditInfo` — matched from DefiLlama protocol audit data
- `AccessInfo` — derived from `poolMeta` text (KYC, liquidity flags)
- `ContractAddresses` — aggregated from underlying/reward tokens
- `ContractSecurity` — GoPlus security scores (24-hour SQLite cache)
