--- a/src/data/merge.ts
+++ b/src/data/merge.ts
@@ -2,9 +2,11 @@ import { logger } from '../lib/logger';
+import { fetchImmunefiIncidents, ImmunefiIncident } from './sources/immunefi';
 import { DefiLlamaHack } from './sources/defillama';
 
-export async function collectIncidents(): Promise<DefiLlamaHack[]> {
+export async function collectIncidents(): Promise<DefiLlamaHack[]> {
   const [defillama, immunefi] = await Promise.all([
     fetchDefiLlamaHacks(),
     fetchImmunefiIncidents().catch(() => [] as ImmunefiIncident[]),
   ]);
 
-  return defillama;
+  return dedupeByProtocolAndDate(defillama, immunefi.map(fromImmunefi));
+}
+
+function fromImmunefi(incident: ImmunefiIncident): DefiLlamaHack {
+  return {
+    protocol: incident.protocol,
+    title: incident.title,
+    date: incident.date,
+    amountLostUsd: incident.amountLostUsd,
+    source: 'immunefi',
+  };
+}
+
+function dedupeByProtocolAndDate(a: DefiLlamaHack[], b: DefiLlamaHack[]): DefiLlamaHack[] {
+  const seen = new Set(a.map((h) => `${h.protocol.toLowerCase()}:${h.date}`));
+  const extras = b.filter((h) => !seen.has(`${h.protocol.toLowerCase()}:${h.date}`));
+  return [...a, ...extras];
+}
 