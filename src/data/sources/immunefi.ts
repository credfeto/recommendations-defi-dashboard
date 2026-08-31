--- /dev/null
+++ b/src/data/sources/immunefi.ts
@@ -0,0 +1,82 @@
+import type { Incident, DataSource } from "../types";
+
+const IMMUNEFI_API = "https://api.immunefi.com/v1/incidents";
+
+interface ImmunefiIncident {
+  id: string;
+  protocol: string;
+  loss_usd: number;
+  date: string;
+  category: string;
+  url?: string;
+}
+
+export async function fetchImmunefiIncidents(): Promise<Incident[]> {
+  const response = await fetch(IMMUNEFI_API, {
+    headers: {
+      Accept: "application/json",
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
+    source: "immunefi",
+    protocol: item.protocol,
+    lossUsd: item.loss_usd,
+    date: new Date(item.date),
+    category: item.category,
+    url: item.url ?? `https://immunefi.com/explore/incident/${item.id}`,
+  }));
+}
+
+export const immunefiSource: DataSource = {
+  id: "immunefi",
+  name: "Immunefi",
+  description:
+    "DeFi incident reports and loss data from the leading bug bounty platform",
+  fetch: fetchImmunefiIncidents,
+  updateIntervalMs: 6 * 60 * 60 * 1000, // 6 hours
+};
