# Pool Types Configuration Guide

## Overview

The pool system has been refactored to be easily extensible. Pool types are now defined in a configuration system that makes it easy to add new categories without modifying core filtering logic.

## Adding a New Pool Type

To add a new pool type, simply add it to the `POOL_TYPES` object in `src/types/poolTypes.ts`:

```typescript
export const POOL_TYPES: Record<string, PoolTypeConfig> = {
  YOUR_TYPE: {
    id: 'YOUR_TYPE',
    name: 'Display Name',
    description: 'Description of this pool type',
    predicate: (pool) => {
      // Return true if pool matches this type
      return pool.someProperty === 'value';
    },
  },
  // ... other types
};
```

### Example: Adding a "Bitcoin" Pool Type

```typescript
BITCOIN: {
  id: 'BITCOIN',
  name: 'Bitcoin-Based Pools',
  description: 'Pools featuring BTC or wrapped Bitcoin tokens',
  predicate: (pool) => {
    const symbols = ['WBTC', 'BTC', 'BTCB', 'RENBTC', 'SBTC'];
    return symbols.some(s => pool.symbol.toUpperCase().includes(s));
  },
},
```

### Example: Adding a "Custom APY Range" Pool Type

```typescript
MEDIUM_YIELD: {
  id: 'MEDIUM_YIELD',
  name: 'Medium Yield (3-5% APY)',
  description: 'Pools with moderate yields between 3% and 5%',
  predicate: (pool) => pool.apy >= 3 && pool.apy <= 5,
},
```

## Backend Integration

The backend (`src/server.ts`) automatically supports new pool types through the existing `filterPoolsByType` function. No changes are needed to the API layer.

To use the new pool type, call the API endpoint:

```bash
GET /api/pools/YOUR_TYPE
```

## Frontend Integration

The React component (`src/FetchPools.tsx`) automatically fetches data for all available pool types using:

```typescript
const poolTypes = getAvailablePoolTypes();
```

The component will:
1. Display a button for each pool type in the sidebar
2. Fetch data for all types on load
3. Show pool details when a type is selected

## Core Concepts

### PoolTypeConfig Interface

```typescript
interface PoolTypeConfig {
  id: string;                          // Unique identifier (used in API)
  name: string;                        // Display name for UI
  description: string;                 // Tooltip text
  predicate: (pool: any) => boolean;   // Function to identify matching pools
}
```

### Base Filters

All pool types automatically apply these base filters:
- IL Risk = "no" (no impermanent loss)
- TVL ≥ $1,000,000 (minimum liquidity)
- APY > 0 (positive yields)

### Combining Filters

Pool types can have overlapping categories. For example, a pool can match both "ETH-Based" and "Liquid Staking Tokens" if it's stETH.

## Built-in Pool Types

1. **ETH** - ETH and ETH derivative tokens
2. **STABLES** - Stablecoin pools
3. **LST** - Liquid staking tokens (stETH, rETH, cbETH)
4. **HIGH_YIELD** - Pools with APY > 5%
5. **LOW_TVL** - Emerging pools with TVL < $10M
6. **BLUE_CHIP** - Established pools with TVL > $100M

## Usage in Code

```typescript
import { getAvailablePoolTypes, getPoolTypeById } from './types/poolTypes';

// Get all pool types
const allTypes = getAvailablePoolTypes();

// Get a specific pool type
const ethType = getPoolTypeById('ETH');

// Use the predicate to filter pools
const ethPools = allPools.filter(ethType.predicate);
```

## Testing

When adding a new pool type, add test cases in `src/__tests__/server.test.ts`:

```typescript
test('identifies your custom pool type correctly', () => {
  const customPools = filterPoolsByType(mockPoolData, 'YOUR_TYPE');
  expect(customPools.length).toBeGreaterThan(0);
  expect(customPools.every(p => /* your condition */)).toBe(true);
});
```

Ensure test coverage remains at 100%.
