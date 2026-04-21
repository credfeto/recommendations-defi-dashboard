export interface DepegAlert {
  symbol: string;
  currentPrice: number;
  pegPrice: number;
  /** Deviation from peg as a fraction, e.g. -0.672 means -67.2% below peg */
  deviation: number;
  severity: 'warning' | 'critical';
}
