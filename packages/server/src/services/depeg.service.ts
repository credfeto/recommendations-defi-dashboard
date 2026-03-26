import { DepegAlert } from '@shared';
import { CoinGeckoStablecoin } from '../api/coingecko.stablecoins.api.service';

/** Assumed peg for all USD stablecoins */
const USD_PEG = 1.0;

/** Deviations above these thresholds trigger alerts */
const WARN_THRESHOLD = 0.005; // 0.5%
const CRITICAL_THRESHOLD = 0.02; // 2%

/**
 * Builds a normalised symbol → current price map from CoinGecko stablecoin data.
 * Symbols are lowercased for case-insensitive matching.
 */
export function buildStablecoinPriceMap(coins: CoinGeckoStablecoin[]): Map<string, number> {
  const map = new Map<string, number>();
  for (const coin of coins) {
    if (coin.current_price != null) {
      map.set(coin.symbol.toLowerCase(), coin.current_price);
    }
  }
  return map;
}

/**
 * Parses a pool symbol string into individual token symbols.
 * Handles separators: -, /, +, space.
 * e.g. "USR-USDC" → ["USR", "USDC"], "crvUSD" → ["crvUSD"]
 */
export function parsePoolSymbols(poolSymbol: string): string[] {
  return poolSymbol
    .split(/[-/+\s]+/)
    .map((s) => s.trim())
    .filter(Boolean);
}

/**
 * Checks whether any tokens in a pool symbol are depegging.
 * Only runs for stablecoin-flagged pools.
 * Returns a DepegAlert for each token whose price deviates from $1 beyond thresholds.
 */
export function checkDepeg(poolSymbol: string, priceMap: Map<string, number>): DepegAlert[] {
  const tokens = parsePoolSymbols(poolSymbol);
  const alerts: DepegAlert[] = [];
  const seen = new Set<string>();

  for (const token of tokens) {
    const key = token.toLowerCase();
    if (seen.has(key)) continue;
    seen.add(key);

    const price = priceMap.get(key);
    if (price == null) continue;

    const deviation = (price - USD_PEG) / USD_PEG;
    const absDeviation = Math.abs(deviation);

    if (absDeviation >= WARN_THRESHOLD) {
      alerts.push({
        symbol: token,
        currentPrice: price,
        pegPrice: USD_PEG,
        deviation,
        severity: absDeviation >= CRITICAL_THRESHOLD ? 'critical' : 'warning',
      });
    }
  }

  return alerts;
}
