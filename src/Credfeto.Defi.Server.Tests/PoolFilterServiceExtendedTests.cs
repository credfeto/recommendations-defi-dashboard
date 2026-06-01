using System.Collections.Generic;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class PoolFilterServiceExtendedTests : TestBase
{
    private static RawPool MakePool(
        string project = "aave",
        string chain = "Ethereum",
        string symbol = "USDC",
        double tvlUsd = 5_000_000,
        double apy = 5.0,
        bool stablecoin = false,
        string ilRisk = "no",
        string[]? underlyingTokens = null
    )
    {
        return new RawPool
        {
            Project = project,
            Chain = chain,
            Symbol = symbol,
            TvlUsd = tvlUsd,
            Apy = apy,
            Stablecoin = stablecoin,
            IlRisk = ilRisk,
            PoolId = "uuid-test",
            Predictions = new RawPredictions(),
            UnderlyingTokens = underlyingTokens,
        };
    }

    [Fact]
    public void FilterPoolsByType_Eth_MatchesPoolWithLstInUnderlyingTokens()
    {
        // Symbol doesn't contain an LST but underlyingTokens does
        IReadOnlyList<RawPool> pools = [MakePool(symbol: "GENERIC", underlyingTokens: ["0xsteth-address-STETH"])];
        IReadOnlyList<RawPool> result = PoolFilterService.FilterPoolsByType(pools, "ETH");
        Assert.Single(result);
    }

    [Fact]
    public void FilterPoolsByType_Eth_NullUnderlyingTokens_NotMatched()
    {
        // Symbol doesn't contain LST, underlyingTokens is null
        IReadOnlyList<RawPool> pools = [MakePool(symbol: "USDC", underlyingTokens: null)];
        IReadOnlyList<RawPool> result = PoolFilterService.FilterPoolsByType(pools, "ETH");
        Assert.Empty(result);
    }

    [Fact]
    public void FilterPoolsByType_Eth_UnderlyingTokensWithNoLst_NotMatched()
    {
        // Symbol doesn't contain LST, underlyingTokens doesn't contain LST either
        IReadOnlyList<RawPool> pools = [MakePool(symbol: "USDC-USDT", underlyingTokens: ["0xusdc", "0xusdt"])];
        IReadOnlyList<RawPool> result = PoolFilterService.FilterPoolsByType(pools, "ETH");
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("STETH")]
    [InlineData("WSTETH")]
    [InlineData("RETH")]
    [InlineData("CBETH")]
    [InlineData("SWETH")]
    [InlineData("LSETH")]
    [InlineData("EETH")]
    [InlineData("WEETH")]
    public void FilterPoolsByType_Eth_MatchesAllLstSymbols(string lstSymbol)
    {
        IReadOnlyList<RawPool> pools = [MakePool(symbol: lstSymbol)];
        IReadOnlyList<RawPool> result = PoolFilterService.FilterPoolsByType(pools, "ETH");
        Assert.Single(result);
    }

    [Fact]
    public void ApplyBaseFilters_TwoPoolsSameApyDifferentTvl_SortedByTvlDescendingWhenApyTied()
    {
        IReadOnlyList<RawPool> pools = [MakePool(apy: 5.0, tvlUsd: 1_000_000), MakePool(apy: 5.0, tvlUsd: 10_000_000)];

        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);

        Assert.Equal(expected: 2, actual: result.Count);
        // Same APY - sorted by TVL descending
        Assert.Equal(expected: 10_000_000, actual: result[0].TvlUsd);
        Assert.Equal(expected: 1_000_000, actual: result[1].TvlUsd);
    }

    [Theory]
    [InlineData("Cardano")]
    [InlineData("FileCoin")]
    [InlineData("Flare")]
    [InlineData("Flow")]
    [InlineData("Icp")]
    [InlineData("Stellar")]
    [InlineData("Ton")]
    [InlineData("Venom")]
    public void ApplyBaseFilters_ExcludesAdditionalUnsupportedChains(string chain)
    {
        IReadOnlyList<RawPool> pools = [MakePool(chain: chain)];
        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);
        Assert.Empty(result);
    }

    [Fact]
    public void ApplyBaseFilters_ExactMinTvl_Included()
    {
        // MIN_TVL is 1_000_000 — exactly at threshold should pass
        IReadOnlyList<RawPool> pools = [MakePool(tvlUsd: 1_000_000)];
        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);
        Assert.Single(result);
    }

    [Fact]
    public void ApplyBaseFilters_JustBelowMinTvl_Excluded()
    {
        IReadOnlyList<RawPool> pools = [MakePool(tvlUsd: 999_999)];
        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);
        Assert.Empty(result);
    }
}
