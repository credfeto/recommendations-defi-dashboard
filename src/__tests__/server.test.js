const { filterPools, getPoolsByType } = require('../server');

const mockPoolData = [
  {
    chain: 'Ethereum',
    project: 'lido',
    symbol: 'STETH',
    tvlUsd: 20000000000,
    apy: 2.5,
    ilRisk: 'no',
    stablecoin: false,
    pool: '1',
  },
  {
    chain: 'Ethereum',
    project: 'circle',
    symbol: 'USDC',
    tvlUsd: 10000000000,
    apy: 3.5,
    ilRisk: 'no',
    stablecoin: true,
    pool: '2',
  },
  {
    chain: 'Ethereum',
    project: 'tether',
    symbol: 'USDT',
    tvlUsd: 5000000,
    apy: 2.0,
    ilRisk: 'no',
    stablecoin: true,
    pool: '3',
  },
  {
    chain: 'Ethereum',
    project: 'weth',
    symbol: 'WETH',
    tvlUsd: 500000,
    apy: 1.5,
    ilRisk: 'yes',
    stablecoin: false,
    pool: '4',
  },
  {
    chain: 'Ethereum',
    project: 'wrapped-eth',
    symbol: 'WEETH',
    tvlUsd: 15000000000,
    apy: 2.8,
    ilRisk: 'no',
    stablecoin: false,
    pool: '5',
  },
  {
    chain: 'Ethereum',
    project: 'aave',
    symbol: 'USDC-ETH',
    tvlUsd: 2000000000,
    apy: 0,
    ilRisk: 'no',
    stablecoin: true,
    pool: '6',
  },
];

describe('Server API Tests', () => {
  describe('Pool Filtering', () => {
    test('filters pools with no IL risk', () => {
      const filtered = filterPools(mockPoolData);
      expect(filtered.every(p => p.ilRisk === 'no')).toBe(true);
    });

    test('filters pools with sufficient TVL', () => {
      const filtered = filterPools(mockPoolData);
      expect(filtered.every(p => p.tvlUsd >= 1000000)).toBe(true);
    });

    test('filters pools with positive APY', () => {
      const filtered = filterPools(mockPoolData);
      expect(filtered.every(p => p.apy > 0)).toBe(true);
    });

    test('combines all filters correctly', () => {
      const filtered = filterPools(mockPoolData);
      expect(filtered.length).toBe(4);
      expect(filtered.map(p => p.symbol)).toEqual(['STETH', 'USDC', 'USDT', 'WEETH']);
    });
  });

  describe('Pool Type Categorization', () => {
    test('identifies ETH-based pools correctly', () => {
      const ethPools = getPoolsByType(mockPoolData, 'ETH');
      expect(ethPools.length).toBe(2);
      expect(ethPools.map(p => p.symbol)).toEqual(['STETH', 'WEETH']);
    });

    test('identifies stablecoin pools correctly', () => {
      const stablePools = getPoolsByType(mockPoolData, 'STABLES');
      expect(stablePools.length).toBe(2);
      expect(stablePools.map(p => p.symbol)).toEqual(['USDC', 'USDT']);
    });

    test('filters ETH pools with all criteria', () => {
      const ethPools = getPoolsByType(mockPoolData, 'ETH');
      expect(ethPools.every(p => p.symbol.toUpperCase().includes('ETH'))).toBe(true);
      expect(ethPools.every(p => p.ilRisk === 'no')).toBe(true);
      expect(ethPools.every(p => p.tvlUsd >= 1000000)).toBe(true);
      expect(ethPools.every(p => p.apy > 0)).toBe(true);
    });

    test('filters stablecoin pools with all criteria', () => {
      const stablePools = getPoolsByType(mockPoolData, 'STABLES');
      expect(stablePools.every(p => p.stablecoin === true)).toBe(true);
      expect(stablePools.every(p => p.ilRisk === 'no')).toBe(true);
      expect(stablePools.every(p => p.tvlUsd >= 1000000)).toBe(true);
      expect(stablePools.every(p => p.apy > 0)).toBe(true);
    });
  });

  describe('Edge Cases', () => {
    test('handles empty pool list', () => {
      const filtered = filterPools([]);
      expect(filtered).toEqual([]);
    });

    test('handles pools with zero TVL', () => {
      const poolsWithZeroTVL = [
        { ...mockPoolData[0], tvlUsd: 0 },
      ];
      const filtered = filterPools(poolsWithZeroTVL);
      expect(filtered).toEqual([]);
    });

    test('handles pools with zero APY', () => {
      const poolsWithZeroAPY = [
        { ...mockPoolData[0], apy: 0 },
      ];
      const filtered = filterPools(poolsWithZeroAPY);
      expect(filtered).toEqual([]);
    });

    test('handles pool type case-insensitivity', () => {
      const ethLower = getPoolsByType(mockPoolData, 'eth');
      const ethUpper = getPoolsByType(mockPoolData, 'ETH');
      const stableLower = getPoolsByType(mockPoolData, 'stables');
      const stableUpper = getPoolsByType(mockPoolData, 'STABLES');
      
      expect(ethLower).toEqual(ethUpper);
      expect(stableLower).toEqual(stableUpper);
    });
  });

  describe('Response Format', () => {
    test('API returns correct response structure', () => {
      const filtered = filterPools(mockPoolData);
      expect(Array.isArray(filtered)).toBe(true);
      expect(filtered.length > 0).toBe(true);
    });

    test('error response has correct structure', () => {
      const invalidType = getPoolsByType(mockPoolData, 'INVALID');
      expect(Array.isArray(invalidType)).toBe(true);
      expect(invalidType.length).toBe(0);
    });

    test('pool object has all required fields', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach(pool => {
        expect(pool).toHaveProperty('symbol');
        expect(pool).toHaveProperty('tvlUsd');
        expect(pool).toHaveProperty('apy');
        expect(pool).toHaveProperty('ilRisk');
        expect(pool).toHaveProperty('stablecoin');
      });
    });
  });

  describe('Data Types', () => {
    test('TVL is a number', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach(pool => {
        expect(typeof pool.tvlUsd).toBe('number');
      });
    });

    test('APY is a number', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach(pool => {
        expect(typeof pool.apy).toBe('number');
      });
    });

    test('symbol is a string', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach(pool => {
        expect(typeof pool.symbol).toBe('string');
      });
    });

    test('stablecoin is a boolean', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach(pool => {
        expect(typeof pool.stablecoin).toBe('boolean');
      });
    });

    test('ilRisk is a string', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach(pool => {
        expect(typeof pool.ilRisk).toBe('string');
      });
    });
  });
});
