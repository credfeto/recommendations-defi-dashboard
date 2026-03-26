import { filterPools, getPoolsByType, applyBaseFilters, filterPoolsByType } from '../../services/pools.service';
import { getAvailablePoolTypesMetadata, POOL_TYPES_METADATA } from '@shared';
import { getPoolsByNameSchema } from '../schemas';
// eslint-disable-next-line @typescript-eslint/no-var-requires
const fastJsonStringify = require('fast-json-stringify');

interface MockPool {
  chain: string;
  project: string;
  symbol: string;
  tvlUsd: number;
  apy: number;
  ilRisk: string;
  stablecoin: boolean;
  pool: string;
}

const mockPoolData: MockPool[] = [
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
  {
    chain: 'Ethereum',
    project: 'rocketpool',
    symbol: 'RETH',
    tvlUsd: 8000000000,
    apy: 6.5,
    ilRisk: 'no',
    stablecoin: false,
    pool: '7',
  },
  {
    chain: 'Ethereum',
    project: 'coinbase',
    symbol: 'CBETH',
    tvlUsd: 3000000000,
    apy: 5.2,
    ilRisk: 'no',
    stablecoin: false,
    pool: '8',
  },
];

describe('Server API Tests', () => {
  describe('Pool Filtering', () => {
    test('filters pools with no IL risk', () => {
      const filtered = filterPools(mockPoolData);
      expect(filtered.every((p) => p.ilRisk === 'no')).toBe(true);
    });

    test('filters pools with sufficient TVL', () => {
      const filtered = filterPools(mockPoolData);
      expect(filtered.every((p) => p.tvlUsd >= 1000000)).toBe(true);
    });

    test('filters pools with positive APY', () => {
      const filtered = filterPools(mockPoolData);
      expect(filtered.every((p) => p.apy > 0)).toBe(true);
    });

    test('combines all filters correctly', () => {
      const filtered = filterPools(mockPoolData);
      expect(filtered.length).toBe(6);
      expect(filtered.map((p) => p.symbol)).toEqual(['RETH', 'CBETH', 'USDC', 'WEETH', 'STETH', 'USDT']);
    });
  });

  describe('Pool Type Categorization', () => {
    test('identifies ETH-based pools correctly', () => {
      const ethPools = getPoolsByType(mockPoolData, 'ETH');
      expect(ethPools.length).toBe(4);
      expect(ethPools.map((p) => p.symbol)).toEqual(['RETH', 'CBETH', 'WEETH', 'STETH']);
    });

    test('identifies stablecoin pools correctly', () => {
      const stablePools = getPoolsByType(mockPoolData, 'STABLES');
      expect(stablePools.length).toBe(2);
      expect(stablePools.map((p) => p.symbol)).toEqual(['USDC', 'USDT']);
    });

    test('identifies high yield pools (>5% APY)', () => {
      const highYieldPools = filterPoolsByType(mockPoolData, 'HIGH_YIELD');
      expect(highYieldPools.length).toBe(2);
      expect(highYieldPools.map((p) => p.symbol)).toEqual(['RETH', 'CBETH']);
    });

    test('identifies blue chip pools (>$100M TVL)', () => {
      const blueChipPools = filterPoolsByType(mockPoolData, 'BLUE_CHIP');
      expect(blueChipPools.length).toBe(5);
      expect(blueChipPools.map((p) => p.symbol)).toEqual(['RETH', 'CBETH', 'USDC', 'WEETH', 'STETH']);
    });

    test('filters ETH pools with all criteria', () => {
      const ethPools = getPoolsByType(mockPoolData, 'ETH');
      expect(ethPools.every((p) => p.symbol.toUpperCase().includes('ETH'))).toBe(true);
      expect(ethPools.every((p) => p.ilRisk === 'no')).toBe(true);
      expect(ethPools.every((p) => p.tvlUsd >= 1000000)).toBe(true);
      expect(ethPools.every((p) => p.apy > 0)).toBe(true);
    });

    test('filters stablecoin pools with all criteria', () => {
      const stablePools = getPoolsByType(mockPoolData, 'STABLES');
      expect(stablePools.every((p) => p.stablecoin === true)).toBe(true);
      expect(stablePools.every((p) => p.ilRisk === 'no')).toBe(true);
      expect(stablePools.every((p) => p.tvlUsd >= 1000000)).toBe(true);
      expect(stablePools.every((p) => p.apy > 0)).toBe(true);
    });
  });

  describe('Edge Cases', () => {
    test('handles empty pool list', () => {
      const filtered = filterPools([]);
      expect(filtered).toEqual([]);
    });

    test('handles pools with zero TVL', () => {
      const poolsWithZeroTVL = [{ ...mockPoolData[0], tvlUsd: 0 }];
      const filtered = filterPools(poolsWithZeroTVL);
      expect(filtered).toEqual([]);
    });

    test('handles pools with zero APY', () => {
      const poolsWithZeroAPY = [{ ...mockPoolData[0], apy: 0 }];
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

    test('handles invalid pool type', () => {
      const invalidType = filterPoolsByType(mockPoolData, 'INVALID');
      expect(Array.isArray(invalidType)).toBe(true);
      expect(invalidType.length).toBe(0);
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
      filtered.forEach((pool) => {
        expect(pool).toHaveProperty('symbol');
        expect(pool).toHaveProperty('tvlUsd');
        expect(pool).toHaveProperty('apy');
        expect(pool).toHaveProperty('ilRisk');
        expect(pool).toHaveProperty('stablecoin');
      });
    });
  });

  describe('Available Pool Types', () => {
    test('getAvailableTypes returns all pool type configs', () => {
      const { getAvailableTypes } = require('../../services/pools.service');
      const types = getAvailableTypes();
      expect(Array.isArray(types)).toBe(true);
      expect(types.length).toBeGreaterThan(0);
      expect(types.some((t) => t.id === 'ETH')).toBe(true);
      expect(types.some((t) => t.id === 'STABLES')).toBe(true);
    });

    test('getFilteredPools wrapper function works', () => {
      const { getFilteredPools } = require('../../services/pools.service');
      const pools = getFilteredPools(mockPoolData, 'ETH');
      expect(Array.isArray(pools)).toBe(true);
      expect(pools.length).toBeGreaterThan(0);
      expect(pools.every((p) => p.symbol.toUpperCase().includes('ETH'))).toBe(true);
    });
  });

  describe('Data Types', () => {
    test('TVL is a number', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach((pool) => {
        expect(typeof pool.tvlUsd).toBe('number');
      });
    });

    test('APY is a number', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach((pool) => {
        expect(typeof pool.apy).toBe('number');
      });
    });

    test('symbol is a string', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach((pool) => {
        expect(typeof pool.symbol).toBe('string');
      });
    });

    test('stablecoin is a boolean', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach((pool) => {
        expect(typeof pool.stablecoin).toBe('boolean');
      });
    });

    test('ilRisk is a string', () => {
      const filtered = filterPools(mockPoolData);
      filtered.forEach((pool) => {
        expect(typeof pool.ilRisk).toBe('string');
      });
    });
  });

  describe('Pool Types Metadata', () => {
    test('getAvailablePoolTypesMetadata returns metadata for all pool types', () => {
      const metadata = getAvailablePoolTypesMetadata();
      expect(Array.isArray(metadata)).toBe(true);
      expect(metadata.length).toBeGreaterThan(0);
    });

    test('pool type metadata has required fields', () => {
      const metadata = getAvailablePoolTypesMetadata();
      metadata.forEach((type) => {
        expect(type).toHaveProperty('name');
        expect(type).toHaveProperty('displayName');
        expect(typeof type.name).toBe('string');
        expect(typeof type.displayName).toBe('string');
      });
    });

    test('includes all required pool types', () => {
      const metadata = getAvailablePoolTypesMetadata();
      const names = metadata.map((t) => t.name);
      expect(names).toContain('ETH');
      expect(names).toContain('STABLES');
      expect(names).toContain('HIGH_YIELD');
      expect(names).toContain('LOW_TVL');
      expect(names).toContain('BLUE_CHIP');
    });

    test('display names are properly formatted', () => {
      const metadata = Object.values(POOL_TYPES_METADATA);
      metadata.forEach((type) => {
        expect(type.displayName.length > 0).toBe(true);
        expect(typeof type.displayName).toBe('string');
      });
    });
  });

  describe('Pool Response Schema', () => {
    // Regression guard: the schema must handle heterogeneous DeFiLlama + Pendle data.
    // Pendle pools are missing exposure, predictions, mu, sigma, count, outlier, apyMean30d.
    // DeFiLlama pools can have predictions: null.
    // fast-json-stringify must not throw for either shape.

    const pendlePool = {
      chain: 'Ethereum',
      project: 'pendle',
      symbol: 'sUSDe',
      tvlUsd: 50_000_000,
      apy: 8.2,
      apyBase: 7.0,
      apyReward: null,
      rewardTokens: null,
      pool: '0xabc123',
      stablecoin: true,
      ilRisk: 'no',
      poolMeta: 'Maturity: Jun 2026',
      volumeUsd1d: 1_200_000,
      dataSource: 'pendle',
      hacks: [],
      depegAlerts: [],
    };

    const defillamaPool = {
      chain: 'Ethereum',
      project: 'lido',
      symbol: 'STETH',
      tvlUsd: 20_000_000_000,
      apy: 2.5,
      apyBase: 2.5,
      apyReward: null,
      rewardTokens: null,
      pool: 'steth-pool-id',
      stablecoin: false,
      ilRisk: 'no',
      exposure: 'single',
      predictions: null,
      poolMeta: null,
      mu: 2.4,
      sigma: 0.3,
      count: 365,
      outlier: false,
      apyMean30d: 2.45,
      volumeUsd1d: null,
      volumeUsd7d: null,
      apyPct1D: 0.1,
      apyPct7D: 0.5,
      apyPct30D: 1.2,
      il7d: null,
      apyBase7d: null,
      apyBaseInception: null,
      underlyingTokens: null,
      url: 'https://defillama.com/yields?pool=steth-pool-id',
      dataSource: 'defillama',
      hacks: [],
      depegAlerts: [],
    };

    let serialize: (data: unknown) => string;

    beforeAll(() => {
      serialize = fastJsonStringify(getPoolsByNameSchema.response[200]);
    });

    test('schema does not require exposure field', () => {
      const required: readonly string[] = (getPoolsByNameSchema.response[200] as any).properties.data.items.required;
      expect(required).not.toContain('exposure');
    });

    test('predictions field allows null in schema', () => {
      const predictionsSchema = (getPoolsByNameSchema.response[200] as any).properties.data.items.properties
        .predictions;
      expect(predictionsSchema).toHaveProperty('anyOf');
      expect(predictionsSchema.anyOf).toContainEqual({ type: 'null' });
    });

    test('nullable statistical fields allow null in schema', () => {
      const props = (getPoolsByNameSchema.response[200] as any).properties.data.items.properties;
      for (const field of ['mu', 'sigma', 'count', 'apyMean30d']) {
        expect(props[field].type).toContain('null');
      }
      expect(props['outlier'].type).toContain('null');
    });

    test('serializes Pendle pool (no exposure/predictions/mu/sigma) without throwing', () => {
      expect(() => serialize({ status: 'ok', data: [pendlePool] })).not.toThrow();
    });

    test('serializes DeFiLlama pool with predictions: null without throwing', () => {
      expect(() => serialize({ status: 'ok', data: [defillamaPool] })).not.toThrow();
    });

    test('Pendle pool serialization produces valid JSON with correct values', () => {
      const result = JSON.parse(serialize({ status: 'ok', data: [pendlePool] }));
      expect(result.status).toBe('ok');
      expect(result.data).toHaveLength(1);
      expect(result.data[0].project).toBe('pendle');
      expect(result.data[0].apy).toBeCloseTo(8.2);
    });

    test('DeFiLlama pool serialization preserves null predictions', () => {
      const result = JSON.parse(serialize({ status: 'ok', data: [defillamaPool] }));
      expect(result.data[0].predictions).toBeNull();
    });

  });
});
