using System;
using System.Collections.Generic;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class DepegServiceExtendedTests : TestBase
{
    [Fact]
    public void BuildStablecoinAddressMap_StablecoinWithoutCurrentPrice_NotIndexed()
    {
        CoinGeckoStablecoin[] stablecoins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "USDC",
                Name = "USD Coin",
                CurrentPrice = null,
            },
        ];

        CoinGeckoCoinPlatforms[] coinList =
        [
            new CoinGeckoCoinPlatforms
            {
                Id = "usd-coin",
                Symbol = "usdc",
                Platforms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ethereum"] = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48",
                },
            },
        ];

        IReadOnlyDictionary<string, string> map = DepegService.BuildStablecoinAddressMap(stablecoins, coinList);

        Assert.Empty(map);
    }

    [Fact]
    public void BuildStablecoinAddressMap_StablecoinWithCurrentPrice_IndexedByAddress()
    {
        CoinGeckoStablecoin[] stablecoins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "USDC",
                Name = "USD Coin",
                CurrentPrice = 1.0m,
            },
        ];

        CoinGeckoCoinPlatforms[] coinList =
        [
            new CoinGeckoCoinPlatforms
            {
                Id = "usd-coin",
                Symbol = "usdc",
                Platforms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ethereum"] = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48",
                },
            },
        ];

        IReadOnlyDictionary<string, string> map = DepegService.BuildStablecoinAddressMap(stablecoins, coinList);

        Assert.True(
            map.ContainsKey("0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"),
            userMessage: "Should contain USDC address"
        );
        Assert.Equal(expected: "usdc", actual: map["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"]);
    }

    [Fact]
    public void BuildStablecoinAddressMap_CoinWithNullPlatforms_Skipped()
    {
        CoinGeckoStablecoin[] stablecoins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "USDC",
                Name = "USD Coin",
                CurrentPrice = 1.0m,
            },
        ];

        CoinGeckoCoinPlatforms[] coinList =
        [
            new CoinGeckoCoinPlatforms
            {
                Id = "usd-coin",
                Symbol = "usdc",
                Platforms = null,
            },
        ];

        IReadOnlyDictionary<string, string> map = DepegService.BuildStablecoinAddressMap(stablecoins, coinList);

        Assert.Empty(map);
    }

    [Fact]
    public void BuildStablecoinAddressMap_CoinNotInStablecoinList_Skipped()
    {
        CoinGeckoStablecoin[] stablecoins =
        [
            new CoinGeckoStablecoin
            {
                Id = "bitcoin",
                Symbol = "BTC",
                Name = "Bitcoin",
                CurrentPrice = 50000m,
            },
        ];

        // usd-coin is not in the stablecoins list
        CoinGeckoCoinPlatforms[] coinList =
        [
            new CoinGeckoCoinPlatforms
            {
                Id = "usd-coin",
                Symbol = "usdc",
                Platforms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ethereum"] = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48",
                },
            },
        ];

        IReadOnlyDictionary<string, string> map = DepegService.BuildStablecoinAddressMap(stablecoins, coinList);

        Assert.Empty(map);
    }

    [Fact]
    public void BuildStablecoinAddressMap_PlatformAddressNon0x_Filtered()
    {
        CoinGeckoStablecoin[] stablecoins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "USDC",
                Name = "USD Coin",
                CurrentPrice = 1.0m,
            },
        ];

        CoinGeckoCoinPlatforms[] coinList =
        [
            new CoinGeckoCoinPlatforms
            {
                Id = "usd-coin",
                Symbol = "usdc",
                Platforms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["solana"] = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v", // non-0x Solana address
                },
            },
        ];

        IReadOnlyDictionary<string, string> map = DepegService.BuildStablecoinAddressMap(stablecoins, coinList);

        Assert.Empty(map);
    }

    [Fact]
    public void BuildStablecoinAddressMap_PlatformAddressEmpty_Filtered()
    {
        CoinGeckoStablecoin[] stablecoins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "USDC",
                Name = "USD Coin",
                CurrentPrice = 1.0m,
            },
        ];

        CoinGeckoCoinPlatforms[] coinList =
        [
            new CoinGeckoCoinPlatforms
            {
                Id = "usd-coin",
                Symbol = "usdc",
                Platforms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["some-chain"] = string.Empty,
                },
            },
        ];

        IReadOnlyDictionary<string, string> map = DepegService.BuildStablecoinAddressMap(stablecoins, coinList);

        Assert.Empty(map);
    }

    [Fact]
    public void CheckDepeg_SpanOverload_EmptySpan_ReturnsNoAlerts()
    {
        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["usdc"] = 0.94m, // critical depeg
        };

        // Empty span should not trigger address-based check
        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "POOL",
            priceMap: priceMap,
            underlyingTokens: [],
            addressMap: null,
            metadataMap: null
        );

        // Symbol "POOL" is not in priceMap → no alerts
        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckDepeg_SpanOverload_NonEmptySpan_DelegatesToArrayOverload()
    {
        const string USDC_ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";

        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["usdc"] = 0.94m, // critical depeg
        };

        Dictionary<string, string> addressMap = new(StringComparer.OrdinalIgnoreCase) { [USDC_ADDRESS] = "usdc" };

        string[] underlying = [USDC_ADDRESS];

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "POOL",
            priceMap: priceMap,
            underlyingTokens: new ReadOnlySpan<string>(underlying),
            addressMap: addressMap,
            metadataMap: null
        );

        Assert.Single(alerts);
        Assert.Equal(expected: "USDC", actual: alerts[0].Symbol);
        Assert.Equal(expected: "critical", actual: alerts[0].Severity);
    }

    [Fact]
    public void CheckDepeg_AddressMapPath_AddressFoundResolvesToSymbol_GeneratesAlert()
    {
        const string USDC_ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";

        // 0.988 → |0.988 - 1.0| / 1.0 = 0.012 = 1.2% — above warn (0.5%) but below critical (2%) → warning
        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase) { ["usdc"] = 0.988m };

        Dictionary<string, string> addressMap = new(StringComparer.OrdinalIgnoreCase) { [USDC_ADDRESS] = "usdc" };

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "GENERIC-POOL",
            priceMap: priceMap,
            underlyingTokens: [USDC_ADDRESS],
            addressMap: addressMap,
            metadataMap: null
        );

        Assert.Single(alerts);
        Assert.Equal(expected: "USDC", actual: alerts[0].Symbol);
        Assert.Equal(expected: "warning", actual: alerts[0].Severity);
    }

    [Fact]
    public void CheckDepeg_AddressMapPath_AddressNotInMap_NoAlert()
    {
        const string UNKNOWN_ADDRESS = "0x1234567890123456789012345678901234567890";

        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase) { ["usdc"] = 0.94m };

        Dictionary<string, string> addressMap = new(StringComparer.OrdinalIgnoreCase) { ["0xaaaa"] = "usdc" };

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "SOME-POOL",
            priceMap: priceMap,
            underlyingTokens: [UNKNOWN_ADDRESS],
            addressMap: addressMap,
            metadataMap: null
        );

        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckDepeg_DuplicateSymbolViaAddressMap_SuppressedBySeenSet()
    {
        const string USDC_ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";

        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["usdc"] = 0.94m, // critical depeg
        };

        Dictionary<string, string> addressMap = new(StringComparer.OrdinalIgnoreCase) { [USDC_ADDRESS] = "usdc" };

        // Symbol check will add "usdc" to seen set first
        // Address check finds same symbol - should be suppressed
        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "USDC",
            priceMap: priceMap,
            underlyingTokens: [USDC_ADDRESS],
            addressMap: addressMap,
            metadataMap: null
        );

        // Only one alert despite two code paths matching the same symbol
        Assert.Single(alerts);
    }

    [Fact]
    public void BuildStablecoinPriceMap_CoinWithoutCurrentPrice_NotAdded()
    {
        CoinGeckoStablecoin[] coins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "USDC",
                Name = "USD Coin",
                CurrentPrice = null,
            },
            new CoinGeckoStablecoin
            {
                Id = "tether",
                Symbol = "USDT",
                Name = "Tether",
                CurrentPrice = 1.0m,
            },
        ];

        IReadOnlyDictionary<string, decimal> map = DepegService.BuildStablecoinPriceMap(coins);

        Assert.False(map.ContainsKey("usdc"), userMessage: "USDC without price should not be in map");
        Assert.True(map.ContainsKey("usdt"), userMessage: "USDT with price should be in map");
    }

    [Fact]
    public void CheckDepeg_AddressMapPath_AddressFoundButSymbolNotInPriceMap_NoAlert()
    {
        const string USDC_ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";

        // Address map maps the address to "usdc", but "usdc" is NOT in the price map
        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string> addressMap = new(StringComparer.OrdinalIgnoreCase) { [USDC_ADDRESS] = "usdc" };

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "GENERIC-POOL",
            priceMap: priceMap,
            underlyingTokens: [USDC_ADDRESS],
            addressMap: addressMap,
            metadataMap: null
        );

        Assert.Empty(alerts);
    }

    [Fact]
    public void BuildMergedStablecoinPriceMap_EmptyBothSources_ReturnsEmptyMap()
    {
        IReadOnlyDictionary<string, decimal> map = DepegService
            .BuildMergedStablecoinPriceMap(coinGeckoCoins: [], chainlinkFeeds: [], defiLlamaStablecoins: [])
            .Prices;

        Assert.Empty(map);
    }

    [Fact]
    public void BuildMergedStablecoinPriceMap_OnlyCoinGecko_ReturnsCoinGeckoPrices()
    {
        CoinGeckoStablecoin[] coins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "USDC",
                Name = "USD Coin",
                CurrentPrice = 1.0m,
            },
        ];

        IReadOnlyDictionary<string, decimal> map = DepegService
            .BuildMergedStablecoinPriceMap(coinGeckoCoins: coins, chainlinkFeeds: [], defiLlamaStablecoins: [])
            .Prices;

        Assert.True(map.ContainsKey("usdc"), "expected 'usdc' to be in merged map");
        Assert.Equal(expected: 1.0m, actual: map["usdc"]);
    }

    [Fact]
    public void BuildMergedStablecoinPriceMap_OnlyChainlink_ReturnsChainlinkPrices()
    {
        ChainlinkPriceFeed[] feeds = [new ChainlinkPriceFeed("usdc", 1.0001m)];

        IReadOnlyDictionary<string, decimal> map = DepegService
            .BuildMergedStablecoinPriceMap(coinGeckoCoins: [], chainlinkFeeds: feeds, defiLlamaStablecoins: [])
            .Prices;

        Assert.True(map.ContainsKey("usdc"), "expected 'usdc' to be in merged map");
        Assert.Equal(expected: 1.0001m, actual: map["usdc"]);
    }

    [Fact]
    public void BuildMergedStablecoinPriceMap_ChainlinkOverridesCoinGecko_ChainlinkPriceTakesPrecedence()
    {
        CoinGeckoStablecoin[] coins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "USDC",
                Name = "USD Coin",
                CurrentPrice = 0.999m,
            },
        ];

        ChainlinkPriceFeed[] feeds = [new ChainlinkPriceFeed("usdc", 1.0001m)];

        IReadOnlyDictionary<string, decimal> map = DepegService
            .BuildMergedStablecoinPriceMap(coinGeckoCoins: coins, chainlinkFeeds: feeds, defiLlamaStablecoins: [])
            .Prices;

        Assert.Equal(expected: 1.0001m, actual: map["usdc"]);
    }

    [Fact]
    public void BuildMergedStablecoinPriceMap_CoinGeckoWithoutPrice_NotIncluded()
    {
        CoinGeckoStablecoin[] coins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "USDC",
                Name = "USD Coin",
                CurrentPrice = null,
            },
        ];

        IReadOnlyDictionary<string, decimal> map = DepegService
            .BuildMergedStablecoinPriceMap(coinGeckoCoins: coins, chainlinkFeeds: [], defiLlamaStablecoins: [])
            .Prices;

        Assert.Empty(map);
    }

    [Fact]
    public void BuildMergedStablecoinPriceMap_BothSources_MergesDistinctSymbols()
    {
        CoinGeckoStablecoin[] coins =
        [
            new CoinGeckoStablecoin
            {
                Id = "tether",
                Symbol = "USDT",
                Name = "Tether",
                CurrentPrice = 1.0m,
            },
        ];

        ChainlinkPriceFeed[] feeds = [new ChainlinkPriceFeed("usdc", 1.0001m)];

        IReadOnlyDictionary<string, decimal> map = DepegService
            .BuildMergedStablecoinPriceMap(coinGeckoCoins: coins, chainlinkFeeds: feeds, defiLlamaStablecoins: [])
            .Prices;

        Assert.True(map.ContainsKey("usdt"), "expected 'usdt' from CoinGecko in merged map");
        Assert.True(map.ContainsKey("usdc"), "expected 'usdc' from Chainlink in merged map");
        Assert.Equal(expected: 2, actual: map.Count);
    }

    [Fact]
    public void BuildMergedStablecoinPriceMap_AllThreeSources_ChainlinkThenDefiLlamaThenCoinGecko()
    {
        CoinGeckoStablecoin[] coins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "USDC",
                Name = "USD Coin",
                CurrentPrice = 0.97m,
            },
            new CoinGeckoStablecoin
            {
                Id = "tether",
                Symbol = "USDT",
                Name = "Tether",
                CurrentPrice = 0.98m,
            },
            new CoinGeckoStablecoin
            {
                Id = "dai",
                Symbol = "DAI",
                Name = "Dai",
                CurrentPrice = 0.99m,
            },
        ];

        ChainlinkPriceFeed[] feeds = [new ChainlinkPriceFeed("usdc", 1.0001m)];

        DefiLlamaStablecoin[] llamaCoins =
        [
            CreateLlamaStablecoin(symbol: "USDC", price: 0.95m),
            CreateLlamaStablecoin(symbol: "USDT", price: 1.0002m),
        ];

        MergedStablecoinPriceMaps maps = DepegService.BuildMergedStablecoinPriceMap(
            coinGeckoCoins: coins,
            chainlinkFeeds: feeds,
            defiLlamaStablecoins: llamaCoins
        );

        Assert.Equal(expected: 1.0001m, actual: maps.Prices["usdc"]);
        Assert.Equal(expected: 1.0002m, actual: maps.Prices["usdt"]);
        Assert.Equal(expected: 0.99m, actual: maps.Prices["dai"]);
        Assert.Equal(expected: "chainlink", actual: maps.Metadata["usdc"].DataSource);
        Assert.Equal(expected: "defillama", actual: maps.Metadata["usdt"].DataSource);
        Assert.Equal(expected: "coingecko", actual: maps.Metadata["dai"].DataSource);
    }

    [Fact]
    public void BuildMergedStablecoinPriceMap_DefiLlamaOnlySymbol_IncludedWithPegMechanism()
    {
        DefiLlamaStablecoin[] llamaCoins = [CreateLlamaStablecoin(symbol: "PYUSD", price: 0.9995m)];

        MergedStablecoinPriceMaps maps = DepegService.BuildMergedStablecoinPriceMap(
            coinGeckoCoins: [],
            chainlinkFeeds: [],
            defiLlamaStablecoins: llamaCoins
        );

        Assert.Equal(expected: 0.9995m, actual: maps.Prices["pyusd"]);
        Assert.Equal(
            expected: new StablecoinMetadata(DataSource: "defillama", PegType: "fiat-backed"),
            actual: maps.Metadata["pyusd"]
        );
    }

    [Fact]
    public void BuildMergedStablecoinPriceMap_DefiLlamaNonUsdOrMissingPrice_Skipped()
    {
        DefiLlamaStablecoin[] llamaCoins =
        [
            CreateLlamaStablecoin(symbol: "EURC", price: 1.08m, pegType: "peggedEUR"),
            CreateLlamaStablecoin(symbol: "USDX", price: null),
        ];

        MergedStablecoinPriceMaps maps = DepegService.BuildMergedStablecoinPriceMap(
            coinGeckoCoins: [],
            chainlinkFeeds: [],
            defiLlamaStablecoins: llamaCoins
        );

        Assert.Empty(maps.Prices);
        Assert.Empty(maps.Metadata);
    }

    [Fact]
    public void BuildMergedStablecoinPriceMap_ChainlinkAndCoinGecko_MetadataHasNoPegType()
    {
        CoinGeckoStablecoin[] coins =
        [
            new CoinGeckoStablecoin
            {
                Id = "tether",
                Symbol = "USDT",
                Name = "Tether",
                CurrentPrice = 1.0m,
            },
        ];

        ChainlinkPriceFeed[] feeds = [new ChainlinkPriceFeed("usdc", 1.0001m)];

        MergedStablecoinPriceMaps maps = DepegService.BuildMergedStablecoinPriceMap(
            coinGeckoCoins: coins,
            chainlinkFeeds: feeds,
            defiLlamaStablecoins: []
        );

        Assert.Equal(
            expected: new StablecoinMetadata(DataSource: "chainlink", PegType: null),
            actual: maps.Metadata["usdc"]
        );
        Assert.Equal(
            expected: new StablecoinMetadata(DataSource: "coingecko", PegType: null),
            actual: maps.Metadata["usdt"]
        );
    }

    [Fact]
    public void CheckDepeg_SymbolPathWithMetadata_AlertCarriesPegTypeAndDataSource()
    {
        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase) { ["usdc"] = 0.95m };
        Dictionary<string, StablecoinMetadata> metadataMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["usdc"] = new StablecoinMetadata(DataSource: "defillama", PegType: "fiat-backed"),
        };

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "USDC",
            priceMap: priceMap,
            underlyingTokens: null,
            addressMap: null,
            metadataMap: metadataMap
        );

        DepegAlert alert = Assert.Single(alerts);
        Assert.Equal(expected: "defillama", actual: alert.DataSource);
        Assert.Equal(expected: "fiat-backed", actual: alert.PegType);
    }

    [Fact]
    public void CheckDepeg_AddressPathWithMetadata_AlertCarriesPegTypeAndDataSource()
    {
        const string USDC_ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";

        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase) { ["usdc"] = 0.99m };
        Dictionary<string, string> addressMap = new(StringComparer.OrdinalIgnoreCase) { [USDC_ADDRESS] = "usdc" };
        Dictionary<string, StablecoinMetadata> metadataMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["usdc"] = new StablecoinMetadata(DataSource: "chainlink", PegType: null),
        };

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "GENERIC-POOL",
            priceMap: priceMap,
            underlyingTokens: [USDC_ADDRESS],
            addressMap: addressMap,
            metadataMap: metadataMap
        );

        DepegAlert alert = Assert.Single(alerts);
        Assert.Equal(expected: "USDC", actual: alert.Symbol);
        Assert.Equal(expected: "chainlink", actual: alert.DataSource);
        Assert.Null(alert.PegType);
    }

    [Fact]
    public void CheckDepeg_SymbolMissingFromMetadataMap_AlertHasNullMetadataFields()
    {
        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase) { ["usdt"] = 0.95m };
        Dictionary<string, StablecoinMetadata> metadataMap = new(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "USDT",
            priceMap: priceMap,
            underlyingTokens: null,
            addressMap: null,
            metadataMap: metadataMap
        );

        DepegAlert alert = Assert.Single(alerts);
        Assert.Null(alert.DataSource);
        Assert.Null(alert.PegType);
    }

    [Fact]
    public void CheckDepeg_NullMetadataMap_AlertHasNullMetadataFields()
    {
        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase) { ["usdt"] = 0.95m };

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "USDT",
            priceMap: priceMap,
            underlyingTokens: null,
            addressMap: null,
            metadataMap: null
        );

        DepegAlert alert = Assert.Single(alerts);
        Assert.Null(alert.DataSource);
        Assert.Null(alert.PegType);
    }

    private static DefiLlamaStablecoin CreateLlamaStablecoin(
        string symbol,
        decimal? price,
        string pegType = "peggedUSD"
    )
    {
        return new DefiLlamaStablecoin
        {
            Id = symbol.ToLowerInvariant(),
            Name = symbol,
            Symbol = symbol,
            PegType = pegType,
            PegMechanism = "fiat-backed",
            Price = price,
        };
    }
}
