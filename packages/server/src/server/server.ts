import { PoolTypeConfig, getPoolTypeById, getAvailablePoolTypes } from '../types/poolTypes';

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

export const applyBaseFilters = (poolData: PoolData[]): PoolData[] => {
  return poolData.filter((pool) => pool.ilRisk === 'no' && pool.tvlUsd >= MIN_TVL && pool.apy > 0);
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
