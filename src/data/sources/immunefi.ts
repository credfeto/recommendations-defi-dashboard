--- /dev/null
+++ b/src/data/sources/immunefi.ts
@@ -0,0 +1,82 @@
+import type { Incident } from '../types';
+
+const IMMUNEFI_API = 'https://api.immunefi.com/v1/incidents';
+
+interface ImmunefiIncident {
+  id: string;
+  protocol: string;
+  title: string;
+  loss_usd: number | null;
+  date: string;
+  url: string;
+  chain?: string;
+  category?: string;
+}
+
+export async function fetchImmunefiIncidents(): Promise<Incident[]> {
+  const response = await fetch(IMMUNEFI_API, {
+    headers: {
+      Accept: 'application/json',
+    },
+  });
+
+  if (!response.ok) {
+    throw new Error(`Immunefi API error: ${response.status} ${response.statusText}`);
+  }
+
+  const data: ImmunefiIncident[] = await response.json();
+
+  return data.map((item) => ({
+    id: `immunefi-${item.id}`,
+    source: 'immunefi',
+    protocol: item.protocol,
+    title: item.title,
+    lossUsd: item.loss_usd ?? 0,
+    date: new Date(item.date),
+    url: item.url,
+    chain: item.chain ?? null,
+    category: item.category ?? null,
+  }));
+}
+
+export function getImmunefiSourceMetadata() {
+  return {
+    name: 'Immunefi',
+    url: 'https://immunefi.com/explore/',
+    description:
+      'DeFi bug bounty platform incident reports and loss data for cross-referencing protocol security posture.',
+    updateFrequency: 'daily',
+    parentIssue: '#242',
+  };
+}
