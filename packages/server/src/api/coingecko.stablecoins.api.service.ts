export interface CoinGeckoStablecoin {
  id: string;
  symbol: string;
  name: string;
  current_price: number;
}

/** Coin list entry including on-chain contract addresses keyed by CoinGecko platform id */
export interface CoinGeckoCoinPlatforms {
  id: string;
  symbol: string;
  platforms: Record<string, string>;
}

const BASE_URL = 'https://api.coingecko.com/api/v3';
const PER_PAGE = 250;

export class CoinGeckoStablecoinsApiService {
  private async fetchPage(page: number): Promise<CoinGeckoStablecoin[]> {
    const url = `${BASE_URL}/coins/markets?vs_currency=usd&category=stablecoins&order=market_cap_desc&per_page=${PER_PAGE}&page=${page}`;
    const res = await fetch(url);
    if (!res.ok) throw new Error(`CoinGecko stablecoins request failed: ${res.status} ${res.statusText}`);
    return res.json() as Promise<CoinGeckoStablecoin[]>;
  }

  public async fetchStablecoins(): Promise<CoinGeckoStablecoin[]> {
    const all: CoinGeckoStablecoin[] = [];
    let page = 1;

    while (true) {
      const results = await this.fetchPage(page);
      all.push(...results);
      if (results.length < PER_PAGE) break;
      page++;
      // Brief pause between pages to respect rate limits
      await new Promise((r) => setTimeout(r, 500));
    }

    return all;
  }

  /**
   * Fetches the full CoinGecko coin list with on-chain contract addresses.
   * Used to build an address → price map for underlyingTokens depeg checking.
   */
  public async fetchCoinList(): Promise<CoinGeckoCoinPlatforms[]> {
    const url = `${BASE_URL}/coins/list?include_platform=true`;
    const res = await fetch(url);
    if (!res.ok) throw new Error(`CoinGecko coin list request failed: ${res.status} ${res.statusText}`);
    return res.json() as Promise<CoinGeckoCoinPlatforms[]>;
  }
}

export const coinGeckoStablecoinsApiService = new CoinGeckoStablecoinsApiService();
