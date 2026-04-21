export interface ContractSecurityInfo {
  chain: string;
  address: string;
  /** Set when this row is a proxy implementation; points to the proxy contract. */
  parentAddress: string | null;
  isOpenSource: number | null;
  isHoneypot: number | null;
  isProxy: number | null;
  buyTax: number | null;
  sellTax: number | null;
  transferTax: number | null;
  cannotBuy: number | null;
  honeypotWithSameCreator: number | null;
  tokenName: string | null;
  tokenSymbol: string | null;
}
