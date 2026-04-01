import axios from 'axios';

const PROTOCOLS_URL = 'https://api.llama.fi/protocols';

export interface RawProtocol {
  slug: string;
  audits?: string | null;
  audit_links?: string[] | null;
}

export class DefiLlamaProtocolsApiService {
  /** Fetch all protocol metadata from DefiLlama. */
  public async fetchProtocols(): Promise<RawProtocol[]> {
    const response = await axios.get<RawProtocol[]>(PROTOCOLS_URL);
    return response.data ?? [];
  }
}

export const defiLlamaProtocolsApiService = new DefiLlamaProtocolsApiService();
