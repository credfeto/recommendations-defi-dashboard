import {
  CoinGeckoStablecoinsApiService,
  CoinGeckoStablecoin,
  CoinGeckoCoinPlatforms,
} from './coingecko.stablecoins.api.service';

const mockStablecoins: CoinGeckoStablecoin[] = [
  { id: 'tether', symbol: 'usdt', name: 'Tether', current_price: 1.0 },
  { id: 'usd-coin', symbol: 'usdc', name: 'USD Coin', current_price: 1.001 },
];

const mockCoinList: CoinGeckoCoinPlatforms[] = [
  { id: 'tether', symbol: 'usdt', platforms: { ethereum: '0xdac17f958d2ee523a2206206994597c13d831ec7' } },
];

describe('CoinGeckoStablecoinsApiService', () => {
  let service: CoinGeckoStablecoinsApiService;

  beforeEach(() => {
    service = new CoinGeckoStablecoinsApiService();
    jest.spyOn(global, 'fetch');
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  describe('fetchStablecoins', () => {
    test('returns stablecoins from a single page', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockStablecoins) });

      const result = await service.fetchStablecoins();

      expect(result).toEqual(mockStablecoins);
      expect(global.fetch).toHaveBeenCalledTimes(1);
      expect((global.fetch as jest.Mock).mock.calls[0][0]).toContain('category=stablecoins');
    });

    test('paginates when a full page is returned', async () => {
      jest.useFakeTimers();

      const fullPage = Array.from({ length: 250 }, (_, i) => ({
        id: `coin-${i}`,
        symbol: `coin${i}`,
        name: `Coin ${i}`,
        current_price: 1.0,
      }));

      (global.fetch as jest.Mock)
        .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(fullPage) })
        .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockStablecoins) });

      const resultPromise = service.fetchStablecoins();
      await jest.runAllTimersAsync();
      const result = await resultPromise;

      jest.useRealTimers();

      expect(result).toHaveLength(252);
      expect(global.fetch).toHaveBeenCalledTimes(2);
    });

    test('throws when response is not ok', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({ ok: false, status: 429, statusText: 'Too Many Requests' });

      await expect(service.fetchStablecoins()).rejects.toThrow('CoinGecko stablecoins request failed: 429');
    });

    test('each coin has required fields', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockStablecoins) });

      const result = await service.fetchStablecoins();

      result.forEach((coin) => {
        expect(coin).toHaveProperty('id');
        expect(coin).toHaveProperty('symbol');
        expect(coin).toHaveProperty('name');
        expect(coin).toHaveProperty('current_price');
        expect(typeof coin.current_price).toBe('number');
      });
    });
  });

  describe('fetchCoinList', () => {
    test('returns coin list with platforms', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockCoinList) });

      const result = await service.fetchCoinList();

      expect(result).toEqual(mockCoinList);
      expect(global.fetch).toHaveBeenCalledTimes(1);
      expect((global.fetch as jest.Mock).mock.calls[0][0]).toContain('include_platform=true');
    });

    test('throws when response is not ok', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({ ok: false, status: 503, statusText: 'Service Unavailable' });

      await expect(service.fetchCoinList()).rejects.toThrow('CoinGecko coin list request failed: 503');
    });

    test('each entry has id, symbol, and platforms', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockCoinList) });

      const result = await service.fetchCoinList();

      result.forEach((coin) => {
        expect(coin).toHaveProperty('id');
        expect(coin).toHaveProperty('symbol');
        expect(coin).toHaveProperty('platforms');
        expect(typeof coin.platforms).toBe('object');
      });
    });
  });
});
