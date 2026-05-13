import { derivePoolAccessInfo } from './pool-access.service';

describe('derivePoolAccessInfo', () => {
  describe('kycRequiredForEntry / kycRequiredForExit', () => {
    it('returns null when no KYC signals are present', () => {
      const result = derivePoolAccessInfo('lido', null);
      expect(result.kycRequiredForEntry).toBeNull();
      expect(result.kycRequiredForExit).toBeNull();
    });

    it('detects "Institutional" poolMeta as KYC required', () => {
      const result = derivePoolAccessInfo('blackrock-buidl', 'Institutional');
      expect(result.kycRequiredForEntry).toBe(true);
      expect(result.kycRequiredForExit).toBe(true);
    });

    it('detects "Institutional only" poolMeta as KYC required', () => {
      const result = derivePoolAccessInfo('multipli.fi', 'Institutional only');
      expect(result.kycRequiredForEntry).toBe(true);
      expect(result.kycRequiredForExit).toBe(true);
    });

    it('detects KYC via known KYC-required project (maple)', () => {
      const result = derivePoolAccessInfo('maple', null);
      expect(result.kycRequiredForEntry).toBe(true);
      expect(result.kycRequiredForExit).toBe(true);
    });

    it('detects KYC via known KYC-required project (goldfinch)', () => {
      const result = derivePoolAccessInfo('goldfinch', 'Senior Pool');
      expect(result.kycRequiredForEntry).toBe(true);
      expect(result.kycRequiredForExit).toBe(true);
    });

    it('is case-insensitive for poolMeta KYC detection', () => {
      const result = derivePoolAccessInfo('protocol', 'INSTITUTIONAL');
      expect(result.kycRequiredForEntry).toBe(true);
    });

    it('detects accredited investor requirement', () => {
      const result = derivePoolAccessInfo('protocol', 'Accredited investors only');
      expect(result.kycRequiredForEntry).toBe(true);
    });

    it('detects permissioned pool', () => {
      const result = derivePoolAccessInfo('protocol', 'Permissioned pool');
      expect(result.kycRequiredForEntry).toBe(true);
    });
  });

  describe('lockupDescription and isLiquid', () => {
    it('returns null lockup and null isLiquid for normal pool', () => {
      const result = derivePoolAccessInfo('lido', null);
      expect(result.lockupDescription).toBeNull();
      expect(result.isLiquid).toBeNull();
    });

    it('detects "7 days unstaking" as illiquid with description', () => {
      const result = derivePoolAccessInfo('ethena-usde', '7 days unstaking');
      expect(result.lockupDescription).toBe('7-day unstaking period');
      expect(result.isLiquid).toBe(false);
    });

    it('detects "30d unlock" as illiquid', () => {
      const result = derivePoolAccessInfo('usd-ai', '30d unlock');
      expect(result.lockupDescription).toBe('30-day withdrawal cycle');
      expect(result.isLiquid).toBe(false);
    });

    it('detects "Unstaking Cooldown: 15days" as illiquid', () => {
      const result = derivePoolAccessInfo('benqi-staked-avax', 'Unstaking Cooldown: 15days');
      expect(result.lockupDescription).toBe('15-day unstaking cooldown');
      expect(result.isLiquid).toBe(false);
    });

    it('detects "Unstaking Cooldown: 14.5days" as illiquid', () => {
      const result = derivePoolAccessInfo('sceptre-liquid', 'Unstaking Cooldown: 14.5days');
      expect(result.lockupDescription).toBe('14.5-day unstaking cooldown');
      expect(result.isLiquid).toBe(false);
    });

    it('detects "30d withdrawal cycle" as illiquid', () => {
      const result = derivePoolAccessInfo('gaib', '30d withdrawal cycle');
      expect(result.lockupDescription).toBe('30-day withdrawal cycle');
      expect(result.isLiquid).toBe(false);
    });

    it('detects "Maturity" in poolMeta as fixed-term', () => {
      const result = derivePoolAccessInfo('pendle', 'Maturity 27 Jun 2025');
      expect(result.lockupDescription).toBe('Fixed-term (held to maturity)');
      expect(result.isLiquid).toBe(false);
    });

    it('detects "7-day lockup" pattern', () => {
      const result = derivePoolAccessInfo('protocol', '7 day lockup');
      expect(result.lockupDescription).toBe('7-day lockup');
      expect(result.isLiquid).toBe(false);
    });
  });

  describe('canSwapToExit', () => {
    it('returns null for non-DEX protocols', () => {
      const result = derivePoolAccessInfo('lido', null);
      expect(result.canSwapToExit).toBeNull();
    });

    it('returns true for uniswap-v3', () => {
      const result = derivePoolAccessInfo('uniswap-v3', null);
      expect(result.canSwapToExit).toBe(true);
    });

    it('returns true for curve', () => {
      const result = derivePoolAccessInfo('curve', null);
      expect(result.canSwapToExit).toBe(true);
    });

    it('returns true for balancer-v2', () => {
      const result = derivePoolAccessInfo('balancer-v2', null);
      expect(result.canSwapToExit).toBe(true);
    });

    it('returns true for pendle (secondary market)', () => {
      const result = derivePoolAccessInfo('pendle', 'Maturity 27 Jun 2025');
      expect(result.canSwapToExit).toBe(true);
    });

    it('returns true for aerodrome', () => {
      const result = derivePoolAccessInfo('aerodrome', null);
      expect(result.canSwapToExit).toBe(true);
    });
  });
});
