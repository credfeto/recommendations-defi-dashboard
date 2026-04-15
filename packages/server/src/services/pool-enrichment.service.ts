import { getCachedOrFetch } from '../db/cache.db';
import { defiLlamaPoolsApiService } from '../api/defillama.pools.api.service';
import { defiLlamaHacksApiService } from '../api/defillama.hacks.api.service';
import { pendleMarketsApiService } from '../api/pendle.markets.api.service';
import { coinGeckoStablecoinsApiService } from '../api/coingecko.stablecoins.api.service';
import { defiLlamaProtocolsApiService } from '../api/defillama.protocols.api.service';
import { buildHackMap } from './hacks.service';
import { buildProtocolAuditMap } from './protocols.service';
import { buildStablecoinPriceMap, buildStablecoinAddressMap } from './depeg.service';
import { CACHE_KEYS } from './cache-warmer.service';

export const getAllPools = async () => {
  const [llamaPools, pendlePools] = await Promise.all([
    getCachedOrFetch(CACHE_KEYS.LLAMA_POOLS, () => defiLlamaPoolsApiService.fetchPools()),
    getCachedOrFetch(CACHE_KEYS.PENDLE_POOLS, () => pendleMarketsApiService.fetchMarkets()),
  ]);
  return [...llamaPools, ...pendlePools];
};

export const getHackMap = async () => {
  try {
    const hacks = await getCachedOrFetch(CACHE_KEYS.HACKS, () => defiLlamaHacksApiService.fetchHacks());
    return buildHackMap(hacks);
  } catch {
    return new Map();
  }
};

export const getProtocolAuditMap = async () => {
  try {
    const protocols = await getCachedOrFetch(CACHE_KEYS.PROTOCOLS, () =>
      defiLlamaProtocolsApiService.fetchProtocols(),
    );
    return buildProtocolAuditMap(protocols);
  } catch {
    return new Map();
  }
};

export const getStablecoinPriceMap = async () => {
  try {
    const coins = await getCachedOrFetch(CACHE_KEYS.STABLECOINS, () =>
      coinGeckoStablecoinsApiService.fetchStablecoins(),
    );
    return buildStablecoinPriceMap(coins);
  } catch {
    return new Map<string, number>();
  }
};

export const getStablecoinAddressMap = async () => {
  try {
    const [coins, coinList] = await Promise.all([
      getCachedOrFetch(CACHE_KEYS.STABLECOINS, () => coinGeckoStablecoinsApiService.fetchStablecoins()),
      getCachedOrFetch(CACHE_KEYS.COIN_LIST, () => coinGeckoStablecoinsApiService.fetchCoinList()),
    ]);
    return buildStablecoinAddressMap(coins, coinList);
  } catch {
    return new Map<string, string>();
  }
};
