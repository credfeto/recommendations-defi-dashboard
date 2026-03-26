import { pendleMarketsApiService } from './pendle.markets.api.service';

// Set RUN_E2E=true to run these tests against the live Pendle API
const runE2E = process.env.RUN_E2E === 'true';

(runE2E ? describe : describe.skip)('PendleMarketsApiService E2E', () => {
  test('returns a non-empty list of markets', async () => {
    const result = await pendleMarketsApiService.fetchMarkets();

    expect(Array.isArray(result)).toBe(true);
    expect(result.length).toBeGreaterThan(0);
  });

  test('all markets have project pendle and dataSource pendle', async () => {
    const result = await pendleMarketsApiService.fetchMarkets();

    expect(result.every((m) => m.project === 'pendle')).toBe(true);
    expect(result.every((m) => m.dataSource === 'pendle')).toBe(true);
  });

  test('each market has core fields with correct types', async () => {
    const result = await pendleMarketsApiService.fetchMarkets();

    result.forEach((market) => {
      expect(typeof market.pool).toBe('string');
      expect(typeof market.symbol).toBe('string');
      expect(typeof market.chain).toBe('string');
      expect(typeof market.tvlUsd).toBe('number');
      expect(typeof market.apy).toBe('number');
      expect(typeof market.stablecoin).toBe('boolean');
      expect(market.ilRisk).toBe('no');
    });
  });

  test('APY values are in percentage form (not decimal)', async () => {
    const result = await pendleMarketsApiService.fetchMarkets();

    // A pool with 0.082 decimal APY should come back as ~8.2%, not 0.082
    const highApy = result.filter((m) => m.apy > 1);
    expect(highApy.length).toBeGreaterThan(0);
  });

  test('markets from multiple chains are returned', async () => {
    const result = await pendleMarketsApiService.fetchMarkets();
    const chains = new Set(result.map((m) => m.chain));

    // Should have at least 2 different chains
    expect(chains.size).toBeGreaterThanOrEqual(2);
  });

  test('no market has an exposure field', async () => {
    const result = await pendleMarketsApiService.fetchMarkets();

    result.forEach((market) => {
      expect(market).not.toHaveProperty('exposure');
    });
  });
});
