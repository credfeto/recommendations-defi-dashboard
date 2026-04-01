import axios from 'axios';
import { GoPlusApiService, CHAIN_NAME_TO_ID } from './goplus.api.service';

jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

const mockResult = {
  '0xae7ab96520de3a18e5e111b5eaab095312d7fe84': {
    is_open_source: '1',
    is_honeypot: '0',
    is_proxy: '1',
    buy_tax: '0',
    sell_tax: '0',
    transfer_tax: '0',
    cannot_buy: '0',
    honeypot_with_same_creator: '0',
    token_name: 'Liquid staked Ether 2.0',
    token_symbol: 'stETH',
  },
};

describe('GoPlusApiService', () => {
  let service: GoPlusApiService;

  beforeEach(() => {
    service = new GoPlusApiService();
    jest.clearAllMocks();
  });

  test('returns empty map for unsupported chain', async () => {
    const result = await service.fetchTokenSecurity('Solana', ['0xabc']);
    expect(result.size).toBe(0);
    expect(mockedAxios.get).not.toHaveBeenCalled();
  });

  test('returns empty map for empty address list', async () => {
    const result = await service.fetchTokenSecurity('Ethereum', []);
    expect(result.size).toBe(0);
    expect(mockedAxios.get).not.toHaveBeenCalled();
  });

  test('calls correct GoPlus URL with chain ID', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: { code: 1, result: mockResult } });

    await service.fetchTokenSecurity('Ethereum', ['0xae7ab96520de3a18e5e111b5eaab095312d7fe84']);

    expect(mockedAxios.get).toHaveBeenCalledWith(
      'https://api.gopluslabs.io/api/v1/token_security/1',
      expect.objectContaining({
        params: { contract_addresses: '0xae7ab96520de3a18e5e111b5eaab095312d7fe84' },
      }),
    );
  });

  test('joins multiple addresses with comma', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: { code: 1, result: {} } });

    await service.fetchTokenSecurity('Ethereum', ['0xAAA', '0xBBB']);

    expect(mockedAxios.get).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({ params: { contract_addresses: '0xaaa,0xbbb' } }),
    );
  });

  test('returns map of lowercased addresses', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: { code: 1, result: mockResult } });

    const result = await service.fetchTokenSecurity('Ethereum', ['0xAE7AB96520DE3A18E5E111B5EAAB095312D7FE84']);

    expect(result.has('0xae7ab96520de3a18e5e111b5eaab095312d7fe84')).toBe(true);
  });

  test('returns correct security fields', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: { code: 1, result: mockResult } });

    const result = await service.fetchTokenSecurity('Ethereum', ['0xae7ab96520de3a18e5e111b5eaab095312d7fe84']);
    const info = result.get('0xae7ab96520de3a18e5e111b5eaab095312d7fe84');

    expect(info?.is_open_source).toBe('1');
    expect(info?.is_proxy).toBe('1');
    expect(info?.is_honeypot).toBe('0');
    expect(info?.token_symbol).toBe('stETH');
  });

  test('returns empty map on network error', async () => {
    mockedAxios.get.mockRejectedValueOnce(new Error('Network Error'));

    const result = await service.fetchTokenSecurity('Ethereum', ['0xabc']);

    expect(result.size).toBe(0);
  });

  test('returns empty map when response has no result', async () => {
    mockedAxios.get.mockResolvedValueOnce({ data: { code: 1, result: null } });

    const result = await service.fetchTokenSecurity('Ethereum', ['0xabc']);

    expect(result.size).toBe(0);
  });

  test('uses correct chain IDs', () => {
    expect(CHAIN_NAME_TO_ID['Ethereum']).toBe(1);
    expect(CHAIN_NAME_TO_ID['Arbitrum']).toBe(42161);
    expect(CHAIN_NAME_TO_ID['Base']).toBe(8453);
    expect(CHAIN_NAME_TO_ID['BSC']).toBe(56);
  });
});
