import axios from 'axios';
import { DefiLlamaProtocolsApiService, RawProtocol } from './defillama.protocols.api.service';

jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

const mockProtocols: RawProtocol[] = [
  {
    slug: 'aave-v3',
    audits: '2',
    audit_links: ['https://aave.com/security'],
  },
  {
    slug: 'lido',
    audits: '2',
    audit_links: ['https://github.com/lidofinance/audits'],
  },
  {
    slug: 'unknown-protocol',
    audits: '0',
    audit_links: null,
  },
];

describe('DefiLlamaProtocolsApiService', () => {
  let service: DefiLlamaProtocolsApiService;

  beforeEach(() => {
    service = new DefiLlamaProtocolsApiService();
    jest.clearAllMocks();
  });

  test('returns protocols array on success', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: mockProtocols });

    const result = await service.fetchProtocols();

    expect(result).toEqual(mockProtocols);
    expect(mockedAxios.get).toHaveBeenCalledWith('https://api.llama.fi/protocols');
  });

  test('returns empty array when response data is null', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: null });

    const result = await service.fetchProtocols();

    expect(result).toEqual([]);
  });

  test('throws on network error', async () => {
    mockedAxios.get.mockRejectedValueOnce(new Error('Network Error'));

    await expect(service.fetchProtocols()).rejects.toThrow('Network Error');
  });

  test('each protocol has a slug field', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: mockProtocols });

    const result = await service.fetchProtocols();

    result.forEach((protocol) => {
      expect(protocol).toHaveProperty('slug');
      expect(typeof protocol.slug).toBe('string');
    });
  });

  test('audit_links is optional and can be null', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: mockProtocols });

    const result = await service.fetchProtocols();

    const withLinks = result.filter((p) => p.audit_links !== null && p.audit_links !== undefined);
    const withoutLinks = result.filter((p) => p.audit_links === null || p.audit_links === undefined);
    expect(withLinks.length).toBeGreaterThan(0);
    expect(withoutLinks.length).toBeGreaterThan(0);
  });
});
