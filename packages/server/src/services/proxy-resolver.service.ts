import axios from 'axios';
import { getRpcUrl } from '../config/rpc.config';

/**
 * EIP-1967 implementation slot (keccak256("eip1967.proxy.implementation") - 1)
 * Most modern upgradeable proxies (OpenZeppelin TransparentUpgradeableProxy,
 * UUPS) store the implementation address here.
 */
const SLOT_EIP1967 = '0x360894a13ba1a3210667c828492db98dca3e2076cc3735a920a3ca505d382bbc';

/**
 * EIP-1967 beacon slot (keccak256("eip1967.proxy.beacon") - 1)
 * Beacon proxies store the beacon address here; the beacon holds the impl.
 */
const SLOT_EIP1967_BEACON = '0xa3f0ad74e5423aebfd80d3ef4346578335a9a72aeaee59ff6cb3582b35133d50';

/**
 * OpenZeppelin legacy slot (keccak256("org.zeppelinos.proxy.implementation"))
 * Used by older OZ proxy contracts before EIP-1967.
 */
const SLOT_OZ_LEGACY = '0x7050c9e0f4ca769c69bd3a8ef740bc37934f8e2c036e5a723fd8ee048ed3f8c3';

const ZERO_RESULT = '0x' + '0'.repeat(64);

async function getStorageAt(rpcUrl: string, address: string, slot: string): Promise<string | null> {
  try {
    const response = await axios.post<{ result?: string }>(rpcUrl, {
      jsonrpc: '2.0',
      method: 'eth_getStorageAt',
      params: [address, slot, 'latest'],
      id: 1,
    });
    return response.data?.result ?? null;
  } catch {
    return null;
  }
}

function extractAddress(storageValue: string): string | null {
  if (!storageValue || storageValue === ZERO_RESULT) return null;
  // Storage values are 32 bytes; address occupies the last 20 bytes
  const hex = storageValue.replace('0x', '');
  if (hex.length !== 64) return null;
  const addr = '0x' + hex.slice(-40);
  if (addr === '0x' + '0'.repeat(40)) return null;
  return addr.toLowerCase();
}

/**
 * Attempt to resolve the implementation address of an upgradeable proxy.
 *
 * Tries slots in order: EIP-1967 → EIP-1967 beacon → OZ legacy.
 * Returns the implementation address (lowercase) or null if:
 *  - no RPC is configured for the chain, or
 *  - no known proxy slot contains a non-zero address.
 */
export async function resolveProxyImplementation(chain: string, proxyAddress: string): Promise<string | null> {
  const rpcUrl = getRpcUrl(chain);
  if (!rpcUrl) return null;

  for (const slot of [SLOT_EIP1967, SLOT_EIP1967_BEACON, SLOT_OZ_LEGACY]) {
    const value = await getStorageAt(rpcUrl, proxyAddress, slot);
    if (value) {
      const impl = extractAddress(value);
      if (impl) return impl;
    }
  }

  return null;
}
