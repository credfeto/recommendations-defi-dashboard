import { POOL_TYPES } from './poolTypesConfig';
import { PoolTypeConfig } from './poolTypeConfig';

export const getAvailablePoolTypes = (): PoolTypeConfig[] => {
  return Object.values(POOL_TYPES);
};
