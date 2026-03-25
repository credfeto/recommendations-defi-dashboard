/**
 * Pool Type Metadata
 * Represents the publicly exposed metadata for available pool types
 */

export interface PoolTypeMetadata {
  name: string;
  displayName: string;
}

export const POOL_TYPES_METADATA: Record<string, PoolTypeMetadata> = {
  ETH: {
    name: 'ETH',
    displayName: 'Ethereum',
  },
  STABLES: {
    name: 'STABLES',
    displayName: 'Stablecoins',
  },
  LST: {
    name: 'LST',
    displayName: 'Liquid Staking',
  },
  HIGH_YIELD: {
    name: 'HIGH_YIELD',
    displayName: 'High Yield',
  },
  LOW_TVL: {
    name: 'LOW_TVL',
    displayName: 'Emerging Pools',
  },
  BLUE_CHIP: {
    name: 'BLUE_CHIP',
    displayName: 'Blue Chip',
  },
};

export const getAvailablePoolTypesMetadata = (): PoolTypeMetadata[] => {
  return Object.values(POOL_TYPES_METADATA);
};
