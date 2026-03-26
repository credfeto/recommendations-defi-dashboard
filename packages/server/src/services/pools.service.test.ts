import { applyBaseFilters, filterPoolsByType } from './pools.service';
import { PoolData } from '@shared/types/poolData';

const makePool = (overrides: Partial<PoolData> = {}): PoolData => ({
  chain: 'Ethereum',
  project: 'test-protocol',
  symbol: 'USDC-ETH',
  tvlUsd: 5_000_000,
  apy: 10,
  apyBase: 10,
  apyReward: 0,
  ilRisk: 'no',
  dataSource: 'defillama',
  pool: 'pool-uuid',
  stablecoin: false,
  ...overrides,
});

describe('applyBaseFilters', () => {
  test('returns empty array for empty input', () => {
    expect(applyBaseFilters([])).toEqual([]);
  });

  test('filters out pools with IL risk', () => {
    const result = applyBaseFilters([makePool({ ilRisk: 'yes' })]);
    expect(result).toHaveLength(0);
  });

  test('filters out pools below minimum TVL', () => {
    const result = applyBaseFilters([makePool({ tvlUsd: 999_999 })]);
    expect(result).toHaveLength(0);
  });

  test('keeps pools exactly at minimum TVL', () => {
    const result = applyBaseFilters([makePool({ tvlUsd: 1_000_000 })]);
    expect(result).toHaveLength(1);
  });

  test('filters out pools with zero APY', () => {
    const result = applyBaseFilters([makePool({ apy: 0 })]);
    expect(result).toHaveLength(0);
  });

  test('filters out pools with APY >= 100', () => {
    expect(applyBaseFilters([makePool({ apy: 100 })])).toHaveLength(0);
    expect(applyBaseFilters([makePool({ apy: 150 })])).toHaveLength(0);
  });

  test('keeps pools with APY just below 100', () => {
    expect(applyBaseFilters([makePool({ apy: 99.9 })])).toHaveLength(1);
  });

  test('filters out excluded chains', () => {
    const excluded = ['Aptos', 'Avalanche', 'Ton', 'Tron', 'Sui', 'Stellar'];
    for (const chain of excluded) {
      const result = applyBaseFilters([makePool({ chain })]);
      expect(result).toHaveLength(0);
    }
  });

  test('chain exclusion is case-insensitive', () => {
    const result = applyBaseFilters([makePool({ chain: 'aptos' })]);
    expect(result).toHaveLength(0);
  });

  test('sorts by APY descending', () => {
    const pools = [makePool({ apy: 5 }), makePool({ apy: 20 }), makePool({ apy: 10 })];
    const result = applyBaseFilters(pools);
    expect(result[0].apy).toBe(20);
    expect(result[1].apy).toBe(10);
    expect(result[2].apy).toBe(5);
  });

  test('sorts by TVL descending when APY is equal', () => {
    const pools = [makePool({ apy: 10, tvlUsd: 1_000_000 }), makePool({ apy: 10, tvlUsd: 5_000_000 })];
    const result = applyBaseFilters(pools);
    expect(result[0].tvlUsd).toBe(5_000_000);
  });

  test('passes valid pool through', () => {
    const pool = makePool();
    const result = applyBaseFilters([pool]);
    expect(result).toHaveLength(1);
    expect(result[0]).toEqual(pool);
  });
});

describe('filterPoolsByType', () => {
  test('returns empty array for unknown pool type', () => {
    expect(filterPoolsByType([makePool()], 'nonexistent-type')).toEqual([]);
  });

  test('returns only pools matching the pool type predicate', () => {
    const stablecoinPool = makePool({ stablecoin: true });
    const nonStablecoinPool = makePool({ stablecoin: false });
    const result = filterPoolsByType([stablecoinPool, nonStablecoinPool], 'stablecoin');
    expect(result.every((p) => p.stablecoin)).toBe(true);
  });
});
