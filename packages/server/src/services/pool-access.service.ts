import { PoolAccessInfo } from '@shared/types/poolAccessInfo';

const KYC_PATTERNS = [
  /\binstitutional\b/i,
  /\baccredited\b/i,
  /\bkyc\b/i,
  /\bwhitelist/i,
  /\bqualified\b/i,
  /\bpermissioned\b/i,
];

const LOCKUP_PATTERNS: Array<{ pattern: RegExp; description: (match: RegExpMatchArray) => string }> = [
  {
    pattern: /(\d+)\s*days?\s+unstaking/i,
    description: (m) => `${m[1]}-day unstaking period`,
  },
  {
    pattern: /unstaking\s+cooldown[:\s]+(\d+(?:\.\d+)?)\s*days?/i,
    description: (m) => `${m[1]}-day unstaking cooldown`,
  },
  {
    pattern: /(\d+)\s*d\s+(?:unlock|withdrawal\s+cycle)/i,
    description: (m) => `${m[1]}-day withdrawal cycle`,
  },
  {
    pattern: /(\d+)\s*days?\s+(?:lock|lockup|locked)/i,
    description: (m) => `${m[1]}-day lockup`,
  },
  {
    pattern: /(\d+)\s*(?:day|d)\s+cooldown/i,
    description: (m) => `${m[1]}-day cooldown`,
  },
  {
    pattern: /maturity\s+\d/i,
    description: () => 'Fixed-term (held to maturity)',
  },
];

/** Projects that are known DEX AMMs where a secondary-market swap exit is always possible. */
const SWAP_EXIT_PROJECTS = new Set([
  'uniswap-v2',
  'uniswap-v3',
  'uniswap-v4',
  'curve',
  'balancer',
  'balancer-v2',
  'balancer-v3',
  'sushiswap',
  'pancakeswap',
  'aerodrome',
  'velodrome',
  'camelot',
  'ramses',
  'thena',
  'trader-joe',
  'quickswap',
  'orca',
  'raydium',
  'pendle',
]);

/**
 * Projects that are known to require KYC / accreditation for entry.
 * This supplements `poolMeta` parsing for protocols that gate all pools.
 */
const KYC_ENTRY_PROJECTS = new Set(['maple', 'maple-v2', 'centrifuge', 'credix', 'goldfinch']);

function hasKycMeta(poolMeta: string | null): boolean {
  if (!poolMeta) {
    return false;
  }
  return KYC_PATTERNS.some((p) => p.test(poolMeta));
}

function detectLockup(poolMeta: string | null): string | null {
  if (!poolMeta) {
    return null;
  }
  for (const { pattern, description } of LOCKUP_PATTERNS) {
    const match = poolMeta.match(pattern);
    if (match) {
      return description(match);
    }
  }
  return null;
}

export function derivePoolAccessInfo(
  project: string,
  poolMeta: string | null,
  exposure: string | null,
): PoolAccessInfo {
  const kycFromMeta = hasKycMeta(poolMeta);
  const kycFromProject = KYC_ENTRY_PROJECTS.has(project.toLowerCase());
  const kycRequired = kycFromMeta || kycFromProject;

  const lockupDescription = detectLockup(poolMeta);
  const hasLockup = lockupDescription !== null;

  const canSwapToExit = SWAP_EXIT_PROJECTS.has(project.toLowerCase()) ? true : null;

  // A pool is illiquid when there is an explicit lockup period.
  // null means we have no data to determine liquidity.
  const isLiquid = hasLockup ? false : null;

  return {
    kycRequiredForEntry: kycRequired ? true : null,
    kycRequiredForExit: kycRequired ? true : null,
    canSwapToExit,
    isLiquid,
    lockupDescription,
  };
}
