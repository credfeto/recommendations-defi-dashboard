import { PoolTypeConfig } from './poolTypeConfig';

export const POOL_TYPES: Record<string, PoolTypeConfig> = {
  ETH: {
    id: 'ETH',
    name: 'Ethereum & Liquid Staking',
    description: 'Pools featuring ETH, ETH derivative tokens, and liquid staking tokens',
    predicate: (pool) => {
      const symbolUpper = pool.symbol.toUpperCase();
      // Include ETH-based pools
      if (symbolUpper.includes('ETH')) return true;
      // Include liquid staking tokens
      const lstSymbols = ['STETH', 'RETH', 'CBETH', 'SWELL', 'LSETH'];
      return lstSymbols.some((s) => symbolUpper.includes(s));
    },
  },
  STABLES: {
    id: 'STABLES',
    name: 'Stablecoin Pools',
    description: 'Pools featuring stablecoin tokens',
    predicate: (pool) => pool.stablecoin === true,
  },
  HIGH_YIELD: {
    id: 'HIGH_YIELD',
    name: 'High Yield (>5% APY)',
    description: 'Pools with APY greater than 5%',
    predicate: (pool) => pool.apy > 5,
  },
  LOW_TVL: {
    id: 'LOW_TVL',
    name: 'Emerging Pools (<$10M TVL)',
    description: 'Newer pools with lower TVL',
    predicate: (pool) => pool.tvlUsd < 10_000_000,
  },
  BLUE_CHIP: {
    id: 'BLUE_CHIP',
    name: 'Blue Chip (>$100M TVL)',
    description: 'Established pools with high TVL',
    predicate: (pool) => pool.tvlUsd > 100_000_000,
  },
};
