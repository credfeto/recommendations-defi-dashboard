import { ContractSecurityInfo } from '../shared';
import { goPlusApiService, GoPlusTokenResult } from '../api/goplus.api.service';
import { resolveProxyImplementation } from './proxy-resolver.service';
import {
  getContractSecurity,
  setContractSecurity,
  getContractSecurityChildren,
  CONTRACT_SECURITY_TTL_MS,
} from '../db/cache.db';

function parseNum(val: string | undefined): number | null {
  if (val === undefined || val === null || val === '') return null;
  const n = parseFloat(val);
  return isNaN(n) ? null : n;
}

function rawToInfo(
  chain: string,
  address: string,
  parentAddress: string | null,
  raw: GoPlusTokenResult,
): ContractSecurityInfo {
  return {
    chain,
    address: address.toLowerCase(),
    parentAddress,
    isOpenSource: parseNum(raw.is_open_source),
    isHoneypot: parseNum(raw.is_honeypot),
    isProxy: parseNum(raw.is_proxy),
    buyTax: parseNum(raw.buy_tax),
    sellTax: parseNum(raw.sell_tax),
    transferTax: parseNum(raw.transfer_tax),
    cannotBuy: parseNum(raw.cannot_buy),
    honeypotWithSameCreator: parseNum(raw.honeypot_with_same_creator),
    tokenName: raw.token_name ?? null,
    tokenSymbol: raw.token_symbol ?? null,
  };
}

/**
 * Returns security info for each address, plus proxy implementation rows.
 *
 * For each address:
 *  1. Return DB row if checked within the last 24h.
 *  2. Otherwise fetch from GoPlus, persist result.
 *  3. If the contract is an upgradeable proxy, resolve its implementation
 *     address via RPC, fetch + persist that too (with parentAddress set).
 *
 * Returns a flat list of ContractSecurityInfo covering all addresses and
 * their implementations.
 */
export async function getContractSecurityForAddresses(
  chain: string,
  addresses: string[],
): Promise<ContractSecurityInfo[]> {
  if (addresses.length === 0) return [];

  const now = Date.now();
  const results: ContractSecurityInfo[] = [];
  const staleAddresses: string[] = [];

  // Separate fresh cache hits from those needing a refresh
  for (const addr of addresses) {
    const cached = getContractSecurity(chain, addr);
    if (cached && now - cached.checkedAt < CONTRACT_SECURITY_TTL_MS) {
      const { checkedAt: _, ...info } = cached;
      results.push(info);
      // Also load any impl rows for this proxy
      if (info.isProxy === 1) {
        for (const child of getContractSecurityChildren(chain, addr)) {
          const { checkedAt: __, ...childInfo } = child;
          results.push(childInfo);
        }
      }
    } else {
      staleAddresses.push(addr);
    }
  }

  if (staleAddresses.length === 0) return results;

  // Batch-fetch from GoPlus for all stale addresses
  const goplusMap = await goPlusApiService.fetchTokenSecurity(chain, staleAddresses);

  for (const addr of staleAddresses) {
    const raw = goplusMap.get(addr.toLowerCase());
    if (!raw) continue;

    const info = rawToInfo(chain, addr, null, raw);
    setContractSecurity(info);
    results.push(info);

    // Resolve proxy implementation if needed
    if (info.isProxy === 1) {
      const implAddr = await resolveProxyImplementation(chain, addr);
      if (implAddr) {
        const implRaw = await fetchSingleAddress(chain, implAddr);
        if (implRaw) {
          const implInfo = rawToInfo(chain, implAddr, addr.toLowerCase(), implRaw);
          setContractSecurity(implInfo);
          results.push(implInfo);
        }
      }
    }
  }

  return results;
}

async function fetchSingleAddress(chain: string, address: string): Promise<GoPlusTokenResult | null> {
  const map = await goPlusApiService.fetchTokenSecurity(chain, [address]);
  return map.get(address.toLowerCase()) ?? null;
}
