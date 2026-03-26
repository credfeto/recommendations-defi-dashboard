import axios from 'axios';
import { DefiLlamaHacksApiService, RawHack } from './defillama.hacks.api.service';

jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

const mockHacks: RawHack[] = [
  {
    date: 1609459200,
    name: 'Compound',
    classification: 'Protocol',
    technique: 'Access Control',
    amount: 150000000,
    source: 'https://example.com',
    parentProtocolId: 'parent#compound',
  },
  {
    date: 1620000000,
    name: 'SushiSwap',
    classification: 'Protocol',
    technique: 'Reentrancy',
    amount: 3000000,
    source: 'https://example.com',
  },
];

describe('DefiLlamaHacksApiService', () => {
  let service: DefiLlamaHacksApiService;

  beforeEach(() => {
    service = new DefiLlamaHacksApiService();
    jest.clearAllMocks();
  });

  test('returns hacks array on success', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: mockHacks });

    const result = await service.fetchHacks();

    expect(result).toEqual(mockHacks);
    expect(mockedAxios.get).toHaveBeenCalledWith('https://api.llama.fi/hacks');
  });

  test('returns empty array when response data is null', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: null });

    const result = await service.fetchHacks();

    expect(result).toEqual([]);
  });

  test('throws on network error', async () => {
    mockedAxios.get.mockRejectedValueOnce(new Error('Network Error'));

    await expect(service.fetchHacks()).rejects.toThrow('Network Error');
  });

  test('each hack has required fields', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: mockHacks });

    const result = await service.fetchHacks();

    result.forEach((hack) => {
      expect(hack).toHaveProperty('date');
      expect(hack).toHaveProperty('name');
      expect(hack).toHaveProperty('classification');
      expect(hack).toHaveProperty('technique');
      expect(hack).toHaveProperty('amount');
      expect(hack).toHaveProperty('source');
      expect(typeof hack.date).toBe('number');
      expect(typeof hack.amount).toBe('number');
    });
  });

  test('parentProtocolId is optional', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: mockHacks });

    const result = await service.fetchHacks();

    const withParent = result.filter((h) => h.parentProtocolId !== undefined);
    const withoutParent = result.filter((h) => h.parentProtocolId === undefined);
    expect(withParent.length).toBeGreaterThan(0);
    expect(withoutParent.length).toBeGreaterThan(0);
  });
});
