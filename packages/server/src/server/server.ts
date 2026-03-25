import { PoolTypeConfig } from '../types/poolTypeConfig';
import { getPoolTypeById } from '../types/getPoolTypeById';
import { getAvailablePoolTypes } from '../types/getAvailablePoolTypes';

interface PoolData {
  symbol: string;
  chain: string;
  project: string;
  tvlUsd: number;
  apy: number;
  apyBase?: number;
  apyReward?: number | null;
  ilRisk: string;
  stablecoin: boolean;
  pool: string;
  [key: string]: any;
}

const MIN_TVL = 1_000_000;

// Add chain names here (case-insensitive) to exclude them from all responses
const EXCLUDED_CHAINS: string[] = ['Tron', 'Sui'];

export const applyBaseFilters = (poolData: PoolData[]): PoolData[] => {
  const excludedLower = EXCLUDED_CHAINS.map((c) => c.toLowerCase());
  return poolData
    .filter(
      (pool) =>
        pool.ilRisk === 'no' &&
        pool.tvlUsd >= MIN_TVL &&
        pool.apy > 0 &&
        !excludedLower.includes(pool.chain.toLowerCase()),
    )
    .sort((a, b) => b.apy - a.apy || b.tvlUsd - a.tvlUsd);
};

export const filterPoolsByType = (allPools: PoolData[], poolType: string): PoolData[] => {
  const typeConfig = getPoolTypeById(poolType);

  if (!typeConfig) {
    return [];
  }

  const filteredByType = allPools.filter(typeConfig.predicate);
  return applyBaseFilters(filteredByType);
};

export const getAvailableTypes = (): PoolTypeConfig[] => {
  return getAvailablePoolTypes();
};

export const getFilteredPools = (allPools: PoolData[], poolType: string) => {
  return filterPoolsByType(allPools, poolType);
};

// Legacy function names for backward compatibility
export const filterPools = applyBaseFilters;
export const getPoolsByType = filterPoolsByType;
