import { getPoolUrl } from './pool-url.service';

describe('getPoolUrl', () => {
  describe('defillama pools', () => {
    test('returns defillama yields URL', () => {
      expect(
        getPoolUrl({ dataSource: 'defillama', pool: 'abc-123', chain: 'Ethereum' }),
      ).toBe('https://defillama.com/yields?pool=abc-123');
    });
  });

  describe('pendle pools', () => {
    test('returns Pendle URL for Ethereum (chainId 1)', () => {
      expect(
        getPoolUrl({ dataSource: 'pendle', pool: '0xabc', chain: 'ethereum' }),
      ).toBe('https://app.pendle.finance/trade/markets/1/0xabc/pt');
    });

    test('returns Pendle URL for Arbitrum (chainId 42161)', () => {
      expect(
        getPoolUrl({ dataSource: 'pendle', pool: '0xdef', chain: 'arbitrum' }),
      ).toBe('https://app.pendle.finance/trade/markets/42161/0xdef/pt');
    });

    test('returns Pendle URL for Base (chainId 8453)', () => {
      expect(
        getPoolUrl({ dataSource: 'pendle', pool: '0x111', chain: 'base' }),
      ).toBe('https://app.pendle.finance/trade/markets/8453/0x111/pt');
    });

    test('returns Pendle URL for BSC (chainId 56)', () => {
      expect(
        getPoolUrl({ dataSource: 'pendle', pool: '0x222', chain: 'bsc' }),
      ).toBe('https://app.pendle.finance/trade/markets/56/0x222/pt');
    });

    test('returns null for unknown Pendle chain', () => {
      expect(
        getPoolUrl({ dataSource: 'pendle', pool: '0xabc', chain: 'avalanche' }),
      ).toBeNull();
    });

    test('is case-insensitive for chain name', () => {
      expect(
        getPoolUrl({ dataSource: 'pendle', pool: '0xabc', chain: 'Ethereum' }),
      ).toBe('https://app.pendle.finance/trade/markets/1/0xabc/pt');
    });
  });

  describe('unknown source', () => {
    test('returns null for unknown dataSource', () => {
      expect(
        getPoolUrl({ dataSource: 'unknown', pool: 'x', chain: 'Ethereum' }),
      ).toBeNull();
    });
  });
});
