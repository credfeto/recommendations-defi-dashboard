import { HackInfo } from './hackInfo';

export interface PoolData {
  symbol: string;
  hacks: HackInfo[];
  chain: string;
  project: string;
  tvlUsd: number;
  apy: number;
  apyBase?: number;
  apyReward?: number | null;
  ilRisk: string;
  stablecoin: boolean;
  pool: string;
  dataSource: string;
  [key: string]: any;
}
