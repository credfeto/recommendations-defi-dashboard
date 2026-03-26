import { defiLlamaPoolsApiService } from './defillama.pools.api.service';

// Set RUN_E2E=true to run these tests against the live DeFiLlama API
const runE2E = process.env.RUN_E2E === 'true';

(runE2E ? describe : describe.skip)('DefiLlamaPoolsApiService E2E', () => {
  test('returns a large list of pools', async () => {
    const result = await defiLlamaPoolsApiService.fetchPools();

    expect(Array.isArray(result)).toBe(true);
    expect(result.length).toBeGreaterThan(100);
  });

  test('all pools have dataSource set to defillama', async () => {
    const result = await defiLlamaPoolsApiService.fetchPools();

    expect(result.every((p) => p.dataSource === 'defillama')).toBe(true);
  });

  test('no pendle pools are included', async () => {
    const result = await defiLlamaPoolsApiService.fetchPools();

    expect(result.some((p) => p.project === 'pendle')).toBe(false);
  });

  test('each pool has core fields', async () => {
    const result = await defiLlamaPoolsApiService.fetchPools();
    const sample = result.slice(0, 10);

    sample.forEach((pool) => {
      expect(typeof pool.pool).toBe('string');
      expect(typeof pool.project).toBe('string');
      expect(typeof pool.symbol).toBe('string');
      expect(typeof pool.chain).toBe('string');
      expect(typeof pool.tvlUsd).toBe('number');
      expect(typeof pool.apy).toBe('number');
    });
  });
});
