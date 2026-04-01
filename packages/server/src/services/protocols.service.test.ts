import { buildProtocolAuditMap, matchAuditInfo } from './protocols.service';
import { RawProtocol } from '../api/defillama.protocols.api.service';

const makeProtocol = (overrides: Partial<RawProtocol> = {}): RawProtocol => ({
  slug: 'test-protocol',
  audits: '2',
  audit_links: ['https://example.com/audit'],
  ...overrides,
});

describe('buildProtocolAuditMap', () => {
  test('returns empty map for empty input', () => {
    expect(buildProtocolAuditMap([])).toEqual(new Map());
  });

  test('indexes protocol by slug', () => {
    const map = buildProtocolAuditMap([makeProtocol({ slug: 'aave-v3' })]);
    expect(map.has('aave-v3')).toBe(true);
  });

  test('also indexes by base slug (strips version suffix)', () => {
    const map = buildProtocolAuditMap([makeProtocol({ slug: 'aave-v3' })]);
    expect(map.has('aave')).toBe(true);
  });

  test('does not overwrite an existing base slug entry', () => {
    const protocols: RawProtocol[] = [
      makeProtocol({ slug: 'aave', audit_links: ['https://aave.com'] }),
      makeProtocol({ slug: 'aave-v3', audit_links: ['https://aave-v3.com'] }),
    ];
    const map = buildProtocolAuditMap(protocols);
    // 'aave' was inserted first and must not be overwritten
    expect(map.get('aave')!.auditLinks).toEqual(['https://aave.com']);
  });

  test('parses audits count as number', () => {
    const map = buildProtocolAuditMap([makeProtocol({ slug: 'lido', audits: '2' })]);
    expect(map.get('lido')!.audits).toBe(2);
  });

  test('defaults audits to 0 when missing', () => {
    const map = buildProtocolAuditMap([makeProtocol({ slug: 'new-proto', audits: undefined })]);
    expect(map.get('new-proto')!.audits).toBe(0);
  });

  test('defaults auditLinks to empty array when null', () => {
    const map = buildProtocolAuditMap([makeProtocol({ slug: 'no-audits', audit_links: null })]);
    expect(map.get('no-audits')!.auditLinks).toEqual([]);
  });

  test('stores audit links correctly', () => {
    const links = ['https://example.com/audit1', 'https://example.com/audit2'];
    const map = buildProtocolAuditMap([makeProtocol({ slug: 'multi-audit', audit_links: links })]);
    expect(map.get('multi-audit')!.auditLinks).toEqual(links);
  });

  test('skips protocols with empty slug', () => {
    const map = buildProtocolAuditMap([makeProtocol({ slug: '' })]);
    expect(map.size).toBe(0);
  });

  test('handles multiple protocols', () => {
    const protocols = [makeProtocol({ slug: 'alpha' }), makeProtocol({ slug: 'beta' })];
    const map = buildProtocolAuditMap(protocols);
    expect(map.has('alpha')).toBe(true);
    expect(map.has('beta')).toBe(true);
  });
});

describe('matchAuditInfo', () => {
  const protocols: RawProtocol[] = [
    makeProtocol({ slug: 'aave-v3', audits: '2', audit_links: ['https://aave.com/security'] }),
    makeProtocol({ slug: 'lido', audits: '2', audit_links: ['https://github.com/lidofinance/audits'] }),
    makeProtocol({ slug: 'curve-dex', audits: '2', audit_links: ['https://curve.fi/audits'] }),
  ];
  const auditMap = buildProtocolAuditMap(protocols);

  test('returns null when no match', () => {
    expect(matchAuditInfo('completely-unknown', auditMap)).toBeNull();
  });

  test('matches by exact slug', () => {
    const result = matchAuditInfo('aave-v3', auditMap);
    expect(result).not.toBeNull();
    expect(result!.audits).toBe(2);
    expect(result!.auditLinks).toContain('https://aave.com/security');
  });

  test('matches by base slug: "aave-v2" resolves via "aave" base slug', () => {
    // aave-v3 protocol also registers 'aave' base slug
    const result = matchAuditInfo('aave-v2', auditMap);
    expect(result).not.toBeNull();
  });

  test('returns full AuditInfo with audits and auditLinks', () => {
    const result = matchAuditInfo('lido', auditMap);
    expect(result).not.toBeNull();
    expect(typeof result!.audits).toBe('number');
    expect(Array.isArray(result!.auditLinks)).toBe(true);
  });
});
