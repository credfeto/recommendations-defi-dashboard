import axios from 'axios';
import { HackInfo } from '@shared';

const HACKS_URL = 'https://api.llama.fi/hacks';

interface RawHack {
  date: number;
  name: string;
  classification: string;
  technique: string;
  amount: number;
  source: string;
  parentProtocolId?: string;
}

/** Normalise a display name or slug into a comparable slug */
function toSlug(str: string): string {
  return str
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');
}

/** Strip common version suffixes so "aave-v3" base-matches "aave" */
function baseSlug(slug: string): string {
  return slug.replace(/-v\d+.*$/, '');
}

function buildHackMap(hacks: RawHack[]): Map<string, HackInfo[]> {
  const map = new Map<string, HackInfo[]>();

  const add = (key: string, info: HackInfo) => {
    if (!key) return;
    if (!map.has(key)) map.set(key, []);
    map.get(key)!.push(info);
  };

  for (const h of hacks) {
    const info: HackInfo = {
      name: h.name,
      date: h.date,
      amountUsd: h.amount,
      classification: h.classification ?? 'Unknown',
      technique: h.technique ?? 'Unknown',
      source: h.source,
    };

    // Index by normalised name (e.g. "Aave V2" → "aave-v2") and its base ("aave")
    const nameSlug = toSlug(h.name);
    add(nameSlug, info);
    add(baseSlug(nameSlug), info);

    // Index by parent protocol slug (e.g. "parent#venus-finance" → "venus-finance")
    if (h.parentProtocolId) {
      const parentSlug = h.parentProtocolId.replace(/^parent#/, '');
      add(parentSlug, info);
      add(baseSlug(parentSlug), info);
    }
  }

  return map;
}

/** Return deduplicated hacks matching the given project slug. */
export function matchHacks(projectSlug: string, hackMap: Map<string, HackInfo[]>): HackInfo[] {
  const seen = new Map<string, HackInfo>(); // key: name+date

  const collect = (key: string) => {
    const items = hackMap.get(key);
    if (items) {
      for (const h of items) {
        seen.set(`${h.name}|${h.date}`, h);
      }
    }
  };

  collect(projectSlug);
  collect(baseSlug(projectSlug));

  // Also check every map key that is a prefix of this project slug
  // e.g. project "compound-v3" will match hack key "compound"
  for (const key of hackMap.keys()) {
    if (projectSlug === key || projectSlug.startsWith(key + '-')) {
      collect(key);
    }
  }

  return Array.from(seen.values()).sort((a, b) => b.date - a.date);
}

export async function fetchHacks(): Promise<RawHack[]> {
  const response = await axios.get<RawHack[]>(HACKS_URL);
  return response.data ?? [];
}

export { buildHackMap };
export type { RawHack };
