using System.Collections.Generic;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Services.Tests;

public sealed class PoolFilterServiceTests : TestBase
{
    [Theory]
    [InlineData("Tron")]
    [InlineData("TRON")]
    [InlineData("sui")]
    public static void ApplyBaseFilters_ExcludedChainAnyCase_IsRemoved(string chain)
    {
        IReadOnlyList<RawPool> pools = [CreatePool(poolId: "excluded", chain: chain)];

        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyBaseFilters_AllowedChain_IsKept()
    {
        IReadOnlyList<RawPool> pools = [CreatePool(poolId: "allowed", chain: "Ethereum")];

        IReadOnlyList<RawPool> result = PoolFilterService.ApplyBaseFilters(pools);

        RawPool pool = Assert.Single(result);
        Assert.Equal(expected: "allowed", actual: pool.PoolId);
    }

    private static RawPool CreatePool(string poolId, string chain)
    {
        return new RawPool
        {
            PoolId = poolId,
            Chain = chain,
            Project = "aave-v3",
            Symbol = "USDC",
            TvlUsd = 5_000_000,
            Apy = 5,
            IlRisk = "no",
        };
    }
}
