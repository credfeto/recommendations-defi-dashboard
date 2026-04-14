import { getContractSecurityForAddresses } from './contract-security.service';

jest.mock('../api/goplus.api.service');
jest.mock('./proxy-resolver.service');
jest.mock('../db/cache.db');

import { goPlusApiService } from '../api/goplus.api.service';
import { resolveProxyImplementation } from './proxy-resolver.service';
import {
  getContractSecurity,
  setContractSecurity,
  getContractSecurityChildren,
  CONTRACT_SECURITY_TTL_MS,
} from '../db/cache.db';

const mockFetch = goPlusApiService.fetchTokenSecurity as jest.MockedFunction<
  typeof goPlusApiService.fetchTokenSecurity
>;
const mockResolveProxy = resolveProxyImplementation as jest.MockedFunction<typeof resolveProxyImplementation>;
const mockGetSecurity = getContractSecurity as jest.MockedFunction<typeof getContractSecurity>;
const mockSetSecurity = setContractSecurity as jest.MockedFunction<typeof setContractSecurity>;
const mockGetChildren = getContractSecurityChildren as jest.MockedFunction<typeof getContractSecurityChildren>;

const ADDR = '0xae7ab96520de3a18e5e111b5eaab095312d7fe84';
const IMPL_ADDR = '0xdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef';

const freshCachedRow = {
  chain: 'Ethereum',
  address: ADDR,
  parentAddress: null,
  isOpenSource: 1,
  isHoneypot: 0,
  isProxy: 0,
  buyTax: 0,
  sellTax: 0,
  transferTax: 0,
  cannotBuy: 0,
  honeypotWithSameCreator: 0,
  tokenName: 'stETH',
  tokenSymbol: 'stETH',
  checkedAt: Date.now(),
};

beforeEach(() => jest.clearAllMocks());

describe('getContractSecurityForAddresses', () => {
  test('returns empty array for empty addresses', async () => {
    const result = await getContractSecurityForAddresses('Ethereum', []);
    expect(result).toEqual([]);
    expect(mockFetch).not.toHaveBeenCalled();
  });

  test('returns cached result when fresh', async () => {
    mockGetSecurity.mockReturnValue(freshCachedRow);
    mockGetChildren.mockReturnValue([]);

    const result = await getContractSecurityForAddresses('Ethereum', [ADDR]);

    expect(result).toHaveLength(1);
    expect(result[0].address).toBe(ADDR);
    expect(mockFetch).not.toHaveBeenCalled();
  });

  test('fetches from GoPlus when cache is stale', async () => {
    mockGetSecurity.mockReturnValue({ ...freshCachedRow, checkedAt: Date.now() - CONTRACT_SECURITY_TTL_MS - 1 });
    mockFetch.mockResolvedValueOnce(
      new Map([[ADDR, { is_open_source: '1', is_honeypot: '0', is_proxy: '0', token_symbol: 'stETH' }]]),
    );
    mockResolveProxy.mockResolvedValue(null);

    await getContractSecurityForAddresses('Ethereum', [ADDR]);

    expect(mockFetch).toHaveBeenCalledWith('Ethereum', [ADDR]);
    expect(mockSetSecurity).toHaveBeenCalled();
  });

  test('fetches from GoPlus when no cache entry', async () => {
    mockGetSecurity.mockReturnValue(null);
    mockFetch.mockResolvedValueOnce(
      new Map([[ADDR, { is_open_source: '1', is_honeypot: '0', is_proxy: '0', token_symbol: 'stETH' }]]),
    );
    mockResolveProxy.mockResolvedValue(null);

    const result = await getContractSecurityForAddresses('Ethereum', [ADDR]);

    expect(result).toHaveLength(1);
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test('resolves proxy impl and fetches its security when is_proxy=1', async () => {
    mockGetSecurity.mockReturnValue(null);
    mockFetch
      .mockResolvedValueOnce(new Map([[ADDR, { is_proxy: '1', token_symbol: 'stETH' }]]))
      .mockResolvedValueOnce(new Map([[IMPL_ADDR, { is_proxy: '0', token_symbol: 'stETH impl' }]]));
    mockResolveProxy.mockResolvedValueOnce(IMPL_ADDR);

    const result = await getContractSecurityForAddresses('Ethereum', [ADDR]);

    expect(result).toHaveLength(2);
    expect(result.find((r) => r.address === IMPL_ADDR.toLowerCase())).toBeDefined();
    expect(result.find((r) => r.address === IMPL_ADDR.toLowerCase())?.parentAddress).toBe(ADDR.toLowerCase());
  });

  test('does not call proxy resolver when is_proxy=0', async () => {
    mockGetSecurity.mockReturnValue(null);
    mockFetch.mockResolvedValueOnce(new Map([[ADDR, { is_proxy: '0' }]]));

    await getContractSecurityForAddresses('Ethereum', [ADDR]);

    expect(mockResolveProxy).not.toHaveBeenCalled();
  });

  test('skips proxy impl if resolver returns null', async () => {
    mockGetSecurity.mockReturnValue(null);
    mockFetch.mockResolvedValueOnce(new Map([[ADDR, { is_proxy: '1' }]]));
    mockResolveProxy.mockResolvedValueOnce(null);

    const result = await getContractSecurityForAddresses('Ethereum', [ADDR]);

    expect(result).toHaveLength(1);
  });

  test('also loads cached proxy child rows when parent is fresh', async () => {
    const implRow = { ...freshCachedRow, address: IMPL_ADDR, parentAddress: ADDR, isProxy: 0, checkedAt: Date.now() };
    mockGetSecurity.mockReturnValue({ ...freshCachedRow, isProxy: 1 });
    mockGetChildren.mockReturnValue([implRow]);

    const result = await getContractSecurityForAddresses('Ethereum', [ADDR]);

    expect(result).toHaveLength(2);
    expect(result.find((r) => r.address === IMPL_ADDR)).toBeDefined();
  });

  test('correctly parses numeric string fields', async () => {
    mockGetSecurity.mockReturnValue(null);
    mockFetch.mockResolvedValueOnce(
      new Map([[ADDR, { is_open_source: '1', is_honeypot: '0', buy_tax: '0.5', sell_tax: '' }]]),
    );
    mockResolveProxy.mockResolvedValue(null);

    const result = await getContractSecurityForAddresses('Ethereum', [ADDR]);

    expect(result[0].isOpenSource).toBe(1);
    expect(result[0].isHoneypot).toBe(0);
    expect(result[0].buyTax).toBe(0.5);
    expect(result[0].sellTax).toBeNull();
  });
});
