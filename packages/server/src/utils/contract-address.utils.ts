/** Returns true if the given string looks like an Ethereum-style hex address. */
function isContractAddress(value: string): boolean {
  return /^0x[0-9a-fA-F]{40}$/.test(value);
}

/**
 * Builds the de-duplicated list of on-chain contract addresses for a pool.
 *
 * Sources (in priority order):
 * 1. `underlyingTokens` — the underlying asset contract addresses
 * 2. `rewardTokens` — reward token contract addresses
 * 3. `pool` — used directly when it is a 0x address (e.g. Pendle market contracts)
 */
export function buildContractAddresses(pool: Record<string, unknown>): string[] {
  const addresses = new Set<string>();

  const underlyingTokens = pool['underlyingTokens'];
  if (Array.isArray(underlyingTokens)) {
    for (const addr of underlyingTokens) {
      if (typeof addr === 'string' && isContractAddress(addr)) {
        addresses.add(addr.toLowerCase());
      }
    }
  }

  const rewardTokens = pool['rewardTokens'];
  if (Array.isArray(rewardTokens)) {
    for (const addr of rewardTokens) {
      if (typeof addr === 'string' && isContractAddress(addr)) {
        addresses.add(addr.toLowerCase());
      }
    }
  }

  const poolId = pool['pool'];
  if (typeof poolId === 'string' && isContractAddress(poolId)) {
    addresses.add(poolId.toLowerCase());
  }

  return Array.from(addresses);
}
