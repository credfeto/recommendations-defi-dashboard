using System;
using System.Collections.Generic;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class DepegServiceTests : TestBase
{
    [Fact]
    public void ParsePoolSymbols_DashSeparated_SplitsIntoTokens()
    {
        string[] result = DepegService.ParsePoolSymbols("USDC-USDT");
        Assert.Equal(expected: ["USDC", "USDT"], actual: result);
    }

    [Fact]
    public void ParsePoolSymbols_SlashSeparated_SplitsIntoTokens()
    {
        string[] result = DepegService.ParsePoolSymbols("USR/USDC");
        Assert.Equal(expected: ["USR", "USDC"], actual: result);
    }

    [Fact]
    public void ParsePoolSymbols_PlusSeparated_SplitsIntoTokens()
    {
        string[] result = DepegService.ParsePoolSymbols("ETH+WETH");
        Assert.Equal(expected: ["ETH", "WETH"], actual: result);
    }

    [Fact]
    public void ParsePoolSymbols_SingleToken_ReturnsSingleElement()
    {
        string[] result = DepegService.ParsePoolSymbols("singletoken");
        Assert.Equal(expected: ["singletoken"], actual: result);
    }

    [Fact]
    public void ParsePoolSymbols_SpaceSeparated_SplitsIntoTokens()
    {
        string[] result = DepegService.ParsePoolSymbols("USDC USDT");
        Assert.Equal(expected: ["USDC", "USDT"], actual: result);
    }

    [Fact]
    public void CheckDepeg_NoPriceData_ReturnsNoAlerts()
    {
        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "USDC-USDT",
            priceMap: priceMap,
            underlyingTokens: null,
            addressMap: null
        );

        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckDepeg_StablecoinOnPeg_ReturnsNoAlerts()
    {
        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase) { ["usdc"] = 1.001m };

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "USDC",
            priceMap: priceMap,
            underlyingTokens: null,
            addressMap: null
        );

        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckDepeg_StablecoinWarningDeviation_ReturnsWarningAlert()
    {
        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase) { ["usdc"] = 0.99m };

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "USDC",
            priceMap: priceMap,
            underlyingTokens: null,
            addressMap: null
        );

        Assert.Single(alerts);
        Assert.Equal(expected: "warning", actual: alerts[0].Severity);
    }

    [Fact]
    public void CheckDepeg_StablecoinCriticalDeviation_ReturnsCriticalAlert()
    {
        Dictionary<string, decimal> priceMap = new(StringComparer.OrdinalIgnoreCase) { ["usdt"] = 0.95m };

        IReadOnlyList<DepegAlert> alerts = DepegService.CheckDepeg(
            poolSymbol: "USDT",
            priceMap: priceMap,
            underlyingTokens: null,
            addressMap: null
        );

        Assert.Single(alerts);
        Assert.Equal(expected: "critical", actual: alerts[0].Severity);
    }

    [Fact]
    public void BuildStablecoinPriceMap_ReturnsLowercasedKeys()
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

        IReadOnlyDictionary<string, decimal> map = DepegService.BuildStablecoinPriceMap(coins);

        Assert.True(map.ContainsKey("usdc"), userMessage: "Price map should contain lowercase 'usdc' key");
        Assert.Equal(expected: 1.0m, actual: map["usdc"]);
    }
}
