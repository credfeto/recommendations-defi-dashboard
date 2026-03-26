import { buildStablecoinPriceMap, buildStablecoinAddressMap, parsePoolSymbols, checkDepeg } from './depeg.service';
import { CoinGeckoStablecoin, CoinGeckoCoinPlatforms } from '../api/coingecko.stablecoins.api.service';

const stablecoins: CoinGeckoStablecoin[] = [
  { id: 'usd-coin', symbol: 'usdc', name: 'USD Coin', current_price: 1.0 },
  { id: 'tether', symbol: 'usdt', name: 'Tether', current_price: 0.999 },
  { id: 'resolv-usr', symbol: 'usr', name: 'Resolv USR', current_price: 0.33 },
  { id: 'dai', symbol: 'dai', name: 'Dai', current_price: 1.005 },
];

const coinList: CoinGeckoCoinPlatforms[] = [
  { id: 'usd-coin', symbol: 'usdc', platforms: { ethereum: '0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48' } },
  { id: 'resolv-usr', symbol: 'usr', platforms: { ethereum: '0xeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee' } },
  { id: 'some-non-stablecoin', symbol: 'weth', platforms: { ethereum: '0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2' } },
];

describe('buildStablecoinPriceMap', () => {
  test('maps lowercase symbol to price', () => {
    const map = buildStablecoinPriceMap(stablecoins);
    expect(map.get('usdc')).toBe(1.0);
    expect(map.get('usdt')).toBe(0.999);
  });

  test('skips coins with null price', () => {
    const coins: CoinGeckoStablecoin[] = [{ id: 'x', symbol: 'X', name: 'X', current_price: null }];
    const map = buildStablecoinPriceMap(coins);
    expect(map.size).toBe(0);
  });

  test('returns empty map for empty input', () => {
    expect(buildStablecoinPriceMap([]).size).toBe(0);
  });
});

describe('buildStablecoinAddressMap', () => {
  test('maps 0x address to symbol for known stablecoins', () => {
    const map = buildStablecoinAddressMap(stablecoins, coinList);
    expect(map.get('0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48')).toBe('usdc');
    expect(map.get('0xeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee')).toBe('usr');
  });

  test('does not include addresses for non-stablecoin coins', () => {
    const map = buildStablecoinAddressMap(stablecoins, coinList);
    expect(map.has('0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2')).toBe(false);
  });

  test('normalises addresses to lowercase', () => {
    const list: CoinGeckoCoinPlatforms[] = [
      { id: 'usd-coin', symbol: 'usdc', platforms: { ethereum: '0xA0B86991C6218B36C1D19D4A2E9EB0CE3606EB48' } },
    ];
    const map = buildStablecoinAddressMap(stablecoins, list);
    expect(map.has('0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48')).toBe(true);
  });

  test('ignores non-0x platform entries (e.g. Solana, Polkadot)', () => {
    const list: CoinGeckoCoinPlatforms[] = [{ id: 'usd-coin', symbol: 'usdc', platforms: { solana: 'EPjFWdd5Au...' } }];
    const map = buildStablecoinAddressMap(stablecoins, list);
    expect(map.size).toBe(0);
  });

  test('returns empty map when inputs are empty', () => {
    expect(buildStablecoinAddressMap([], []).size).toBe(0);
  });
});

describe('parsePoolSymbols', () => {
  test('splits on hyphen', () => {
    expect(parsePoolSymbols('USR-USDC')).toEqual(['USR', 'USDC']);
  });

  test('splits on slash', () => {
    expect(parsePoolSymbols('USDC/USDT')).toEqual(['USDC', 'USDT']);
  });

  test('splits on plus', () => {
    expect(parsePoolSymbols('USDC+DAI')).toEqual(['USDC', 'DAI']);
  });

  test('handles single token', () => {
    expect(parsePoolSymbols('USDC')).toEqual(['USDC']);
  });

  test('trims whitespace tokens', () => {
    expect(parsePoolSymbols('USDC - USDT')).toEqual(['USDC', 'USDT']);
  });
});

describe('checkDepeg', () => {
  const priceMap = buildStablecoinPriceMap(stablecoins);

  test('returns empty array when no tokens are in the price map', () => {
    expect(checkDepeg('WETH-WBTC', priceMap)).toEqual([]);
  });

  test('returns empty array when all tokens are within threshold', () => {
    // USDT at 0.999 = 0.1% deviation, below 0.5% warning threshold
    expect(checkDepeg('USDT', priceMap)).toEqual([]);
  });

  test('returns warning alert for token between 0.5% and 2% off peg', () => {
    // 0.993 = -0.7% below peg — clearly in the warning band
    const coins: CoinGeckoStablecoin[] = [{ id: 'x', symbol: 'tkn', name: 'Token', current_price: 0.993 }];
    const map = buildStablecoinPriceMap(coins);
    const alerts = checkDepeg('TKN', map);
    expect(alerts).toHaveLength(1);
    expect(alerts[0].severity).toBe('warning');
    expect(alerts[0].symbol).toBe('TKN');
    expect(alerts[0].currentPrice).toBe(0.993);
  });

  test('returns critical alert for token more than 2% off peg', () => {
    // USR at 0.33 = -67% deviation
    const alerts = checkDepeg('USR', priceMap);
    expect(alerts).toHaveLength(1);
    expect(alerts[0].severity).toBe('critical');
    expect(alerts[0].deviation).toBeCloseTo(-0.67, 1);
  });

  test('checks multiple tokens in a pool symbol', () => {
    const alerts = checkDepeg('USR-USDC', priceMap);
    expect(alerts).toHaveLength(1);
    expect(alerts[0].symbol).toBe('USR');
  });

  test('deduplicates repeated tokens', () => {
    expect(checkDepeg('USR-USR', priceMap)).toHaveLength(1);
  });

  test('checks underlyingTokens addresses via addressMap', () => {
    const addressMap = buildStablecoinAddressMap(stablecoins, coinList);
    const alerts = checkDepeg(
      'SOME-LP',
      priceMap,
      ['0xeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee'], // USR address
      addressMap,
    );
    expect(alerts).toHaveLength(1);
    expect(alerts[0].severity).toBe('critical');
  });

  test('does not duplicate alert when symbol and address match same token', () => {
    const addressMap = buildStablecoinAddressMap(stablecoins, coinList);
    // Pool symbol "USR" already catches it; address should not produce a second alert
    const alerts = checkDepeg('USR', priceMap, ['0xeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee'], addressMap);
    expect(alerts).toHaveLength(1);
  });

  test('skips unknown addresses', () => {
    const addressMap = buildStablecoinAddressMap(stablecoins, coinList);
    expect(checkDepeg('SOME-LP', priceMap, ['0xdeadbeef'], addressMap)).toEqual([]);
  });
});
