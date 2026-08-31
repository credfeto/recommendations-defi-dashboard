--- a/src/defi_dashboard/security_indicators.py
+++ b/src/defi_dashboard/security_indicators.py
@@ -1,5 +1,7 @@
 """Security indicator aggregation for DeFi protocols."""
 from __future__ import annotations
 
+from defi_dashboard.data_sources.immunefi import has_active_bounty
+
 
 def compute_security_score(protocol_name: str, audit_count: int) -> float:
-    base = min(audit_count * 10.0, 50.0)
-    return base
+    base = min(audit_count * 10.0, 50.0)
+    bounty_bonus = 20.0 if has_active_bounty(protocol_name) else 0.0
+    return min(base + bounty_bonus, 100.0)
