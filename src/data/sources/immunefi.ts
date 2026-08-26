new file
--- /dev/null
+++ b/src/data/sources/immunefi.ts
@@ -0,0 +1,84 @@
+import { createHash } from 'crypto';
+import { logger } from '../../lib/logger';
+
+export interface ImmunefiIncident {
+  id: string;
+  protocol: string;
+  title: string;
+  date: string;
+  amountLostUsd: number;
+  category: string;
+  source: 'immunefi';
+  url: string;
+}
+
+const IMMUNEFI_EXPLORE_URL = 'https://immunefi.com/explore/';
+
+/**
+ * Fetches Immunefi incident data.
+ *
+ * Immunefi does not expose a fully documented public API, so this fetches
+ * the public explore page and extracts the embedded incident JSON. If the
+ * page structure changes or the fetch fails, an empty list is returned so
+ * the dashboard can fall back to other sources (e.g. DefiLlama hacks).
+ */
+export async function fetchImmunefiIncidents(): Promise<ImmunefiIncident[]> {
+  try {
+    const response = await fetch(IMMUNEFI_EXPLORE_URL, {
+      headers: {
+        accept: 'application/json, text/html',
+        'user-agent': 'recommendations-defi-dashboard/1.0 (+https://github.com/credfeto/recommendations-defi-dashboard)',
+      },
+    });
+
+    if (!response.ok) {
+      logger.warn(`Immunefi fetch returned status ${response.status}; skipping source`);
+      return [];
+    }
+
+    const body = await response.text();
+    return parseIncidents(body);
+  } catch (error) {
+    logger.warn(`Failed to fetch Immunefi incidents: ${error instanceof Error ? error.message : String(error)}`);
+    return [];
+  }
+}
+
+function parseIncidents(body: string): ImmunefiIncident[] {
+  // Immunefi embeds incident data as JSON inside __NEXT_DATA__ / script tags.
+  const matches = [...body.matchAll(/<script[^>]*id="__NEXT_DATA__"[^>]*>([\s\S]*?)<\/script>/g)];
+  if (matches.length === 0) {
+    return [];
+  }
+
+  try {
+    const data = JSON.parse(matches[0][1]) as ImmunefiRawPayload;
+    const incidents = data?.props?.pageProps?.incidents ?? [];
+    return incidents.map(toIncident).filter((i): i is ImmunefiIncident => i !== null);
+  } catch {
+    logger.warn('Failed to parse Immunefi embedded JSON payload');
+    return [];
+  }
+}
+
+function toIncident(raw: ImmunefiRawIncident): ImmunefiIncident | null {
+  if (!raw?.protocol?.name || !raw.title) {
+    return null;
+  }
+
+  return {
+    id: `immunefi-${createHash('sha1').update(`${raw.protocol.name}:${raw.title}:${raw.date}`).digest('hex').slice(0, 12)}`,
+    protocol: raw.protocol.name,
+    title: raw.title,
+    date: raw.date ?? '',
+    amountLostUsd: typeof raw.amount === 'number' ? raw.amount : 0,
+    category: raw.category ?? 'exploit',
+    source: 'immunefi',
+    url: raw.url ?? IMMUNEFI_EXPLORE_URL,
+  };
+}
+
+interface ImmunefiRawIncident {
+  protocol?: { name?: string };
+  title?: string;
+  date?: string;
+  amount?: number;
+  category?: string;
+  url?: string;
+}
+
+interface ImmunefiRawPayload {
+  props?: { pageProps?: { incidents?: ImmunefiRawIncident[] } };
+}
