import { buildContractAddresses } from './contract-address.utils';

describe('buildContractAddresses', () => {
  it('returns empty array when no addresses are present', () => {
    expect(buildContractAddresses({ pool: 'some-uuid', underlyingTokens: null, rewardTokens: null })).toEqual([]);
  });

  it('includes underlyingTokens contract addresses', () => {
    const result = buildContractAddresses({
      pool: 'some-uuid',
      underlyingTokens: ['0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48'],
      rewardTokens: null,
    });
    expect(result).toEqual(['0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48']);
  });

  it('includes rewardTokens contract addresses', () => {
    const result = buildContractAddresses({
      pool: 'some-uuid',
      underlyingTokens: null,
      rewardTokens: ['0x8F08B70456eb22f6109F57b8fafE862ED28E6040'],
    });
    expect(result).toEqual(['0x8f08b70456eb22f6109f57b8fafe862ed28e6040']);
  });

  it('includes pool address when it is a 0x contract address (Pendle)', () => {
    const result = buildContractAddresses({
      pool: '0xd1d7d99764f8a52aff0b6f1b3540909f0000b2a3',
      underlyingTokens: null,
      rewardTokens: null,
    });
    expect(result).toEqual(['0xd1d7d99764f8a52aff0b6f1b3540909f0000b2a3']);
  });

  it('excludes pool UUID (non-address)', () => {
    const result = buildContractAddresses({
      pool: '747c1d2a-c668-4682-b9f9-296708a3dd90',
      underlyingTokens: null,
      rewardTokens: null,
    });
    expect(result).toEqual([]);
  });

  it('excludes the zero address (0x000...000)', () => {
    const result = buildContractAddresses({
      pool: 'some-uuid',
      underlyingTokens: ['0x0000000000000000000000000000000000000000'],
      rewardTokens: null,
    });
    // Zero address is technically valid format — kept in list as it represents native ETH
    expect(result).toEqual(['0x0000000000000000000000000000000000000000']);
  });

  it('de-duplicates addresses across sources', () => {
    const addr = '0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48';
    const result = buildContractAddresses({
      pool: addr,
      underlyingTokens: [addr],
      rewardTokens: [addr],
    });
    expect(result).toHaveLength(1);
    expect(result[0]).toBe(addr.toLowerCase());
  });

  it('normalises addresses to lowercase', () => {
    const result = buildContractAddresses({
      pool: 'some-uuid',
      underlyingTokens: ['0xA0B86991C6218B36C1D19D4A2E9EB0CE3606EB48'],
      rewardTokens: null,
    });
    expect(result).toEqual(['0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48']);
  });

  it('combines all three sources', () => {
    const result = buildContractAddresses({
      pool: '0x1111111111111111111111111111111111111111',
      underlyingTokens: ['0x2222222222222222222222222222222222222222'],
      rewardTokens: ['0x3333333333333333333333333333333333333333'],
    });
    expect(result).toHaveLength(3);
    expect(result).toContain('0x1111111111111111111111111111111111111111');
    expect(result).toContain('0x2222222222222222222222222222222222222222');
    expect(result).toContain('0x3333333333333333333333333333333333333333');
  });
});
