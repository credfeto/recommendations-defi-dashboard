using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Credfeto.Defi.Server.Models;

namespace Credfeto.Defi.Server.Services;

/// <summary>
///     Builds stablecoin price maps and detects depeg risk for pool tokens.
/// </summary>
public static partial class DepegService
{
    private const decimal USD_PEG = 1.0m;
    private const decimal WARN_THRESHOLD = 0.005m; // 0.5%
    private const decimal CRITICAL_THRESHOLD = 0.02m; // 2%

    [GeneratedRegex(pattern: "[-/+\\s]+", options: RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 500)]
    private static partial Regex TokenSplitRegex();

    /// <summary>
    ///     Builds a normalised symbol → current price map from CoinGecko stablecoin data.
    ///     Symbols are lowercased for case-insensitive matching.
    /// </summary>
    public static IReadOnlyDictionary<string, decimal> BuildStablecoinPriceMap(IReadOnlyList<CoinGeckoStablecoin> coins)
    {
        Dictionary<string, decimal> map = new(StringComparer.OrdinalIgnoreCase);

        foreach (CoinGeckoStablecoin coin in coins)
        {
            if (coin.CurrentPrice.HasValue)
            {
                map.TryAdd(key: coin.Symbol.ToLowerInvariant(), value: coin.CurrentPrice.Value);
            }
        }

        return map;
    }

    /// <summary>
    ///     Builds a contract address → stablecoin symbol map from the full CoinGecko coin list.
    ///     Only stablecoin IDs present in the price data are indexed.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BuildStablecoinAddressMap(
        IReadOnlyList<CoinGeckoStablecoin> stablecoins,
        IReadOnlyList<CoinGeckoCoinPlatforms> coinList
    )
    {
        Dictionary<string, string> idToSymbol = new(StringComparer.OrdinalIgnoreCase);

        foreach (CoinGeckoStablecoin coin in stablecoins)
        {
            if (coin.CurrentPrice.HasValue)
            {
                idToSymbol.TryAdd(key: coin.Id, value: coin.Symbol.ToLowerInvariant());
            }
        }

        Dictionary<string, string> addressMap = new(StringComparer.OrdinalIgnoreCase);

        foreach (CoinGeckoCoinPlatforms coin in coinList)
        {
            if (!idToSymbol.TryGetValue(key: coin.Id, out string? symbol))
            {
                continue;
            }

            if (coin.Platforms is null)
            {
                continue;
            }

            foreach (
                string address in coin.Platforms.Values.Where(a =>
                    !string.IsNullOrEmpty(a)
                    && a.StartsWith(value: "0x", comparisonType: StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                addressMap.TryAdd(key: address.ToLowerInvariant(), value: symbol);
            }
        }

        return addressMap;
    }

    /// <summary>
    ///     Parses a pool symbol string into individual token symbols.
    ///     Handles separators: -, /, +, space.
    ///     e.g. "USR-USDC" → ["USR", "USDC"], "crvUSD" → ["crvUSD"]
    /// </summary>
    public static string[] ParsePoolSymbols(string poolSymbol)
    {
        return
        [
            .. TokenSplitRegex()
                .Split(poolSymbol)
                .Select(static p => p.Trim())
                .Where(static p => !string.IsNullOrEmpty(p)),
        ];
    }

    /// <summary>
    ///     Checks a pool for stablecoin depeg risk using two sources:
    ///     1. Pool symbol tokens looked up in the symbol → price map.
    ///     2. Underlying token contract addresses looked up in the address → symbol map,
    ///        then prices resolved via the symbol → price map.
    /// </summary>
    /// <summary>
    ///     Overload accepting a <see cref="ReadOnlySpan{T}" /> of underlying token addresses.
    ///     Delegates to the array overload after converting.
    /// </summary>
    public static IReadOnlyList<DepegAlert> CheckDepeg(
        string poolSymbol,
        IReadOnlyDictionary<string, decimal> priceMap,
        ReadOnlySpan<string> underlyingTokens,
        IReadOnlyDictionary<string, string>? addressMap
    )
    {
        return CheckDepeg(
            poolSymbol: poolSymbol,
            priceMap: priceMap,
            underlyingTokens: underlyingTokens.IsEmpty ? null : underlyingTokens.ToArray(),
            addressMap: addressMap
        );
    }

    /// <summary>
    ///     Checks a pool for stablecoin depeg risk using two sources:
    ///     1. Pool symbol tokens looked up in the symbol → price map.
    ///     2. Underlying token contract addresses looked up in the address → symbol map,
    ///        then prices resolved via the symbol → price map.
    /// </summary>
    public static IReadOnlyList<DepegAlert> CheckDepeg(
        string poolSymbol,
        IReadOnlyDictionary<string, decimal> priceMap,
        string[]? underlyingTokens,
        IReadOnlyDictionary<string, string>? addressMap
    )
    {
        List<DepegAlert> alerts = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        // Symbol-based check
        foreach (string token in ParsePoolSymbols(poolSymbol))
        {
            string key = token.ToLowerInvariant();

            if (!seen.Add(key))
            {
                continue;
            }

            if (!priceMap.TryGetValue(key: key, out decimal price))
            {
                continue;
            }

            DepegAlert? alert = MakeAlert(symbol: token, price: price);

            if (alert is not null)
            {
                alerts.Add(alert);
            }
        }

        // Address-based check (underlyingTokens)
        if (addressMap is not null && underlyingTokens is not null)
        {
            foreach (string address in underlyingTokens)
            {
                if (!addressMap.TryGetValue(key: address.ToLowerInvariant(), out string? symbol))
                {
                    continue;
                }

                if (!seen.Add(symbol))
                {
                    continue;
                }

                if (!priceMap.TryGetValue(key: symbol, out decimal price))
                {
                    continue;
                }

                DepegAlert? alert = MakeAlert(symbol: symbol.ToUpperInvariant(), price: price);

                if (alert is not null)
                {
                    alerts.Add(alert);
                }
            }
        }

        return alerts;
    }

    private static DepegAlert? MakeAlert(string symbol, decimal price)
    {
        decimal deviation = (price - USD_PEG) / USD_PEG;
        decimal absDeviation = Math.Abs(deviation);

        if (absDeviation < WARN_THRESHOLD)
        {
            return null;
        }

        return new DepegAlert
        {
            Symbol = symbol,
            CurrentPrice = price,
            PegPrice = USD_PEG,
            Deviation = deviation,
            Severity = absDeviation >= CRITICAL_THRESHOLD ? "critical" : "warning",
        };
    }
}
