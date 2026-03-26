import axios from 'axios';
import { PendleMarketsApiService, normalizePendleMarket, PendlePoolData } from './pendle.markets.api.service';

jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

const mockMarket = {
  address: '0xabc123',
  chainId: 1,
  simpleSymbol: 'sUSDe',
  expiry: '2026-06-26T00:00:00.000Z',
  isActive: true,
  categoryIds: ['stables', 'ethena'],
  liquidity: { usd: 50_000_000 },
  aggregatedApy: 0.082,
  underlyingApy: 0.07,
  pendleApy: 0.006,
  lpRewardApy: 0.004,
  swapFeeApy: 0.001,
  tradingVolume: { usd: 1_200_000 },
};

const mockMarketPage = { total: 1, results: [mockMarket] };

describe('PendleMarketsApiService', () => {
  let service: PendleMarketsApiService;

  beforeEach(() => {
    service = new PendleMarketsApiService();
    jest.clearAllMocks();
  });

  describe('fetchMarkets', () => {
    test('fetches markets from all configured chains', async () => {
      mockedAxios.get.mockResolvedValue({ data: mockMarketPage });

      await service.fetchMarkets();

      // 4 chains: Ethereum (1), Arbitrum (42161), Base (8453), BSC (56)
      expect(mockedAxios.get).toHaveBeenCalledTimes(4);
    });

    test('returns normalized PendlePoolData', async () => {
      mockedAxios.get.mockResolvedValue({ data: mockMarketPage });

      const result = await service.fetchMarkets();

      expect(result.length).toBeGreaterThan(0);
      expect(result[0].project).toBe('pendle');
      expect(result[0].dataSource).toBe('pendle');
    });

    test('excludes inactive markets', async () => {
      const inactiveMarket = { ...mockMarket, isActive: false };
      mockedAxios.get.mockResolvedValue({ data: { total: 1, results: [inactiveMarket] } });

      const result = await service.fetchMarkets();

      expect(result).toHaveLength(0);
    });

    test('tolerates failed chain requests via allSettled', async () => {
      mockedAxios.get
        .mockResolvedValueOnce({ data: mockMarketPage })
        .mockRejectedValueOnce(new Error('Chain unavailable'))
        .mockResolvedValueOnce({ data: mockMarketPage })
        .mockRejectedValueOnce(new Error('Chain unavailable'));

      const result = await service.fetchMarkets();

      expect(result.length).toBe(2); // 2 chains succeeded
    });

    test('paginates until all markets are fetched', async () => {
      const page1Market = mockMarket;
      const page2Market = { ...mockMarket, address: '0xdef456' };

      // Use URL + skip param to serve pages deterministically for chain 1;
      // all other chains return empty so only chain 1 exercises the pagination path.
      mockedAxios.get.mockImplementation((url: string, config?: { params?: { skip?: number } }) => {
        if (url.includes('/1/markets')) {
          const skip = config?.params?.skip ?? 0;
          if (skip === 0) return Promise.resolve({ data: { total: 2, results: [page1Market] } });
          return Promise.resolve({ data: { total: 2, results: [page2Market] } });
        }
        return Promise.resolve({ data: { total: 0, results: [] } });
      });

      const result = await service.fetchMarkets();

      expect(result.some((r) => r.pool === '0xabc123')).toBe(true);
      expect(result.some((r) => r.pool === '0xdef456')).toBe(true);
    });
  });
});

describe('normalizePendleMarket', () => {
  test('converts decimal APY to percentage', () => {
    const result = normalizePendleMarket(mockMarket);
    expect(result.apy).toBeCloseTo(8.2, 5);
    expect(result.apyBase).toBeCloseTo(7.0, 5);
  });

  test('maps liquidity.usd to tvlUsd', () => {
    expect(normalizePendleMarket(mockMarket).tvlUsd).toBe(50_000_000);
  });

  test('detects stablecoin from categoryIds', () => {
    expect(normalizePendleMarket(mockMarket).stablecoin).toBe(true);
    expect(normalizePendleMarket({ ...mockMarket, categoryIds: ['eth'] }).stablecoin).toBe(false);
  });

  test('maps chainId to chain name', () => {
    expect(normalizePendleMarket({ ...mockMarket, chainId: 1 }).chain).toBe('Ethereum');
    expect(normalizePendleMarket({ ...mockMarket, chainId: 42161 }).chain).toBe('Arbitrum');
    expect(normalizePendleMarket({ ...mockMarket, chainId: 8453 }).chain).toBe('Base');
    expect(normalizePendleMarket({ ...mockMarket, chainId: 56 }).chain).toBe('BSC');
    expect(normalizePendleMarket({ ...mockMarket, chainId: 999 }).chain).toBe('999');
  });

  test('sets project and dataSource', () => {
    const result = normalizePendleMarket(mockMarket);
    expect(result.project).toBe('pendle');
    expect(result.dataSource).toBe('pendle');
    expect(result.ilRisk).toBe('no');
  });

  test('uses market address as pool id', () => {
    expect(normalizePendleMarket(mockMarket).pool).toBe('0xabc123');
  });

  test('formats poolMeta with maturity date', () => {
    expect(normalizePendleMarket(mockMarket).poolMeta).toContain('Maturity');
  });

  test('passes through trading volume', () => {
    expect(normalizePendleMarket(mockMarket).volumeUsd1d).toBe(1_200_000);
  });

  test('volumeUsd1d is null when tradingVolume is absent', () => {
    const { tradingVolume: _, ...noVolume } = mockMarket;
    expect(normalizePendleMarket(noVolume as any).volumeUsd1d).toBeNull();
  });

  test('does not include exposure field', () => {
    expect(normalizePendleMarket(mockMarket)).not.toHaveProperty('exposure');
  });
});
