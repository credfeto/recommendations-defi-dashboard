import axios from 'axios';
import { DefiLlamaPoolsApiService } from './defillama.pools.api.service';

jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

const rawPools = [
  { pool: 'abc', project: 'aave', symbol: 'USDC', chain: 'Ethereum', tvlUsd: 5000000, apy: 3.5 },
  { pool: 'def', project: 'pendle', symbol: 'sUSDe', chain: 'Ethereum', tvlUsd: 1000000, apy: 8.2 },
  { pool: 'ghi', project: 'lido', symbol: 'STETH', chain: 'Ethereum', tvlUsd: 20000000000, apy: 2.5 },
];

describe('DefiLlamaPoolsApiService', () => {
  let service: DefiLlamaPoolsApiService;

  beforeEach(() => {
    service = new DefiLlamaPoolsApiService();
    jest.clearAllMocks();
  });

  test('returns pools with defillama dataSource added', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: { data: rawPools } });

    const result = await service.fetchPools();

    expect(result.every((p) => p.dataSource === 'defillama')).toBe(true);
  });

  test('excludes pendle pools', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: { data: rawPools } });

    const result = await service.fetchPools();

    expect(result.some((p) => p.project === 'pendle')).toBe(false);
    expect(result).toHaveLength(2);
  });

  test('calls the correct DeFiLlama pools URL', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: { data: [] } });

    await service.fetchPools();

    expect(mockedAxios.get).toHaveBeenCalledWith('https://yields.llama.fi/pools');
  });

  test('returns empty array when response data is null', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: { data: null } });

    const result = await service.fetchPools();

    expect(result).toEqual([]);
  });

  test('throws on network error', async () => {
    mockedAxios.get.mockRejectedValueOnce(new Error('Network Error'));

    await expect(service.fetchPools()).rejects.toThrow('Network Error');
  });

  test('preserves all original pool fields', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: { data: rawPools } });

    const result = await service.fetchPools();
    const aave = result.find((p) => p.project === 'aave');

    expect(aave).toBeDefined();
    expect(aave?.pool).toBe('abc');
    expect(aave?.symbol).toBe('USDC');
    expect(aave?.tvlUsd).toBe(5000000);
  });
});
