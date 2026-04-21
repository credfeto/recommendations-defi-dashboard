import { PoolData } from '@shared/types/poolData';
import { PoolTypeConfig } from '../types/poolTypeConfig';
import { getPoolTypeById } from '../types/getPoolTypeById';
import { getAvailablePoolTypes } from '../types/getAvailablePoolTypes';

const MIN_TVL = 1_000_000;

// Add chain names here (case-insensitive) to exclude them from all responses
const EXCLUDED_CHAINS: string[] = [
  'Aptos',
  'Avalanche',
  'Cardano',
  'FileCoin',
  'Flare',
  'Flow',
  'Icp',
  'Stellar',
  'Sui',
  'Ton',
  'Tron',
  'Venom',
];

export const applyBaseFilters = (poolData: PoolData[]): PoolData[] => {
  const excludedLower = EXCLUDED_CHAINS.map((c) => c.toLowerCase());
  return poolData
    .filter(
      (pool) =>
        pool.ilRisk === 'no' &&
        pool.tvlUsd >= MIN_TVL &&
        pool.apy > 0 &&
        pool.apy < 100 &&
        !excludedLower.includes(pool.chain.toLowerCase()),
    )
    .sort((a, b) => b.apy - a.apy || b.tvlUsd - a.tvlUsd);
};

export const filterPoolsByType = (allPools: PoolData[], poolType: string): PoolData[] => {
  const typeConfig = getPoolTypeById(poolType);
  if (!typeConfig) return [];
  return applyBaseFilters(allPools.filter(typeConfig.predicate));
};

export const getAvailableTypes = (): PoolTypeConfig[] => getAvailablePoolTypes();

export const getFilteredPools = (allPools: PoolData[], poolType: string): PoolData[] =>
  filterPoolsByType(allPools, poolType);

// Legacy aliases kept for backward compatibility
export const filterPools = applyBaseFilters;
export const getPoolsByType = filterPoolsByType;
