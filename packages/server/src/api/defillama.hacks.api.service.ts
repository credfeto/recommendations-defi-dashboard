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

/** Fetch all recorded DeFi exploits from DefiLlama. */
export async function fetchDefiLlamaHacks(): Promise<RawHack[]> {
  const response = await axios.get<RawHack[]>(HACKS_URL);
  return response.data ?? [];
}
