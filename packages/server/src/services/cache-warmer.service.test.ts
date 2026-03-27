import { cacheWarmerService, CACHE_KEYS } from './cache-warmer.service';

jest.mock('../db/cache.db', () => ({ getCached: jest.fn(), setCached: jest.fn(), isFresh: jest.fn() }));

jest.mock('../api/defillama.pools.api.service', () => ({ defiLlamaPoolsApiService: { fetchPools: jest.fn() } }));
jest.mock('../api/defillama.hacks.api.service', () => ({ defiLlamaHacksApiService: { fetchHacks: jest.fn() } }));
jest.mock('../api/pendle.markets.api.service', () => ({ pendleMarketsApiService: { fetchMarkets: jest.fn() } }));
jest.mock('../api/coingecko.stablecoins.api.service', () => ({
  coinGeckoStablecoinsApiService: { fetchStablecoins: jest.fn(), fetchCoinList: jest.fn() },
}));

import { getCached, setCached, isFresh } from '../db/cache.db';
import { defiLlamaPoolsApiService } from '../api/defillama.pools.api.service';
import { defiLlamaHacksApiService } from '../api/defillama.hacks.api.service';
import { pendleMarketsApiService } from '../api/pendle.markets.api.service';
import { coinGeckoStablecoinsApiService } from '../api/coingecko.stablecoins.api.service';

const mockGetCached = getCached as jest.Mock;
const mockSetCached = setCached as jest.Mock;
const mockIsFresh = isFresh as jest.Mock;

const makeLogger = () => ({ info: jest.fn(), error: jest.fn() });

/** Flush all pending microtasks so warmCache's fire-and-forget Promise resolves */
const flushPromises = () => new Promise((resolve) => setImmediate(resolve));

beforeEach(() => {
  jest.clearAllMocks();
});

describe('CacheWarmerService', () => {
  describe('when cache is fresh', () => {
    test('does not call fetcher', async () => {
      mockGetCached.mockReturnValue({ data: [], age: 100 });
      mockIsFresh.mockReturnValue(true);

      cacheWarmerService.warmCache(makeLogger());
      await flushPromises();

      expect(defiLlamaPoolsApiService.fetchPools).not.toHaveBeenCalled();
      expect(pendleMarketsApiService.fetchMarkets).not.toHaveBeenCalled();
      expect(defiLlamaHacksApiService.fetchHacks).not.toHaveBeenCalled();
      expect(coinGeckoStablecoinsApiService.fetchStablecoins).not.toHaveBeenCalled();
      expect(coinGeckoStablecoinsApiService.fetchCoinList).not.toHaveBeenCalled();
    });

    test('does not write to cache', async () => {
      mockGetCached.mockReturnValue({ data: [], age: 100 });
      mockIsFresh.mockReturnValue(true);

      cacheWarmerService.warmCache(makeLogger());
      await flushPromises();

      expect(mockSetCached).not.toHaveBeenCalled();
    });

    test('does not log anything', async () => {
      mockGetCached.mockReturnValue({ data: [], age: 100 });
      mockIsFresh.mockReturnValue(true);
      const logger = makeLogger();

      cacheWarmerService.warmCache(logger);
      await flushPromises();

      expect(logger.info).not.toHaveBeenCalled();
      expect(logger.error).not.toHaveBeenCalled();
    });
  });

  describe('when cache entry is missing', () => {
    test('calls every fetcher', async () => {
      mockGetCached.mockReturnValue(null);
      (defiLlamaPoolsApiService.fetchPools as jest.Mock).mockResolvedValue([]);
      (pendleMarketsApiService.fetchMarkets as jest.Mock).mockResolvedValue([]);
      (defiLlamaHacksApiService.fetchHacks as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchStablecoins as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchCoinList as jest.Mock).mockResolvedValue([]);

      cacheWarmerService.warmCache(makeLogger());
      await flushPromises();

      expect(defiLlamaPoolsApiService.fetchPools).toHaveBeenCalledTimes(1);
      expect(pendleMarketsApiService.fetchMarkets).toHaveBeenCalledTimes(1);
      expect(defiLlamaHacksApiService.fetchHacks).toHaveBeenCalledTimes(1);
      expect(coinGeckoStablecoinsApiService.fetchStablecoins).toHaveBeenCalledTimes(1);
      expect(coinGeckoStablecoinsApiService.fetchCoinList).toHaveBeenCalledTimes(1);
    });

    test('writes fetched data to cache', async () => {
      mockGetCached.mockReturnValue(null);
      const poolsData = [{ pool: 'test' }];
      (defiLlamaPoolsApiService.fetchPools as jest.Mock).mockResolvedValue(poolsData);
      (pendleMarketsApiService.fetchMarkets as jest.Mock).mockResolvedValue([]);
      (defiLlamaHacksApiService.fetchHacks as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchStablecoins as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchCoinList as jest.Mock).mockResolvedValue([]);

      cacheWarmerService.warmCache(makeLogger());
      await flushPromises();

      expect(mockSetCached).toHaveBeenCalledWith(CACHE_KEYS.LLAMA_POOLS, poolsData);
    });

    test('logs info for each successfully warmed key', async () => {
      mockGetCached.mockReturnValue(null);
      (defiLlamaPoolsApiService.fetchPools as jest.Mock).mockResolvedValue([]);
      (pendleMarketsApiService.fetchMarkets as jest.Mock).mockResolvedValue([]);
      (defiLlamaHacksApiService.fetchHacks as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchStablecoins as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchCoinList as jest.Mock).mockResolvedValue([]);
      const logger = makeLogger();

      cacheWarmerService.warmCache(logger);
      await flushPromises();

      expect(logger.info).toHaveBeenCalledTimes(5);
      expect(logger.info).toHaveBeenCalledWith(expect.stringContaining(CACHE_KEYS.LLAMA_POOLS));
      expect(logger.info).toHaveBeenCalledWith(expect.stringContaining(CACHE_KEYS.PENDLE_POOLS));
      expect(logger.info).toHaveBeenCalledWith(expect.stringContaining(CACHE_KEYS.HACKS));
      expect(logger.info).toHaveBeenCalledWith(expect.stringContaining(CACHE_KEYS.STABLECOINS));
      expect(logger.info).toHaveBeenCalledWith(expect.stringContaining(CACHE_KEYS.COIN_LIST));
    });
  });

  describe('when cache entry is stale', () => {
    test('calls the fetcher', async () => {
      mockGetCached.mockReturnValue({ data: [], age: 9_999_999 });
      mockIsFresh.mockReturnValue(false);
      (defiLlamaPoolsApiService.fetchPools as jest.Mock).mockResolvedValue([]);
      (pendleMarketsApiService.fetchMarkets as jest.Mock).mockResolvedValue([]);
      (defiLlamaHacksApiService.fetchHacks as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchStablecoins as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchCoinList as jest.Mock).mockResolvedValue([]);

      cacheWarmerService.warmCache(makeLogger());
      await flushPromises();

      expect(defiLlamaPoolsApiService.fetchPools).toHaveBeenCalledTimes(1);
    });
  });

  describe('when a fetcher throws', () => {
    test('logs an error for the failing key', async () => {
      mockGetCached.mockReturnValue(null);
      const fetchError = new Error('network failure');
      (defiLlamaPoolsApiService.fetchPools as jest.Mock).mockRejectedValue(fetchError);
      (pendleMarketsApiService.fetchMarkets as jest.Mock).mockResolvedValue([]);
      (defiLlamaHacksApiService.fetchHacks as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchStablecoins as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchCoinList as jest.Mock).mockResolvedValue([]);
      const logger = makeLogger();

      cacheWarmerService.warmCache(logger);
      await flushPromises();

      expect(logger.error).toHaveBeenCalledTimes(1);
      expect(logger.error).toHaveBeenCalledWith(
        expect.objectContaining({ err: fetchError }),
        expect.stringContaining(CACHE_KEYS.LLAMA_POOLS),
      );
    });

    test('does not write to cache for the failing key', async () => {
      mockGetCached.mockReturnValue(null);
      (defiLlamaPoolsApiService.fetchPools as jest.Mock).mockRejectedValue(new Error('fail'));
      (pendleMarketsApiService.fetchMarkets as jest.Mock).mockResolvedValue([]);
      (defiLlamaHacksApiService.fetchHacks as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchStablecoins as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchCoinList as jest.Mock).mockResolvedValue([]);

      cacheWarmerService.warmCache(makeLogger());
      await flushPromises();

      expect(mockSetCached).not.toHaveBeenCalledWith(CACHE_KEYS.LLAMA_POOLS, expect.anything());
    });

    test('still fetches and caches the remaining keys', async () => {
      mockGetCached.mockReturnValue(null);
      (defiLlamaPoolsApiService.fetchPools as jest.Mock).mockRejectedValue(new Error('fail'));
      (pendleMarketsApiService.fetchMarkets as jest.Mock).mockResolvedValue([]);
      (defiLlamaHacksApiService.fetchHacks as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchStablecoins as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchCoinList as jest.Mock).mockResolvedValue([]);

      cacheWarmerService.warmCache(makeLogger());
      await flushPromises();

      expect(mockSetCached).toHaveBeenCalledWith(CACHE_KEYS.PENDLE_POOLS, expect.anything());
      expect(mockSetCached).toHaveBeenCalledWith(CACHE_KEYS.HACKS, expect.anything());
      expect(mockSetCached).toHaveBeenCalledWith(CACHE_KEYS.STABLECOINS, expect.anything());
      expect(mockSetCached).toHaveBeenCalledWith(CACHE_KEYS.COIN_LIST, expect.anything());
    });

    test('continues logging info for successful keys despite one failure', async () => {
      mockGetCached.mockReturnValue(null);
      (defiLlamaPoolsApiService.fetchPools as jest.Mock).mockRejectedValue(new Error('fail'));
      (pendleMarketsApiService.fetchMarkets as jest.Mock).mockResolvedValue([]);
      (defiLlamaHacksApiService.fetchHacks as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchStablecoins as jest.Mock).mockResolvedValue([]);
      (coinGeckoStablecoinsApiService.fetchCoinList as jest.Mock).mockResolvedValue([]);
      const logger = makeLogger();

      cacheWarmerService.warmCache(logger);
      await flushPromises();

      expect(logger.info).toHaveBeenCalledTimes(4);
      expect(logger.error).toHaveBeenCalledTimes(1);
    });
  });

  describe('when all fetchers throw', () => {
    test('logs an error for every key and writes nothing to cache', async () => {
      mockGetCached.mockReturnValue(null);
      const err = new Error('all down');
      (defiLlamaPoolsApiService.fetchPools as jest.Mock).mockRejectedValue(err);
      (pendleMarketsApiService.fetchMarkets as jest.Mock).mockRejectedValue(err);
      (defiLlamaHacksApiService.fetchHacks as jest.Mock).mockRejectedValue(err);
      (coinGeckoStablecoinsApiService.fetchStablecoins as jest.Mock).mockRejectedValue(err);
      (coinGeckoStablecoinsApiService.fetchCoinList as jest.Mock).mockRejectedValue(err);
      const logger = makeLogger();

      cacheWarmerService.warmCache(logger);
      await flushPromises();

      expect(logger.error).toHaveBeenCalledTimes(5);
      expect(mockSetCached).not.toHaveBeenCalled();
    });
  });
});
