import axios from 'axios';

const HACKS_URL = 'https://api.llama.fi/hacks';

export interface RawHack {
  date: number;
  name: string;
  classification: string;
  technique: string;
  amount: number;
  source: string;
  parentProtocolId?: string;
}

export class DefiLlamaHacksApiService {
  /** Fetch all recorded DeFi exploits from DefiLlama. */
  public async fetchHacks(): Promise<RawHack[]> {
    const response = await axios.get<RawHack[]>(HACKS_URL);
    return response.data ?? [];
  }
}

export const defiLlamaHacksApiService = new DefiLlamaHacksApiService();
