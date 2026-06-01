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
            addressMap: null
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
            addressMap: addressMap
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
            addressMap: addressMap
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
            addressMap: addressMap
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
            addressMap: addressMap
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
            addressMap: addressMap
        );

        Assert.Empty(alerts);
    }
}
