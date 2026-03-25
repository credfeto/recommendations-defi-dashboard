/**
 * Example: How to Add New Pool Types
 *
 * This file demonstrates how easy it is to extend the pool system.
 * Copy these examples to src/types/poolTypes.ts in the POOL_TYPES object.
 */

// Example 1: Bitcoin-based pools
export const BITCOIN_POOL_TYPE = {
  id: 'BITCOIN',
  name: 'Bitcoin-Based Pools',
  description: 'Pools featuring BTC or wrapped Bitcoin tokens',
  predicate: (pool: any) => {
    const btcSymbols = ['WBTC', 'BTC', 'BTCB', 'RENBTC', 'SBTC', 'XBTC'];
    return btcSymbols.some((s) => pool.symbol.toUpperCase().includes(s));
  },
};

// Example 2: Polygon ecosystem pools
export const POLYGON_POOL_TYPE = {
  id: 'POLYGON',
  name: 'Polygon Ecosystem',
  description: 'Pools on Polygon chain with MATIC and ecosystem tokens',
  predicate: (pool: any) => {
    return pool.chain === 'Polygon' || pool.symbol.toUpperCase().includes('MATIC');
  },
};

// Example 3: Ultra-low APY (stable/conservative)
export const CONSERVATIVE_POOL_TYPE = {
  id: 'CONSERVATIVE',
  name: 'Conservative Pools (0-2% APY)',
  description: 'Low-risk pools with minimal APY',
  predicate: (pool: any) => pool.apy >= 0 && pool.apy <= 2,
};

// Example 4: Single-sided staking
export const SINGLE_SIDED_POOL_TYPE = {
  id: 'SINGLE_SIDED',
  name: 'Single-Sided Staking',
  description: 'Pools that accept single-sided deposits',
  predicate: (pool: any) => {
    // You would need the API to provide this data
    return pool.exposure === 'single';
  },
};

// Example 5: High TVL for risk-averse investors
export const MEGA_CAP_POOL_TYPE = {
  id: 'MEGA_CAP',
  name: 'Mega Cap Pools (>$500M TVL)',
  description: 'Ultra-large pools with maximum liquidity',
  predicate: (pool: any) => pool.tvlUsd > 500_000_000,
};

// Example 6: Liquidity mining pools
export const LIQUIDITY_MINING_POOL_TYPE = {
  id: 'LIQUIDITY_MINING',
  name: 'Liquidity Mining Programs',
  description: 'Pools with additional reward tokens',
  predicate: (pool: any) => {
    return pool.apyReward !== null && pool.apyReward > 0;
  },
};

// Example 7: Derivative trading pools
export const DERIVATIVES_POOL_TYPE = {
  id: 'DERIVATIVES',
  name: 'Derivative Trading Pools',
  description: 'Pools for leveraged and perpetual trading',
  predicate: (pool: any) => {
    const derivativeProjects = ['AAVE', 'COMPOUND', 'DYDX', 'PERP'];
    return derivativeProjects.some((p) => pool.project.toUpperCase().includes(p));
  },
};

// Example 8: Cross-chain bridges
export const CROSS_CHAIN_POOL_TYPE = {
  id: 'CROSS_CHAIN',
  name: 'Cross-Chain Bridge Pools',
  description: 'Pools that facilitate cross-chain liquidity',
  predicate: (pool: any) => {
    const bridges = ['BRIDGE', 'WRAP', 'CROSS'];
    return bridges.some((b) => pool.project.toUpperCase().includes(b));
  },
};

/**
 * TO ADD THESE TO THE SYSTEM:
 *
 * 1. Open src/types/poolTypes.ts
 * 2. Add to the POOL_TYPES object:
 *
 *    export const POOL_TYPES: Record<string, PoolTypeConfig> = {
 *      ETH: { ... },
 *      STABLES: { ... },
 *      // Add your new types here:
 *      BITCOIN: BITCOIN_POOL_TYPE,
 *      POLYGON: POLYGON_POOL_TYPE,
 *      // etc.
 *    };
 *
 * 3. The system automatically creates:
 *    - API endpoint: GET /api/pools/BITCOIN
 *    - UI button with pool count
 *    - Data fetching for the new type
 *
 * That's it! No other changes needed.
 */
