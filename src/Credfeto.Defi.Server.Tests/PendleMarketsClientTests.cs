using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.Pendle;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class PendleMarketsClientTests : TestBase
{
    private static PendleMarketsClient CreateClientWithHandler(HttpMessageHandler handler)
    {
        // Create a new HttpClient each time the factory is invoked so that
        // each call to FetchMarketsForChainAsync gets a fresh, undisposed client.
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));
        ILogger<PendleMarketsClient> logger = GetSubstitute<ILogger<PendleMarketsClient>>();

        return new PendleMarketsClient(httpClientFactory: factory, logger: logger);
    }

    [Fact]
    public async Task FetchMarketsAsync_EmptyResponseForAllChains_ReturnsEmptyListAsync()
    {
        const string JSON = """{"total":0,"results":[]}""";
        using FreshResponseHttpHandler handler = new(JSON);

        PendleMarketsClient client = CreateClientWithHandler(handler);

        IReadOnlyList<RawPool> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.Empty(markets);
    }

    [Fact]
    public async Task FetchMarketsAsync_ActiveMarket_IsIncludedAsync()
    {
        const string JSON =
            """{"total":1,"results":[{"address":"0xmarket1","chainId":1,"simpleSymbol":"PT-USDC","expiry":"2025-12-31","isActive":true,"aggregatedApy":0.05,"underlyingApy":0.03,"pendleApy":0.01,"lpRewardApy":0.005,"swapFeeApy":0.005,"liquidity":{"usd":1000000}}]}""";
        using FreshResponseHttpHandler handler = new(JSON);

        PendleMarketsClient client = CreateClientWithHandler(handler);

        IReadOnlyList<RawPool> markets = await client.FetchMarketsAsync(this.CancellationToken());

        // 4 chains are queried, each returns the same active market
        Assert.Equal(expected: 4, actual: markets.Count);
        Assert.Equal(expected: "pendle", actual: markets[0].Project);
    }

    [Fact]
    public async Task FetchMarketsAsync_InactiveMarket_IsExcludedAsync()
    {
        const string JSON =
            """{"total":1,"results":[{"address":"0xmarket1","chainId":1,"simpleSymbol":"PT-USDC","isActive":false,"aggregatedApy":0.05,"underlyingApy":0.03,"pendleApy":0.01,"lpRewardApy":0.005,"swapFeeApy":0.005}]}""";
        using FreshResponseHttpHandler handler = new(JSON);

        PendleMarketsClient client = CreateClientWithHandler(handler);

        IReadOnlyList<RawPool> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.Empty(markets);
    }

    [Fact]
    public async Task FetchMarketsAsync_HttpError_SkipsFailedChainsReturnsEmptyAsync()
    {
        using ErrorHttpHandler handler = new();

        PendleMarketsClient client = CreateClientWithHandler(handler);

        IReadOnlyList<RawPool> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.Empty(markets);
    }

    [Fact]
    public async Task FetchMarketsAsync_ActiveMarketWithExpiry_HasMaturityPoolMetaAsync()
    {
        const string JSON =
            """{"total":1,"results":[{"address":"0xmarket1","chainId":1,"simpleSymbol":"PT-USDC","expiry":"2025-06-30T00:00:00Z","isActive":true,"aggregatedApy":0.08,"underlyingApy":0.05,"pendleApy":0.02,"lpRewardApy":0.005,"swapFeeApy":0.005}]}""";
        using FreshResponseHttpHandler handler = new(JSON);

        PendleMarketsClient client = CreateClientWithHandler(handler);

        IReadOnlyList<RawPool> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.NotEmpty(markets);
        Assert.NotNull(markets[0].PoolMeta);
        Assert.Contains(
            expectedSubstring: "Maturity",
            actualString: markets[0].PoolMeta!,
            System.StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public async Task FetchMarketsAsync_StableMarket_HasStablecoinFlagAsync()
    {
        const string JSON =
            """{"total":1,"results":[{"address":"0xmarket1","chainId":1,"simpleSymbol":"PT-USDC","isActive":true,"categoryIds":["stables"],"aggregatedApy":0.05,"underlyingApy":0.03,"pendleApy":0.01,"lpRewardApy":0.005,"swapFeeApy":0.005}]}""";
        using FreshResponseHttpHandler handler = new(JSON);

        PendleMarketsClient client = CreateClientWithHandler(handler);

        IReadOnlyList<RawPool> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.NotEmpty(markets);
        Assert.True(
            markets[0].Stablecoin,
            userMessage: "Market with 'stables' category should be marked as stablecoin"
        );
    }

    [Fact]
    public async Task FetchMarketsAsync_NullResultsResponse_ReturnsEmptyAsync()
    {
        const string JSON = """{"total":5,"results":null}""";
        using FreshResponseHttpHandler handler = new(JSON);

        PendleMarketsClient client = CreateClientWithHandler(handler);

        IReadOnlyList<RawPool> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.Empty(markets);
    }

    private sealed class ErrorHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    /// <summary>
    ///     Returns a fresh <see cref="HttpResponseMessage" /> with the given JSON body on every request,
    ///     preventing content-stream reuse issues when the same handler is called multiple times.
    /// </summary>
    private sealed class FreshResponseHttpHandler : HttpMessageHandler
    {
        private readonly string _json;

        public FreshResponseHttpHandler(string json) => this._json = json;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(this._json, Encoding.UTF8, mediaType: "application/json"),
                }
            );
        }
    }
}
