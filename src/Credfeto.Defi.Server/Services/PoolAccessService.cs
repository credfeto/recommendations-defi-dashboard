using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Credfeto.Defi.Server.Models;

namespace Credfeto.Defi.Server.Services;

/// <summary>
///     Derives pool access and liquidity restriction information from pool metadata.
/// </summary>
internal static partial class PoolAccessService
{
    [GeneratedRegex(
        pattern: @"\binstitutional\b",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex InstitutionalPattern { get; }

    [GeneratedRegex(
        pattern: @"\baccredited\b",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex AccreditedPattern { get; }

    [GeneratedRegex(
        pattern: @"\bkyc\b",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex KycPattern { get; }

    [GeneratedRegex(
        pattern: @"\bwhitelist",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex WhitelistPattern { get; }

    [GeneratedRegex(
        pattern: @"\bqualified\b",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex QualifiedPattern { get; }

    [GeneratedRegex(
        pattern: @"\bpermissioned\b",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex PermissionedPattern { get; }

    [GeneratedRegex(
        pattern: @"(?<days>\d+)\s*days?\s+unstaking",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex LockupDaysUnstakingPattern { get; }

    [GeneratedRegex(
        pattern: @"unstaking\s+cooldown[:\s]+(?<days>\d+(?:\.\d+)?)\s*days?",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex LockupUnstakingCooldownPattern { get; }

    [GeneratedRegex(
        pattern: @"(?<days>\d+)\s*d\s+(?:unlock|withdrawal\s+cycle)",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex LockupWithdrawalCyclePattern { get; }

    [GeneratedRegex(
        pattern: @"(?<days>\d+)\s*days?\s+(?:lock|lockup|locked)",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex LockupDaysLockedPattern { get; }

    [GeneratedRegex(
        pattern: @"(?<days>\d+)\s*(?:day|d)\s+cooldown",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex LockupCooldownPattern { get; }

    [GeneratedRegex(
        pattern: @"maturity\s+\d",
        options: RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex MaturityPattern { get; }

    private static readonly HashSet<string> SwapExitProjects = new(StringComparer.OrdinalIgnoreCase)
    {
        "uniswap-v2",
        "uniswap-v3",
        "uniswap-v4",
        "curve",
        "balancer",
        "balancer-v2",
        "balancer-v3",
        "sushiswap",
        "pancakeswap",
        "aerodrome",
        "velodrome",
        "camelot",
        "ramses",
        "thena",
        "trader-joe",
        "quickswap",
        "orca",
        "raydium",
        "pendle",
    };

    private static readonly HashSet<string> KycEntryProjects = new(StringComparer.OrdinalIgnoreCase)
    {
        "maple",
        "maple-v2",
        "centrifuge",
        "credix",
        "goldfinch",
    };

    /// <summary>
    ///     Derives pool access info from the project identifier and pool metadata string.
    /// </summary>
    public static PoolAccessInfo DerivePoolAccessInfo(string project, string? poolMeta)
    {
        bool kycFromMeta = HasKycMeta(poolMeta);
        bool kycFromProject = KycEntryProjects.Contains(project);
        bool kycRequired = kycFromMeta || kycFromProject;

        string? lockupDescription = DetectLockup(poolMeta);
        bool hasLockup = lockupDescription is not null;

        bool? canSwapToExit = SwapExitProjects.Contains(project) ? true : null;
        bool? isLiquid = hasLockup ? false : null;

        return new PoolAccessInfo
        {
            KycRequiredForEntry = kycRequired ? true : null,
            KycRequiredForExit = kycRequired ? true : null,
            CanSwapToExit = canSwapToExit,
            IsLiquid = isLiquid,
            LockupDescription = lockupDescription,
        };
    }

    private static bool HasKycMeta(string? poolMeta)
    {
        return !string.IsNullOrEmpty(poolMeta)
            && (
                InstitutionalPattern.IsMatch(poolMeta)
                || AccreditedPattern.IsMatch(poolMeta)
                || KycPattern.IsMatch(poolMeta)
                || WhitelistPattern.IsMatch(poolMeta)
                || QualifiedPattern.IsMatch(poolMeta)
                || PermissionedPattern.IsMatch(poolMeta)
            );
    }

    private static string? DetectLockup(string? poolMeta)
    {
        if (string.IsNullOrEmpty(poolMeta))
        {
            return null;
        }

        Match m = LockupDaysUnstakingPattern.Match(poolMeta);

        if (m.Success)
        {
            return $"{m.Groups["days"].Value}-day unstaking period";
        }

        m = LockupUnstakingCooldownPattern.Match(poolMeta);

        if (m.Success)
        {
            return $"{m.Groups["days"].Value}-day unstaking cooldown";
        }

        m = LockupWithdrawalCyclePattern.Match(poolMeta);

        if (m.Success)
        {
            return $"{m.Groups["days"].Value}-day withdrawal cycle";
        }

        m = LockupDaysLockedPattern.Match(poolMeta);

        if (m.Success)
        {
            return $"{m.Groups["days"].Value}-day lockup";
        }

        m = LockupCooldownPattern.Match(poolMeta);

        if (m.Success)
        {
            return $"{m.Groups["days"].Value}-day cooldown";
        }

        return MaturityPattern.IsMatch(poolMeta) ? "Fixed-term (held to maturity)" : null;
    }
}
