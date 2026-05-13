export interface PoolAccessInfo {
  /** Whether KYC or accreditation is required to enter (deposit into) the pool. null = unknown. */
  kycRequiredForEntry: boolean | null;
  /** Whether KYC or accreditation is required to exit (withdraw from) the pool. null = unknown. */
  kycRequiredForExit: boolean | null;
  /** Whether a DEX swap can be used as an alternative exit path. null = unknown. */
  canSwapToExit: boolean | null;
  /** Whether the pool allows immediate withdrawal without a lock or cooldown period. null = unknown. */
  isLiquid: boolean | null;
  /** Human-readable description of any lock or cooldown period, if present. */
  lockupDescription: string | null;
}
