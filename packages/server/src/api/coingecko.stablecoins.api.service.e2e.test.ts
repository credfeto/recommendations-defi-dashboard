import { coinGeckoStablecoinsApiService } from './coingecko.stablecoins.api.service';

// Set RUN_E2E=true to run these tests against the live CoinGecko API
const runE2E = process.env.RUN_E2E === 'true';

(runE2E ? describe : describe.skip)('CoinGeckoStablecoinsApiService E2E', () => {
  describe('fetchStablecoins', () => {
    test('returns a non-empty list of stablecoins', async () => {
      const result = await coinGeckoStablecoinsApiService.fetchStablecoins();

      expect(Array.isArray(result)).toBe(true);
      expect(result.length).toBeGreaterThan(0);
    });

    test('each coin has id, symbol, name, and current_price', async () => {
      const result = await coinGeckoStablecoinsApiService.fetchStablecoins();

      result.forEach((coin) => {
        expect(typeof coin.id).toBe('string');
        expect(typeof coin.symbol).toBe('string');
        expect(typeof coin.name).toBe('string');
        expect(typeof coin.current_price).toBe('number');
      });
    });

    test('contains well-known stablecoins', async () => {
      const result = await coinGeckoStablecoinsApiService.fetchStablecoins();
      const symbols = result.map((c) => c.symbol.toLowerCase());

      expect(symbols).toContain('usdt');
      expect(symbols).toContain('usdc');
    });
  });

  describe('fetchCoinList', () => {
    test('returns a large list of coins with platform data', async () => {
      const result = await coinGeckoStablecoinsApiService.fetchCoinList();

      expect(Array.isArray(result)).toBe(true);
      expect(result.length).toBeGreaterThan(1000);
    });

    test('each entry has id, symbol, and platforms object', async () => {
      const result = await coinGeckoStablecoinsApiService.fetchCoinList();
      const sample = result.slice(0, 20);

      sample.forEach((coin) => {
        expect(typeof coin.id).toBe('string');
        expect(typeof coin.symbol).toBe('string');
        expect(typeof coin.platforms).toBe('object');
      });
    });

    test('tether has an ethereum contract address', async () => {
      const result = await coinGeckoStablecoinsApiService.fetchCoinList();
      const tether = result.find((c) => c.id === 'tether');

      expect(tether).toBeDefined();
      expect(tether?.platforms['ethereum']).toBeDefined();
      expect(tether?.platforms['ethereum']).toMatch(/^0x/i);
    });
  });
});
