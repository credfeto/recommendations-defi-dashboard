import { goPlusApiService } from './goplus.api.service';

// Set RUN_E2E=true to run these tests against the live GoPlus API
const runE2E = process.env.RUN_E2E === 'true';

(runE2E ? describe : describe.skip)('GoPlusApiService E2E', () => {
  test('returns security data for a known Ethereum token', async () => {
    // stETH
    const result = await goPlusApiService.fetchTokenSecurity('Ethereum', [
      '0xae7ab96520de3a18e5e111b5eaab095312d7fe84',
    ]);

    expect(result.size).toBe(1);
    const info = result.get('0xae7ab96520de3a18e5e111b5eaab095312d7fe84');
    expect(info).toBeDefined();
    expect(info!.is_open_source).toBe('1');
    expect(info!.is_honeypot).toBe('0');
  });

  test('returns empty map for unsupported chain', async () => {
    const result = await goPlusApiService.fetchTokenSecurity('Solana', ['0xabc']);
    expect(result.size).toBe(0);
  });

  test('handles batch of addresses', async () => {
    const addresses = [
      '0xae7ab96520de3a18e5e111b5eaab095312d7fe84', // stETH
      '0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2', // WETH
    ];
    const result = await goPlusApiService.fetchTokenSecurity('Ethereum', addresses);
    expect(result.size).toBe(2);
  });
});
