export interface PoolData {
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
