import { getCached, setCached, isFresh } from '../db/cache.db';
import { defiLlamaPoolsApiService } from '../api/defillama.pools.api.service';
import { defiLlamaHacksApiService } from '../api/defillama.hacks.api.service';
import { pendleMarketsApiService } from '../api/pendle.markets.api.service';
import { coinGeckoStablecoinsApiService } from '../api/coingecko.stablecoins.api.service';

export const CACHE_KEYS = {
  LLAMA_POOLS: 'defillama_pools',
  PENDLE_POOLS: 'pendle_pools',
  HACKS: 'defillama_hacks',
  STABLECOINS: 'coingecko_stablecoins',
  COIN_LIST: 'coingecko_coin_list',
} as const;

type WarmLogger = { info: (msg: string) => void; error: (obj: unknown, msg: string) => void };

export class CacheWarmerService {
  private readonly fetchers: ReadonlyArray<{ key: string; fetcher: () => Promise<unknown> }> = [
    { key: CACHE_KEYS.LLAMA_POOLS, fetcher: () => defiLlamaPoolsApiService.fetchPools() },
    { key: CACHE_KEYS.PENDLE_POOLS, fetcher: () => pendleMarketsApiService.fetchMarkets() },
    { key: CACHE_KEYS.HACKS, fetcher: () => defiLlamaHacksApiService.fetchHacks() },
    { key: CACHE_KEYS.STABLECOINS, fetcher: () => coinGeckoStablecoinsApiService.fetchStablecoins() },
    { key: CACHE_KEYS.COIN_LIST, fetcher: () => coinGeckoStablecoinsApiService.fetchCoinList() },
  ];

  /**
   * Fires background fetches for any cache entries that are missing or stale.
   * Each entry is fetched independently; errors are logged and skipped so a
   * failure in one API does not block the others.
   */
  public warmCache(logger: WarmLogger): void {
    void Promise.all(
      this.fetchers.map(async ({ key, fetcher }) => {
        const cached = getCached(key);
        if (cached !== null && isFresh(cached.age)) return;
        try {
          const data = await fetcher();
          setCached(key, data);
          logger.info(`Cache warmed: ${key}`);
        } catch (err) {
          logger.error({ err }, `Failed to warm cache for ${key}`);
        }
      }),
    );
  }
}

export const cacheWarmerService = new CacheWarmerService();
