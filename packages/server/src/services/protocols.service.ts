import { AuditInfo } from '../shared';
import { RawProtocol } from '../api/defillama.protocols.api.service';
import { baseSlug } from '../utils/slug.utils';

export type ProtocolAuditMap = Map<string, AuditInfo>;

export function buildProtocolAuditMap(protocols: RawProtocol[]): ProtocolAuditMap {
  const map: ProtocolAuditMap = new Map();

  for (const p of protocols) {
    if (!p.slug) continue;
    const info: AuditInfo = { audits: parseInt(p.audits ?? '0', 10) || 0, auditLinks: p.audit_links ?? [] };
    map.set(p.slug, info);

    const base = baseSlug(p.slug);
    if (base !== p.slug && !map.has(base)) {
      map.set(base, info);
    }
  }

  return map;
}

/** Return audit info for a given project slug, or null if not found. */
export function matchAuditInfo(projectSlug: string, protocolMap: ProtocolAuditMap): AuditInfo | null {
  if (protocolMap.has(projectSlug)) return protocolMap.get(projectSlug)!;

  const base = baseSlug(projectSlug);
  if (base !== projectSlug && protocolMap.has(base)) return protocolMap.get(base)!;

  return null;
}
