export interface CoinGeckoStablecoin {
  id: string;
  symbol: string;
  name: string;
  current_price: number;
}

const BASE_URL = 'https://api.coingecko.com/api/v3';
const PER_PAGE = 250;

async function fetchPage(page: number): Promise<CoinGeckoStablecoin[]> {
  const url = `${BASE_URL}/coins/markets?vs_currency=usd&category=stablecoins&order=market_cap_desc&per_page=${PER_PAGE}&page=${page}`;
  const res = await fetch(url);
  if (!res.ok) throw new Error(`CoinGecko stablecoins request failed: ${res.status} ${res.statusText}`);
  return res.json() as Promise<CoinGeckoStablecoin[]>;
}

export async function fetchCoinGeckoStablecoins(): Promise<CoinGeckoStablecoin[]> {
  const all: CoinGeckoStablecoin[] = [];
  let page = 1;

  while (true) {
    const results = await fetchPage(page);
    all.push(...results);
    if (results.length < PER_PAGE) break;
    page++;
    // Brief pause between pages to respect rate limits
    await new Promise((r) => setTimeout(r, 500));
  }

  return all;
}
