using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Services;

public static partial class DepegService
{
    private const decimal USD_PEG = 1.0m;
    private const decimal WARN_THRESHOLD = 0.005m; // 0.5%
    private const decimal CRITICAL_THRESHOLD = 0.02m; // 2%
    private const string DEFILLAMA_USD_PEG_TYPE = "peggedUSD";
    private const string DATA_SOURCE_CHAINLINK = "chainlink";
    private const string DATA_SOURCE_DEFILLAMA = "defillama";
    private const string DATA_SOURCE_COINGECKO = "coingecko";

    [GeneratedRegex(pattern: "[-/+\\s]+", options: RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 500)]
    private static partial Regex TokenSplitRegex { get; }

    public static IReadOnlyDictionary<string, decimal> BuildStablecoinPriceMap(IReadOnlyList<CoinGeckoStablecoin> coins)
    {
        Dictionary<string, decimal> map = new(StringComparer.OrdinalIgnoreCase);

        foreach (CoinGeckoStablecoin coin in coins)
        {
            if (coin.CurrentPrice.HasValue)
            {
                _ = map.TryAdd(key: coin.Symbol.ToLowerInvariant(), value: coin.CurrentPrice.Value);
            }
        }

        return map;
    }

    public static MergedStablecoinPriceMaps BuildMergedStablecoinPriceMap(
        IReadOnlyList<CoinGeckoStablecoin> coinGeckoCoins,
        IReadOnlyList<ChainlinkPriceFeed> chainlinkFeeds,
        IReadOnlyList<DefiLlamaStablecoin> defiLlamaStablecoins
    )
    {
        Dictionary<string, decimal> prices = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StablecoinMetadata> metadata = new(StringComparer.OrdinalIgnoreCase);

        // Sources are added in precedence order and TryAdd keeps the first price seen, so Chainlink (canonical
        // on-chain) wins over DefiLlama (aggregated), which wins over CoinGecko.
        foreach (ChainlinkPriceFeed feed in chainlinkFeeds)
        {
            AddPrice(
                prices: prices,
                metadata: metadata,
                symbol: feed.Symbol,
                price: feed.CurrentPrice,
                source: new(DataSource: DATA_SOURCE_CHAINLINK, PegType: null)
            );
        }

        foreach (DefiLlamaStablecoin coin in defiLlamaStablecoins)
        {
            // Only USD-pegged assets are comparable with the USD peg the alert thresholds are measured against.
            if (coin.Price.HasValue && StringComparer.Ordinal.Equals(x: coin.PegType, y: DEFILLAMA_USD_PEG_TYPE))
            {
                // PegType is always peggedUSD here, so the mechanism (fiat-backed, algorithmic, ...) is the useful detail.
                AddPrice(
                    prices: prices,
                    metadata: metadata,
                    symbol: coin.Symbol,
                    price: coin.Price.Value,
                    source: new(DataSource: DATA_SOURCE_DEFILLAMA, PegType: coin.PegMechanism)
                );
            }
        }

        foreach (CoinGeckoStablecoin coin in coinGeckoCoins)
        {
            if (coin.CurrentPrice.HasValue)
            {
                AddPrice(
                    prices: prices,
                    metadata: metadata,
                    symbol: coin.Symbol,
                    price: coin.CurrentPrice.Value,
                    source: new(DataSource: DATA_SOURCE_COINGECKO, PegType: null)
                );
            }
        }

        return new(Prices: prices, Metadata: metadata);
    }

    private static void AddPrice(
        Dictionary<string, decimal> prices,
        Dictionary<string, StablecoinMetadata> metadata,
        string symbol,
        decimal price,
        in StablecoinMetadata source
    )
    {
        string key = symbol.ToLowerInvariant();

        if (prices.TryAdd(key: key, value: price))
        {
            metadata[key] = source;
        }
    }

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
                _ = idToSymbol.TryAdd(key: coin.Id, value: coin.Symbol.ToLowerInvariant());
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
                _ = addressMap.TryAdd(key: address.ToLowerInvariant(), value: symbol);
            }
        }

        return addressMap;
    }

    public static string[] ParsePoolSymbols(string poolSymbol)
    {
        return
        [
            .. TokenSplitRegex
                .Split(poolSymbol)
                .Select(static p => p.Trim())
                .Where(static p => !string.IsNullOrEmpty(p)),
        ];
    }

    public static IReadOnlyList<DepegAlert> CheckDepeg(
        string poolSymbol,
        IReadOnlyDictionary<string, decimal> priceMap,
        ReadOnlySpan<string> underlyingTokens,
        IReadOnlyDictionary<string, string>? addressMap,
        IReadOnlyDictionary<string, StablecoinMetadata>? metadataMap
    )
    {
        string[]? underlyingTokensArray = underlyingTokens.IsEmpty ? null : underlyingTokens.ToArray();

        return CheckDepeg(
            poolSymbol: poolSymbol,
            priceMap: priceMap,
            underlyingTokens: underlyingTokensArray,
            addressMap: addressMap,
            metadataMap: metadataMap
        );
    }

    public static IReadOnlyList<DepegAlert> CheckDepeg(
        string poolSymbol,
        IReadOnlyDictionary<string, decimal> priceMap,
        string[]? underlyingTokens,
        IReadOnlyDictionary<string, string>? addressMap,
        IReadOnlyDictionary<string, StablecoinMetadata>? metadataMap
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

            DepegAlert? alert = MakeAlert(
                symbol: token,
                price: price,
                metadata: FindMetadata(metadataMap: metadataMap, key: key)
            );

            if (alert is not null)
            {
                alerts.Add(alert);
            }
        }

        // Address-based check (underlyingTokens)
        if (addressMap is not null && underlyingTokens is not null)
        {
            AddAddressAlerts(
                alerts: alerts,
                seen: seen,
                priceMap: priceMap,
                underlyingTokens: underlyingTokens,
                addressMap: addressMap,
                metadataMap: metadataMap
            );
        }

        return alerts;
    }

    private static void AddAddressAlerts(
        List<DepegAlert> alerts,
        HashSet<string> seen,
        IReadOnlyDictionary<string, decimal> priceMap,
        string[] underlyingTokens,
        IReadOnlyDictionary<string, string> addressMap,
        IReadOnlyDictionary<string, StablecoinMetadata>? metadataMap
    )
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

            DepegAlert? alert = MakeAlert(
                symbol: symbol.ToUpperInvariant(),
                price: price,
                metadata: FindMetadata(metadataMap: metadataMap, key: symbol)
            );

            if (alert is not null)
            {
                alerts.Add(alert);
            }
        }
    }

    private static StablecoinMetadata? FindMetadata(
        IReadOnlyDictionary<string, StablecoinMetadata>? metadataMap,
        string key
    )
    {
        return metadataMap is not null && metadataMap.TryGetValue(key: key, out StablecoinMetadata metadata)
            ? metadata
            : null;
    }

    private static DepegAlert? MakeAlert(string symbol, decimal price, in StablecoinMetadata? metadata)
    {
        decimal deviation = (price - USD_PEG) / USD_PEG;
        decimal absDeviation = Math.Abs(deviation);

        if (absDeviation < WARN_THRESHOLD)
        {
            return null;
        }

        string severity = absDeviation >= CRITICAL_THRESHOLD ? "critical" : "warning";

        return new DepegAlert
        {
            Symbol = symbol,
            CurrentPrice = price,
            PegPrice = USD_PEG,
            Deviation = deviation,
            Severity = severity,
            PegType = metadata?.PegType,
            DataSource = metadata?.DataSource,
        };
    }
}
