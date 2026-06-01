using System;
using System.Collections.Generic;
using System.Linq;
using Credfeto.Defi.Server.Models;

namespace Credfeto.Defi.Server.Services;

/// <summary>
///     Applies pool type category filters and base quality filters.
/// </summary>
internal static class PoolFilterService
{
    private const double MIN_TVL = 1_000_000;

    private static readonly HashSet<string> ExcludedChains = new(StringComparer.OrdinalIgnoreCase)
    {
        "Aptos",
        "Avalanche",
        "Cardano",
        "FileCoin",
        "Flare",
        "Flow",
        "Icp",
        "Stellar",
        "Sui",
        "Ton",
        "Tron",
        "Venom",
    };

    private static readonly string[] LstSymbols =
    [
        "ETH",
        "STETH",
        "WSTETH",
        "RETH",
        "CBETH",
        "SWETH",
        "LSETH",
        "EETH",
        "WEETH",
    ];

    /// <summary>
    ///     Applies base quality filters: IL risk, TVL, APY range, chain exclusions.
    ///     Returns pools sorted by APY descending, then TVL descending.
    /// </summary>
    public static IReadOnlyList<RawPool> ApplyBaseFilters(IReadOnlyList<RawPool> pools)
    {
        List<RawPool> result = [];

        foreach (RawPool pool in pools)
        {
            if (!string.Equals(a: pool.IlRisk, b: "no", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (pool.TvlUsd < MIN_TVL)
            {
                continue;
            }

            if (pool.Apy is <= 0 or >= 100)
            {
                continue;
            }

            if (ExcludedChains.Contains(pool.Chain))
            {
                continue;
            }

            result.Add(pool);
        }

        result.Sort(
            static (a, b) =>
            {
                int apyComparison = b.Apy.CompareTo(a.Apy);

                return apyComparison != 0 ? apyComparison : b.TvlUsd.CompareTo(a.TvlUsd);
            }
        );

        return result;
    }

    /// <summary>
    ///     Filters pools by pool type category and applies base filters.
    /// </summary>
    public static IReadOnlyList<RawPool> FilterPoolsByType(IReadOnlyList<RawPool> allPools, string poolType)
    {
        return ApplyBaseFilters([.. allPools.Where(pool => MatchesCategory(pool: pool, poolType: poolType))]);
    }

    private static bool MatchesCategory(RawPool pool, string poolType)
    {
        if (string.Equals(a: poolType, b: "ETH", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return MatchesEthCategory(pool);
        }

        if (string.Equals(a: poolType, b: "STABLES", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return pool.Stablecoin;
        }

        if (string.Equals(a: poolType, b: "HIGH_YIELD", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return pool.Apy > 5;
        }

        return string.Equals(a: poolType, b: "LOW_TVL", comparisonType: StringComparison.OrdinalIgnoreCase)
            ? pool.TvlUsd < 10_000_000
            : string.Equals(a: poolType, b: "BLUE_CHIP", comparisonType: StringComparison.OrdinalIgnoreCase)
                && pool.TvlUsd > 100_000_000;
    }

    private static bool MatchesEthCategory(RawPool pool)
    {
        string symbolUpper = pool.Symbol.ToUpperInvariant();

        return LstSymbols.Any(s => symbolUpper.Contains(value: s, comparisonType: StringComparison.Ordinal))
            || pool.UnderlyingTokens is not null
                && pool.UnderlyingTokens.Any(token =>
                    LstSymbols.Any(s =>
                        token.ToUpperInvariant().Contains(value: s, comparisonType: StringComparison.Ordinal)
                    )
                );
    }
}
