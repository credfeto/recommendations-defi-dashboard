import { HackInfo } from '@shared';
import { RawHack } from '../api/defillama.hacks.api.service';
import { toSlug, baseSlug } from '../utils/slug.utils';

export type HackMap = Map<string, HackInfo[]>;

export function buildHackMap(hacks: RawHack[]): HackMap {
  const map: HackMap = new Map();

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

    const nameSlug = toSlug(h.name);
    add(nameSlug, info);
    add(baseSlug(nameSlug), info);

    if (h.parentProtocolId) {
      const parentSlug = h.parentProtocolId.replace(/^parent#/, '');
      add(parentSlug, info);
      add(baseSlug(parentSlug), info);
    }
  }

  return map;
}

/** Return deduplicated hacks matching the given project slug. */
export function matchHacks(projectSlug: string, hackMap: HackMap): HackInfo[] {
  const seen = new Map<string, HackInfo>();

  const collect = (key: string) => {
    const items = hackMap.get(key);
    if (items) {
      for (const h of items) seen.set(`${h.name}|${h.date}`, h);
    }
  };

  collect(projectSlug);
  collect(baseSlug(projectSlug));

  // Also match any hack key that is a prefix of this project slug
  // e.g. "compound-v3" matches hack key "compound"
  for (const key of hackMap.keys()) {
    if (projectSlug === key || projectSlug.startsWith(key + '-')) {
      collect(key);
    }
  }

  return Array.from(seen.values()).sort((a, b) => b.date - a.date);
}
