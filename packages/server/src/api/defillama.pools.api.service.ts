import axios from 'axios';

const LLAMA_POOLS_URL = 'https://yields.llama.fi/pools';

/**
 * Fetch all yield pools from DefiLlama.
 * Pendle pools are excluded — the Pendle API is the authoritative source for those.
 */
export async function fetchDefiLlamaPools(): Promise<any[]> {
  const response = await axios.get(LLAMA_POOLS_URL);
  return (response.data.data ?? [])
    .filter((p: any) => p.project !== 'pendle')
    .map((p: any) => ({ ...p, dataSource: 'defillama' }));
}
