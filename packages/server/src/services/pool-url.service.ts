const PENDLE_CHAIN_IDS: Record<string, number> = { ethereum: 1, arbitrum: 42161, base: 8453, bsc: 56 };

/**
 * Returns a direct URL to the pool/protocol page for the given pool, or null
 * if a URL cannot be determined.
 * - DefiLlama pools: defillama.com/yields?pool={uuid}
 * - Pendle pools:    app.pendle.finance/trade/markets/{chainId}/{address}/pt
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
