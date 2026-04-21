import { PoolTypeConfig } from './poolTypeConfig';

import { PoolData } from '../shared/types/poolData';

export const POOL_TYPES: Record<string, PoolTypeConfig> = {
  ETH: {
    id: 'ETH',
    name: 'Ethereum & Liquid Staking',
    description: 'Pools featuring ETH, ETH derivative tokens, and liquid staking tokens',
    predicate: (pool: PoolData): boolean => {
      const symbolUpper = pool.symbol.toUpperCase();
      const lstSymbols = ['ETH', 'STETH', 'WSTETH', 'RETH', 'CBETH', 'SWETH', 'LSETH', 'EETH', 'WEETH'];
      if (lstSymbols.some((s) => symbolUpper.includes(s))) {
        return true;
      }

      const underlyingTokens = pool['underlyingTokens'] as string[] | undefined;
      if (underlyingTokens && underlyingTokens.length !== 0) {
        for (const underlyingToken of underlyingTokens) {
          const underlyingSymbolUpper = underlyingToken.toUpperCase();
          if (lstSymbols.some((s) => underlyingSymbolUpper.includes(s))) {
            return true;
          }
        }
      }

      return false;
    },
  },
  STABLES: {
    id: 'STABLES',
    name: 'Stablecoin Pools',
    description: 'Pools featuring stablecoin tokens',
    predicate: (pool: PoolData): boolean => pool.stablecoin === true,
  },
  HIGH_YIELD: {
    id: 'HIGH_YIELD',
    name: 'High Yield (>5% APY)',
    description: 'Pools with APY greater than 5%',
    predicate: (pool: PoolData): boolean => pool.apy > 5,
  },
  LOW_TVL: {
    id: 'LOW_TVL',
    name: 'Emerging Pools (<$10M TVL)',
    description: 'Newer pools with lower TVL',
    predicate: (pool: PoolData): boolean => pool.tvlUsd < 10_000_000,
  },
  BLUE_CHIP: {
    id: 'BLUE_CHIP',
    name: 'Blue Chip (>$100M TVL)',
    description: 'Established pools with high TVL',
    predicate: (pool: PoolData): boolean => pool.tvlUsd > 100_000_000,
  },
};
