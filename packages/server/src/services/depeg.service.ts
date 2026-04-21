import { DepegAlert } from '@shared';
import { CoinGeckoStablecoin, CoinGeckoCoinPlatforms } from '../api/coingecko.stablecoins.api.service';

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
 * Builds a contract address → stablecoin symbol map by cross-referencing:
 *  - stablecoins: the price data we already have (id → symbol)
 *  - coinList:    the full coin list with on-chain addresses (id → platforms → address)
 *
 * Addresses and symbols are lowercased. Only stablecoin IDs present in the price
 * data are indexed, so the symbol can then be used to look up the price in priceMap.
 */
export function buildStablecoinAddressMap(
  stablecoins: CoinGeckoStablecoin[],
  coinList: CoinGeckoCoinPlatforms[],
): Map<string, string> {
  const idToSymbol = new Map<string, string>();
  for (const coin of stablecoins) {
    if (coin.current_price != null) {
      idToSymbol.set(coin.id, coin.symbol.toLowerCase());
    }
  }

  const addressMap = new Map<string, string>();
  for (const coin of coinList) {
    const symbol = idToSymbol.get(coin.id);
    if (symbol == null) continue;
    for (const address of Object.values(coin.platforms)) {
      if (address && address.startsWith('0x')) {
        addressMap.set(address.toLowerCase(), symbol);
      }
    }
  }
  return addressMap;
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

function makeAlert(symbol: string, price: number): DepegAlert | null {
  const deviation = (price - USD_PEG) / USD_PEG;
  const absDeviation = Math.abs(deviation);
  if (absDeviation < WARN_THRESHOLD) return null;
  return {
    symbol,
    currentPrice: price,
    pegPrice: USD_PEG,
    deviation,
    severity: absDeviation >= CRITICAL_THRESHOLD ? 'critical' : 'warning',
  };
}

/**
 * Checks a pool for stablecoin depeg risk using two sources:
 *  1. Pool symbol tokens looked up in the symbol → price map
 *  2. underlyingTokens contract addresses looked up in the address → symbol map,
 *     then prices resolved via the symbol → price map
 *
 * Applies to all pools — the price maps act as the natural filter
 * (only known stablecoins produce alerts).
 */
export function checkDepeg(
  poolSymbol: string,
  priceMap: Map<string, number>,
  underlyingTokens: string[] | null = null,
  addressMap: Map<string, string> | null = null,
): DepegAlert[] {
  const alerts: DepegAlert[] = [];
  const seen = new Set<string>(); // deduplicate by lowercase symbol key

  // --- Symbol-based check ---
  for (const token of parsePoolSymbols(poolSymbol)) {
    const key = token.toLowerCase();
    if (seen.has(key)) continue;
    seen.add(key);
    const price = priceMap.get(key);
    if (price == null) continue;
    const alert = makeAlert(token, price);
    if (alert) alerts.push(alert);
  }

  // --- Address-based check (underlyingTokens) ---
  if (addressMap && underlyingTokens) {
    for (const address of underlyingTokens) {
      const symbol = addressMap.get(address.toLowerCase());
      if (symbol == null) continue; // address is not a known stablecoin
      if (seen.has(symbol)) continue; // already reported via symbol check
      seen.add(symbol);
      const price = priceMap.get(symbol);
      if (price == null) continue;
      const alert = makeAlert(symbol.toUpperCase(), price);
      if (alert) alerts.push(alert);
    }
  }

  return alerts;
}
