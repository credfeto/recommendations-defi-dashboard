using System.Collections.Generic;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class PoolFilterServiceTests : TestBase
{
    private static RawPool MakePool(
        string project = "aave",
        string chain = "Ethereum",
        string symbol = "USDC",
        double tvlUsd = 5_000_000,
        double apy = 5.0,
        bool stablecoin = false,
        string ilRisk = "no"
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
        };
    }

    [Fact]
    public void ApplyBaseFilters_ExcludesHighIlRisk()
    {
        IReadOnlyList<RawPool> pools = [MakePool(ilRisk: "yes")];
        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);
        Assert.Empty(result);
    }

    [Fact]
    public void ApplyBaseFilters_ExcludesLowTvl()
    {
        IReadOnlyList<RawPool> pools = [MakePool(tvlUsd: 500_000)];
        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);
        Assert.Empty(result);
    }

    [Fact]
    public void ApplyBaseFilters_ExcludesZeroApy()
    {
        IReadOnlyList<RawPool> pools = [MakePool(apy: 0)];
        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);
        Assert.Empty(result);
    }

    [Fact]
    public void ApplyBaseFilters_Excludes100PctApy()
    {
        IReadOnlyList<RawPool> pools = [MakePool(apy: 100)];
        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("Aptos")]
    [InlineData("Avalanche")]
    [InlineData("Tron")]
    [InlineData("Sui")]
    public void ApplyBaseFilters_ExcludesUnsupportedChains(string chain)
    {
        IReadOnlyList<RawPool> pools = [MakePool(chain: chain)];
        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);
        Assert.Empty(result);
    }

    [Fact]
    public void ApplyBaseFilters_PassesValidPool()
    {
        IReadOnlyList<RawPool> pools = [MakePool()];
        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);
        Assert.Single(result);
    }

    [Fact]
    public void FilterPoolsByType_Eth_MatchesEthSymbol()
    {
        IReadOnlyList<RawPool> pools = [MakePool(symbol: "ETH-STETH")];
        IReadOnlyList<RawPool> result = PoolFilterService.FilterPoolsByType(pools, "ETH");
        Assert.Single(result);
    }

    [Fact]
    public void FilterPoolsByType_Stables_MatchesStablecoin()
    {
        IReadOnlyList<RawPool> pools = [MakePool(stablecoin: true)];
        IReadOnlyList<RawPool> result = PoolFilterService.FilterPoolsByType(pools, "STABLES");
        Assert.Single(result);
    }

    [Fact]
    public void FilterPoolsByType_HighYield_MatchesHighApy()
    {
        IReadOnlyList<RawPool> pools = [MakePool(apy: 10.0)];
        IReadOnlyList<RawPool> result = PoolFilterService.FilterPoolsByType(pools, "HIGH_YIELD");
        Assert.Single(result);
    }

    [Fact]
    public void FilterPoolsByType_LowTvl_MatchesSmallPool()
    {
        IReadOnlyList<RawPool> pools = [MakePool(tvlUsd: 5_000_000)]; // < $10M
        IReadOnlyList<RawPool> result = PoolFilterService.FilterPoolsByType(pools, "LOW_TVL");
        Assert.Single(result);
    }

    [Fact]
    public void FilterPoolsByType_BlueChip_MatchesLargePool()
    {
        IReadOnlyList<RawPool> pools = [MakePool(tvlUsd: 500_000_000)]; // > $100M
        IReadOnlyList<RawPool> result = PoolFilterService.FilterPoolsByType(pools, "BLUE_CHIP");
        Assert.Single(result);
    }

    [Fact]
    public void FilterPoolsByType_InvalidType_ReturnsEmpty()
    {
        IReadOnlyList<RawPool> pools = [MakePool()];
        IReadOnlyList<RawPool> result = PoolFilterService.FilterPoolsByType(pools, "INVALID_TYPE");
        Assert.Empty(result);
    }

    [Fact]
    public void ApplyBaseFilters_SortsByApyDescending()
    {
        IReadOnlyList<RawPool> pools = [MakePool(apy: 3.0), MakePool(apy: 10.0), MakePool(apy: 7.0)];

        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);

        Assert.Equal(expected: 3, actual: result.Count);
        Assert.Equal(expected: 10.0, actual: result[0].Apy);
        Assert.Equal(expected: 7.0, actual: result[1].Apy);
        Assert.Equal(expected: 3.0, actual: result[2].Apy);
    }
}
