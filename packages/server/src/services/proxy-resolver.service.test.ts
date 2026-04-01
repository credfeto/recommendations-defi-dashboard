import axios from 'axios';
import { resolveProxyImplementation } from './proxy-resolver.service';

jest.mock('axios');
jest.mock('../config/rpc.config');

const mockedAxios = axios as jest.Mocked<typeof axios>;
import { getRpcUrl } from '../config/rpc.config';
const mockedGetRpcUrl = getRpcUrl as jest.MockedFunction<typeof getRpcUrl>;

const ZERO_SLOT = '0x' + '0'.repeat(64);
const IMPL_ADDR = '0xabcdefabcdefabcdefabcdefabcdefabcdefabcd';
// Padded to 32 bytes (left-padded with zeros, address in last 20 bytes)
const IMPL_SLOT_VALUE = '0x000000000000000000000000' + IMPL_ADDR.slice(2);

describe('resolveProxyImplementation', () => {
  beforeEach(() => jest.clearAllMocks());

  test('returns null when no RPC is configured for chain', async () => {
    mockedGetRpcUrl.mockReturnValue(null);

    const result = await resolveProxyImplementation('Ethereum', '0xproxy');

    expect(result).toBeNull();
    expect(mockedAxios.post).not.toHaveBeenCalled();
  });

  test('returns implementation address from EIP-1967 slot', async () => {
    mockedGetRpcUrl.mockReturnValue('https://rpc.example.com');
    mockedAxios.post.mockResolvedValueOnce({ data: { result: IMPL_SLOT_VALUE } });

    const result = await resolveProxyImplementation('Ethereum', '0xproxy');

    expect(result).toBe(IMPL_ADDR.toLowerCase());
    // Should only call one slot since EIP-1967 matched
    expect(mockedAxios.post).toHaveBeenCalledTimes(1);
  });

  test('falls through to beacon slot when EIP-1967 is zero', async () => {
    mockedGetRpcUrl.mockReturnValue('https://rpc.example.com');
    mockedAxios.post
      .mockResolvedValueOnce({ data: { result: ZERO_SLOT } })       // EIP-1967 slot: zero
      .mockResolvedValueOnce({ data: { result: IMPL_SLOT_VALUE } }); // beacon slot: match

    const result = await resolveProxyImplementation('Ethereum', '0xproxy');

    expect(result).toBe(IMPL_ADDR.toLowerCase());
    expect(mockedAxios.post).toHaveBeenCalledTimes(2);
  });

  test('falls through to OZ legacy slot when EIP-1967 and beacon are zero', async () => {
    mockedGetRpcUrl.mockReturnValue('https://rpc.example.com');
    mockedAxios.post
      .mockResolvedValueOnce({ data: { result: ZERO_SLOT } })       // EIP-1967: zero
      .mockResolvedValueOnce({ data: { result: ZERO_SLOT } })       // beacon: zero
      .mockResolvedValueOnce({ data: { result: IMPL_SLOT_VALUE } }); // OZ legacy: match

    const result = await resolveProxyImplementation('Ethereum', '0xproxy');

    expect(result).toBe(IMPL_ADDR.toLowerCase());
    expect(mockedAxios.post).toHaveBeenCalledTimes(3);
  });

  test('returns null when all slots are zero', async () => {
    mockedGetRpcUrl.mockReturnValue('https://rpc.example.com');
    mockedAxios.post.mockResolvedValue({ data: { result: ZERO_SLOT } });

    const result = await resolveProxyImplementation('Ethereum', '0xproxy');

    expect(result).toBeNull();
  });

  test('returns null when RPC call throws', async () => {
    mockedGetRpcUrl.mockReturnValue('https://rpc.example.com');
    mockedAxios.post.mockRejectedValue(new Error('connection refused'));

    const result = await resolveProxyImplementation('Ethereum', '0xproxy');

    expect(result).toBeNull();
  });

  test('sends correct eth_getStorageAt parameters', async () => {
    mockedGetRpcUrl.mockReturnValue('https://rpc.example.com');
    mockedAxios.post.mockResolvedValueOnce({ data: { result: IMPL_SLOT_VALUE } });

    await resolveProxyImplementation('Ethereum', '0xproxy');

    expect(mockedAxios.post).toHaveBeenCalledWith(
      'https://rpc.example.com',
      expect.objectContaining({
        method: 'eth_getStorageAt',
        params: expect.arrayContaining(['0xproxy', 'latest']),
      }),
    );
  });
});
