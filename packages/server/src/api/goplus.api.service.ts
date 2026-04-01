import axios from 'axios';

const GOPLUS_BASE = 'https://api.gopluslabs.io/api/v1/token_security';

export const CHAIN_NAME_TO_ID: Record<string, number> = {
  Ethereum: 1,
  Arbitrum: 42161,
  Base: 8453,
  BSC: 56,
};

export interface GoPlusTokenResult {
  is_open_source?: string;
  is_honeypot?: string;
  is_proxy?: string;
  buy_tax?: string;
  sell_tax?: string;
  transfer_tax?: string;
  cannot_buy?: string;
  honeypot_with_same_creator?: string;
  token_name?: string;
  token_symbol?: string;
}

export class GoPlusApiService {
  /**
   * Fetch security info for one or more contract addresses on a given chain.
   * Returns a map of lowercased address -> raw result.
   * Returns an empty map if the chain is unsupported or the request fails.
   */
  public async fetchTokenSecurity(
    chain: string,
    addresses: string[],
  ): Promise<Map<string, GoPlusTokenResult>> {
    const chainId = CHAIN_NAME_TO_ID[chain];
    if (!chainId || addresses.length === 0) return new Map();

    try {
      const joined = addresses.map((a) => a.toLowerCase()).join(',');
      const response = await axios.get<{ code: number; result: Record<string, GoPlusTokenResult> }>(
        `${GOPLUS_BASE}/${chainId}`,
        { params: { contract_addresses: joined } },
      );
      const result = response.data?.result ?? {};
      return new Map(Object.entries(result).map(([addr, info]) => [addr.toLowerCase(), info]));
    } catch {
      return new Map();
    }
  }
}

export const goPlusApiService = new GoPlusApiService();
