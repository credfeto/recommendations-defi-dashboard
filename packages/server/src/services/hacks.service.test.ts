import { buildHackMap, matchHacks } from './hacks.service';
import { RawHack } from '../api/defillama.hacks.api.service';

const makeHack = (overrides: Partial<RawHack> = {}): RawHack => ({
  name: 'Protocol X',
  date: 1_700_000_000,
  amount: 5_000_000,
  chains: ['Ethereum'],
  classification: 'Hack',
  technique: 'Flash Loan',
  source: 'https://example.com',
  parentProtocolId: undefined,
  ...overrides,
});

describe('buildHackMap', () => {
  test('returns empty map for empty input', () => {
    expect(buildHackMap([])).toEqual(new Map());
  });

  test('indexes hack by slugified name', () => {
    const map = buildHackMap([makeHack({ name: 'Protocol X' })]);
    expect(map.has('protocol-x')).toBe(true);
  });

  test('indexes hack by base slug (strips version suffix)', () => {
    const map = buildHackMap([makeHack({ name: 'Aave V3' })]);
    expect(map.has('aave-v3')).toBe(true);
    expect(map.has('aave')).toBe(true);
  });

  test('indexes hack by parentProtocolId (strips parent# prefix)', () => {
    const map = buildHackMap([makeHack({ parentProtocolId: 'parent#compound' })]);
    expect(map.has('compound')).toBe(true);
  });

  test('stores HackInfo with correct fields', () => {
    const hack = makeHack({
      name: 'Ronin',
      date: 1_648_000_000,
      amount: 620_000_000,
      classification: 'Exploit',
      technique: 'Private Key',
    });
    const map = buildHackMap([hack]);
    const info = map.get('ronin')![0];
    expect(info.name).toBe('Ronin');
    expect(info.date).toBe(1_648_000_000);
    expect(info.amountUsd).toBe(620_000_000);
    expect(info.classification).toBe('Exploit');
    expect(info.technique).toBe('Private Key');
  });

  test('defaults classification and technique to Unknown when missing', () => {
    const hack = makeHack({ classification: undefined, technique: undefined });
    const map = buildHackMap([hack]);
    const info = map.get('protocol-x')?.[0];
    expect(info).toBeDefined();
    expect(info!.classification).toBe('Unknown');
    expect(info!.technique).toBe('Unknown');
  });

  test('handles multiple hacks', () => {
    const hacks = [makeHack({ name: 'Alpha' }), makeHack({ name: 'Beta' })];
    const map = buildHackMap(hacks);
    expect(map.has('alpha')).toBe(true);
    expect(map.has('beta')).toBe(true);
  });
});

describe('matchHacks', () => {
  const hacks: RawHack[] = [
    makeHack({ name: 'Compound', date: 1_700_000_100 }),
    makeHack({ name: 'Compound V2', date: 1_700_000_200, parentProtocolId: 'parent#compound' }),
    makeHack({ name: 'Unrelated', date: 1_700_000_000 }),
  ];
  const hackMap = buildHackMap(hacks);

  test('returns empty array when no match', () => {
    expect(matchHacks('unknown-protocol', hackMap)).toHaveLength(0);
  });

  test('matches by exact slug', () => {
    const result = matchHacks('compound', hackMap);
    expect(result.length).toBeGreaterThan(0);
    expect(result.every((h) => h.name.toLowerCase().includes('compound'))).toBe(true);
  });

  test('matches versioned slug via prefix: "compound-v3" matches "compound" hack', () => {
    const result = matchHacks('compound-v3', hackMap);
    expect(result.some((h) => h.name === 'Compound')).toBe(true);
  });

  test('deduplicates hacks that appear under multiple keys', () => {
    const result = matchHacks('compound', hackMap);
    const uniqueKeys = new Set(result.map((h) => `${h.name}|${h.date}`));
    expect(uniqueKeys.size).toBe(result.length);
  });

  test('returns results sorted newest first', () => {
    const result = matchHacks('compound', hackMap);
    for (let i = 1; i < result.length; i++) {
      expect(result[i - 1].date).toBeGreaterThanOrEqual(result[i].date);
    }
  });
});
