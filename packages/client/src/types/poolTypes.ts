/**
 * Pool Type Configuration System
 * Defines how different pool categories are identified and filtered
 */

export interface PoolTypeConfig {
  id: string;
  name: string;
  description: string;
  predicate: (pool: any) => boolean;
}

export const POOL_TYPES: Record<string, PoolTypeConfig> = {
  ETH: {
    id: 'ETH',
    name: 'ETH-Based Pools',
    description: 'Pools featuring ETH or ETH derivative tokens',
    predicate: (pool) => pool.symbol.toUpperCase().includes('ETH'),
  },
  STABLES: {
    id: 'STABLES',
    name: 'Stablecoin Pools',
    description: 'Pools featuring stablecoin tokens',
    predicate: (pool) => pool.stablecoin === true,
  },
  LST: {
    id: 'LST',
    name: 'Liquid Staking Tokens',
    description: 'Pools featuring liquid staking tokens (stETH, rETH, etc)',
    predicate: (pool) => {
      const symbols = ['STETH', 'RETH', 'CBETH', 'SWELL', 'LSETH'];
      return symbols.some((s) => pool.symbol.toUpperCase().includes(s));
    },
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

export const getAvailablePoolTypes = (): PoolTypeConfig[] => {
  return Object.values(POOL_TYPES);
};

export const getPoolTypeById = (typeId: string): PoolTypeConfig | undefined => {
  return POOL_TYPES[typeId.toUpperCase()];
};
