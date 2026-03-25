import { PoolTypeMetadata } from './poolTypeMetadata';
import { POOL_TYPES_METADATA } from './poolTypesMetadataConfig';

export const getAvailablePoolTypesMetadata = (): PoolTypeMetadata[] => {
  return Object.values(POOL_TYPES_METADATA);
};
