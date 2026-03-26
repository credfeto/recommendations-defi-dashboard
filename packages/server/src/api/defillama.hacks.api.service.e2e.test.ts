import { defiLlamaHacksApiService } from './defillama.hacks.api.service';

// Set RUN_E2E=true to run these tests against the live DeFiLlama API
const runE2E = process.env.RUN_E2E === 'true';

(runE2E ? describe : describe.skip)('DefiLlamaHacksApiService E2E', () => {
  test('returns a non-empty list of hacks', async () => {
    const result = await defiLlamaHacksApiService.fetchHacks();

    expect(Array.isArray(result)).toBe(true);
    expect(result.length).toBeGreaterThan(0);
  });

  test('each hack has required fields with correct types', async () => {
    const result = await defiLlamaHacksApiService.fetchHacks();

    result.forEach((hack) => {
      expect(typeof hack.date).toBe('number');
      expect(typeof hack.name).toBe('string');
      expect(typeof hack.amount).toBe('number');
      expect(typeof hack.source).toBe('string');
    });
  });

  test('contains significant historical exploits', async () => {
    const result = await defiLlamaHacksApiService.fetchHacks();
    const names = result.map((h) => h.name.toLowerCase());

    // These are well-documented exploits that should always be present
    expect(names.some((n) => n.includes('ronin') || n.includes('poly') || n.includes('wormhole'))).toBe(true);
  });

  test('hack amounts are positive numbers', async () => {
    const result = await defiLlamaHacksApiService.fetchHacks();

    result.forEach((hack) => {
      expect(hack.amount).toBeGreaterThan(0);
    });
  });
});
