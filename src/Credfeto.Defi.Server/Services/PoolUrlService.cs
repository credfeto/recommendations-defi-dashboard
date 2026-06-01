using System;
using System.Collections.Generic;
using System.Globalization;
using Credfeto.Defi.Server.Models;

namespace Credfeto.Defi.Server.Services;

/// <summary>
///     Generates direct pool URLs for DefiLlama and Pendle pools.
/// </summary>
internal static class PoolUrlService
{
    private static readonly IReadOnlyDictionary<string, int> PendleChainIds = new Dictionary<string, int>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["ethereum"] = 1,
        ["arbitrum"] = 42161,
        ["base"] = 8453,
        ["bsc"] = 56,
    };

    /// <summary>
    ///     Returns a direct URL to the pool page, or null if a URL cannot be determined.
    ///     - DefiLlama pools: defillama.com/yields?pool={uuid}
    ///     - Pendle pools:    app.pendle.finance/trade/markets/{chainId}/{address}/pt
    /// </summary>
    public static Uri? GetPoolUrl(RawPool pool)
    {
        if (string.Equals(a: pool.Project, b: "pendle", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            if (!PendleChainIds.TryGetValue(key: pool.Chain.ToLowerInvariant(), out int chainId))
            {
                return null;
            }

            string urlString = string.Format(
                provider: CultureInfo.InvariantCulture,
                format: "https://app.pendle.finance/trade/markets/{0}/{1}/pt",
                chainId,
                pool.PoolId
            );

            return new Uri(urlString, UriKind.Absolute);
        }

        // All non-Pendle pools are treated as DefiLlama
        return new Uri($"https://defillama.com/yields?pool={pool.PoolId}", UriKind.Absolute);
    }
}
