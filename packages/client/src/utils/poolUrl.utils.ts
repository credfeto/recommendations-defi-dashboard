const PENDLE_CHAIN_IDS: Record<string, number> = {
  ethereum: 1,
  arbitrum: 42161,
  base: 8453,
  bsc: 56,
};

/**
 * Returns a direct URL to the pool/protocol page for the given pool.
 * - DefiLlama pools link to the specific yield entry on defillama.com
 * - Pendle pools link to the market page on app.pendle.finance
 */
export function getPoolUrl(pool: { dataSource: string; pool: string; chain: string }): string | null {
  if (pool.dataSource === 'pendle') {
    const chainId = PENDLE_CHAIN_IDS[pool.chain.toLowerCase()];
    if (!chainId) return null;
    return `https://app.pendle.finance/trade/markets/${chainId}/${pool.pool}/pt`;
  }

  if (pool.dataSource === 'defillama') {
    return `https://defillama.com/yields?pool=${pool.pool}`;
  }

  return null;
}
