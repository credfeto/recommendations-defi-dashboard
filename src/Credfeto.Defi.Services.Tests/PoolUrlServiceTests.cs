using System;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Services.Tests;

public sealed class PoolUrlServiceTests : TestBase
{
    [Theory]
    [InlineData("Arbitrum", 42161)]
    [InlineData("ethereum", 1)]
    [InlineData("BASE", 8453)]
    [InlineData("Bsc", 56)]
    public static void GetPoolUrl_PendleKnownChainAnyCase_ReturnsPendleUrl(string chain, int chainId)
    {
        RawPool pool = new()
        {
            Project = "pendle",
            Chain = chain,
            PoolId = "0xabc",
        };

        Uri? result = PoolUrlService.GetPoolUrl(pool);

        Assert.NotNull(result);
        Assert.Equal(
            expected: $"https://app.pendle.finance/trade/markets/{chainId}/0xabc/pt",
            actual: result.AbsoluteUri
        );
    }

    [Fact]
    public void GetPoolUrl_PendleUnknownChain_ReturnsNull()
    {
        RawPool pool = new()
        {
            Project = "pendle",
            Chain = "Solana",
            PoolId = "0xabc",
        };

        Uri? result = PoolUrlService.GetPoolUrl(pool);

        Assert.Null(result);
    }

    [Fact]
    public void GetPoolUrl_NonPendlePool_ReturnsDefiLlamaUrl()
    {
        RawPool pool = new()
        {
            Project = "aave-v3",
            Chain = "Ethereum",
            PoolId = "pool-123",
        };

        Uri? result = PoolUrlService.GetPoolUrl(pool);

        Assert.NotNull(result);
        Assert.Equal(expected: "https://defillama.com/yields?pool=pool-123", actual: result.AbsoluteUri);
    }
}
