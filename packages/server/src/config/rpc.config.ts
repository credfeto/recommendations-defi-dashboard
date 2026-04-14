/**
 * RPC endpoint configuration.
 *
 * Set these environment variables to enable proxy implementation resolution
 * for each supported chain:
 *
 *   RPC_ETHEREUM   - Ethereum mainnet (chain ID 1)
 *   RPC_ARBITRUM   - Arbitrum One (chain ID 42161)
 *   RPC_BASE       - Base (chain ID 8453)
 *   RPC_BSC        - BNB Smart Chain (chain ID 56)
 */
const RPC_ENV: Record<string, string> = {
  Ethereum: 'RPC_ETHEREUM',
  Arbitrum: 'RPC_ARBITRUM',
  Base: 'RPC_BASE',
  BSC: 'RPC_BSC',
};

/** Returns the configured RPC URL for a chain, or null if not set. */
export function getRpcUrl(chain: string): string | null {
  const envKey = RPC_ENV[chain];
  if (!envKey) return null;
  return process.env[envKey] ?? null;
}

/** Chain names that have RPC support configured. */
export function getSupportedRpcChains(): string[] {
  return Object.keys(RPC_ENV).filter((chain) => {
    const envKey = RPC_ENV[chain];
    return !!envKey && !!process.env[envKey];
  });
}
