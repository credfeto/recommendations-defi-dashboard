export interface PoolTypeConfig {
  id: string;
  name: string;
  description: string;
  predicate: (pool: any) => boolean;
}
