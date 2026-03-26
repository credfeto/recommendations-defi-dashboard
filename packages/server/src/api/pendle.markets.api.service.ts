import axios from 'axios';

const PENDLE_API_BASE = 'https://api-v2.pendle.finance/core/v1';

// Add chain IDs here to include additional networks
const PENDLE_CHAIN_IDS: number[] = [1, 42161, 8453, 56];

const CHAIN_ID_TO_NAME: Record<number, string> = { 1: 'Ethereum', 42161: 'Arbitrum', 8453: 'Base', 56: 'BSC' };

interface PendleMarket {
  address: string;
  chainId: number;
  simpleSymbol: string;
  expiry: string;
  isActive: boolean;
  categoryIds: string[];
  liquidity: { usd: number };
  aggregatedApy: number;
  underlyingApy: number;
  pendleApy: number;
  lpRewardApy: number;
  swapFeeApy: number;
  tradingVolume?: { usd?: number };
}

export interface PendlePoolData {
  symbol: string;
  chain: string;
  project: 'pendle';
  tvlUsd: number;
  apy: number;
  apyBase: number;
  apyReward: number;
  ilRisk: 'no';
  stablecoin: boolean;
  pool: string;
  poolMeta: string | null;
  volumeUsd1d: number | null;
  dataSource: 'pendle';
  [key: string]: unknown;
}

export const normalizePendleMarket = (market: PendleMarket): PendlePoolData => {
  const chain = CHAIN_ID_TO_NAME[market.chainId] ?? String(market.chainId);
  const expiry = market.expiry
    ? new Date(market.expiry).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
    : null;

  return {
    symbol: market.simpleSymbol,
    chain,
    project: 'pendle',
    tvlUsd: market.liquidity?.usd ?? 0,
    apy: market.aggregatedApy * 100,
    apyBase: market.underlyingApy * 100,
    apyReward: (market.pendleApy + market.lpRewardApy + market.swapFeeApy) * 100,
    ilRisk: 'no',
    stablecoin: market.categoryIds?.includes('stables') ?? false,
    pool: market.address,
    poolMeta: expiry ? `Maturity ${expiry}` : null,
    volumeUsd1d: market.tradingVolume?.usd ?? null,
    dataSource: 'pendle',
  };
};

export class PendleMarketsApiService {
  private async fetchMarketsForChain(chainId: number): Promise<PendleMarket[]> {
    const markets: PendleMarket[] = [];
    const limit = 100;
    let skip = 0;
    let total = Infinity;

    while (skip < total) {
      const response = await axios.get<{ total: number; results: PendleMarket[] }>(
        `${PENDLE_API_BASE}/${chainId}/markets`,
        { params: { limit, skip, select: 'all' } },
      );
      const { results, total: pageTotal } = response.data;
      total = pageTotal;
      markets.push(...results);
      skip += results.length;
      if (results.length === 0) break;
    }

    return markets.filter((m) => m.isActive);
  }

  public async fetchMarkets(): Promise<PendlePoolData[]> {
    const results = await Promise.allSettled(PENDLE_CHAIN_IDS.map((id) => this.fetchMarketsForChain(id)));
    return results.flatMap((r) => (r.status === 'fulfilled' ? r.value : [])).map(normalizePendleMarket);
  }
}

export const pendleMarketsApiService = new PendleMarketsApiService();
