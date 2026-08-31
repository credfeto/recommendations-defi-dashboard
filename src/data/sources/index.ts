--- a/src/data/sources/index.ts
+++ b/src/data/sources/index.ts
@@ -1,5 +1,6 @@
 export { defillamaSource } from "./defillama";
 export { rektSource } from "./rekt";
+export { immunefiSource } from "./immunefi";
 
-export const sources = [defillamaSource, rektSource] as const;
+export const sources = [defillamaSource, rektSource, immunefiSource] as const;
