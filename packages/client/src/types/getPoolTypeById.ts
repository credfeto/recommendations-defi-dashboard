import { POOL_TYPES } from './poolTypesConfig';
import { PoolTypeConfig } from './poolTypeConfig';

export const getPoolTypeById = (typeId: string): PoolTypeConfig | undefined => {
  return POOL_TYPES[typeId.toUpperCase()];
};
