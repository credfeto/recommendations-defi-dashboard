import { defiLlamaProtocolsApiService } from './defillama.protocols.api.service';

// Set RUN_E2E=true to run these tests against the live DeFiLlama API
const runE2E = process.env.RUN_E2E === 'true';

(runE2E ? describe : describe.skip)('DefiLlamaProtocolsApiService E2E', () => {
  test('returns a non-empty list of protocols', async () => {
    const result = await defiLlamaProtocolsApiService.fetchProtocols();

    expect(Array.isArray(result)).toBe(true);
    expect(result.length).toBeGreaterThan(0);
  });

  test('each protocol has a slug field', async () => {
    const result = await defiLlamaProtocolsApiService.fetchProtocols();

    result.forEach((protocol) => {
      expect(typeof protocol.slug).toBe('string');
      expect(protocol.slug.length).toBeGreaterThan(0);
    });
  });

  test('well-known protocols have audit data', async () => {
    const result = await defiLlamaProtocolsApiService.fetchProtocols();
    const bySlug = new Map(result.map((p) => [p.slug, p]));

    const aave = bySlug.get('aave-v3');
    expect(aave).toBeDefined();
    expect(Number(aave!.audits)).toBeGreaterThan(0);
    expect(Array.isArray(aave!.audit_links)).toBe(true);
    expect(aave!.audit_links!.length).toBeGreaterThan(0);

    const lido = bySlug.get('lido');
    expect(lido).toBeDefined();
    expect(Number(lido!.audits)).toBeGreaterThan(0);
  });
});
