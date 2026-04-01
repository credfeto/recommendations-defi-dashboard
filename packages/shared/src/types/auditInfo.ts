export interface AuditInfo {
  /** 0 = none, 1 = single audit, 2 = multiple audits */
  audits: number;
  auditLinks: string[];
}
