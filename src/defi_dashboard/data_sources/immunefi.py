--- /dev/null
+++ b/src/defi_dashboard/data_sources/immunefi.py
@@ -0,0 +1,85 @@
+"""Immunefi bounty programme data source for protocol security metadata."""
+from __future__ import annotations
+
+import logging
+from typing import Any
+
+import requests
+
+logger = logging.getLogger(__name__)
+
+IMMUNEFI_EXPLORE_URL = "https://immunefi.com/explore/"
+_API_ENDPOINT = "https://api.immunefi.com/v1/bounties"
+
+
+def fetch_immunefi_bounties(timeout: int = 30) -> list[dict[str, Any]]:
+    """Fetch active Immunefi bounty programmes.
+
+    Returns a list of dicts with keys: protocol, max_payout_usd, assets_in_scope.
+    Falls back to an empty list on network or parsing errors.
+    """
+    try:
+        resp = requests.get(_API_ENDPOINT, timeout=timeout)
+        resp.raise_for_status()
+        data = resp.json()
+    except Exception:
+        logger.exception("Failed to fetch Immunefi bounty data")
+        return []
+
+    results: list[dict[str, Any]] = []
+    for item in data if isinstance(data, list) else data.get("bounties", []):
+        protocol = item.get("protocol") or item.get("project") or item.get("name")
+        max_payout = (
+            item.get("max_payout")
+            or item.get("maxBounty")
+            or item.get("max_bounty_usd")
+        )
+        assets = item.get("assets_in_scope") or item.get("assetsInScope") or []
+        if protocol is not None:
+            results.append(
+                {
+                    "protocol": str(protocol),
+                    "max_payout_usd": float(max_payout) if max_payout is not None else 0.0,
+                    "assets_in_scope": list(assets),
+                }
+            )
+    return results
+
+
+def get_protocol_bounty_info(protocol_name: str) -> dict[str, Any] | None:
+    """Return bounty metadata for a single protocol (case-insensitive match)."""
+    bounties = fetch_immunefi_bounties()
+    target = protocol_name.strip().lower()
+    for entry in bounties:
+        if entry["protocol"].lower() == target:
+            return entry
+    return None
+
+
+def has_active_bounty(protocol_name: str) -> bool:
+    """Check whether a protocol currently has an active Immunefi bounty."""
+    info = get_protocol_bounty_info(protocol_name)
+    return info is not None and info["max_payout_usd"] > 0
