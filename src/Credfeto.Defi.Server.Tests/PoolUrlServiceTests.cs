using System;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class PoolUrlServiceTests : TestBase
{
    [Fact]
    public void GetPoolUrl_DefiLlamaPool_ReturnsDefiLlamaUrl()
    {
        RawPool pool = new()
        {
            Project = "aave-v3",
            PoolId = "abc123-uuid",
            Chain = "Ethereum",
            Symbol = "USDC",
            IlRisk = "no",
            Apy = 5.0,
            Stablecoin = false,
            Predictions = new RawPredictions(),
        };

        Uri? url = PoolUrlService.GetPoolUrl(pool);

        Assert.NotNull(url);
        Assert.Equal(expected: "https://defillama.com/yields?pool=abc123-uuid", actual: url.ToString());
    }

    [Fact]
    public void GetPoolUrl_PendlePool_ReturnsPendleUrl()
    {
        RawPool pool = new()
        {
            Project = "pendle",
            PoolId = "0xabc123def",
            Chain = "Ethereum",
            Symbol = "USDC-PT",
            IlRisk = "no",
            Apy = 10.0,
            Stablecoin = false,
            Predictions = new RawPredictions(),
        };

        Uri? url = PoolUrlService.GetPoolUrl(pool);

        Assert.NotNull(url);
        Assert.Contains(
            expectedSubstring: "app.pendle.finance",
            actualString: url.ToString(),
            comparisonType: System.StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "0xabc123def",
            actualString: url.ToString(),
            comparisonType: System.StringComparison.Ordinal
        );
    }

    [Fact]
    public void GetPoolUrl_PendlePool_UnknownChain_ReturnsNull()
    {
        RawPool pool = new()
        {
            Project = "pendle",
            PoolId = "0xabc123",
            Chain = "UnknownChain",
            Symbol = "TOKEN",
            IlRisk = "no",
            Apy = 5.0,
            Stablecoin = false,
            Predictions = new RawPredictions(),
        };

        Uri? url = PoolUrlService.GetPoolUrl(pool);

        Assert.Null(url);
    }

    [Theory]
    [InlineData("Ethereum", 1)]
    [InlineData("Arbitrum", 42161)]
    [InlineData("Base", 8453)]
    [InlineData("BSC", 56)]
    public void GetPoolUrl_PendlePool_IncludesChainId(string chain, int chainId)
    {
        RawPool pool = new()
        {
            Project = "pendle",
            PoolId = "0xmarketaddr",
            Chain = chain,
            Symbol = "SY-TOKEN",
            IlRisk = "no",
            Apy = 8.0,
            Stablecoin = false,
            Predictions = new RawPredictions(),
        };

        Uri? url = PoolUrlService.GetPoolUrl(pool);

        Assert.NotNull(url);
        Assert.Contains(
            expectedSubstring: chainId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            actualString: url.ToString(),
            comparisonType: System.StringComparison.Ordinal
        );
    }
}
