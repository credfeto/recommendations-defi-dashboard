using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.ApiClients.Pendle.Tests;

public sealed class PendleMarketsClientTests : TestBase
{
    private const int PAGE_LIMIT = 100;

    private static PendleMarketsClient CreateClientWithHandlerFactory(
        Func<HttpMessageHandler> handlerFactory,
        bool loggingEnabled = false
    )
    {
        // Each call to the factory must hand back a fresh handler, since
        // FetchMarketsForChainAsync disposes its HttpClient (and, by
        // default, the handler it wraps) once each chain has been fetched.
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handlerFactory()));
        ILogger<PendleMarketsClient> logger = GetSubstitute<ILogger<PendleMarketsClient>>();

        if (loggingEnabled)
        {
            logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        }

        return new PendleMarketsClient(httpClientFactory: factory, logger: logger);
    }

    private static string BuildMarketsPageJson(int total, int count)
    {
        string items = string.Join(
            separator: ',',
            values: Enumerable.Range(start: 0, count: count).Select(i => $$"""{"address":"0xitem{{i}}"}""")
        );

        return $$"""{"total":{{total}},"results":[{{items}}]}""";
    }

    [Fact]
    public async Task FetchMarketsAsync_EmptyResponseForAllChains_ReturnsEmptyListAsync()
    {
        const string JSON = """{"total":0,"results":[]}""";
        PendleMarketsClient client = CreateClientWithHandlerFactory(() => new FreshResponseHttpHandler(JSON));

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.Empty(markets);
    }

    [Fact]
    public async Task FetchMarketsAsync_ActiveMarket_IsIncludedAsync()
    {
        const string JSON =
            """{"total":1,"results":[{"address":"0xmarket1","chainId":1,"simpleSymbol":"PT-USDC","expiry":"2025-12-31","isActive":true,"aggregatedApy":0.05,"underlyingApy":0.03,"pendleApy":0.01,"lpRewardApy":0.005,"swapFeeApy":0.005,"liquidity":{"usd":1000000}}]}""";
        PendleMarketsClient client = CreateClientWithHandlerFactory(() => new FreshResponseHttpHandler(JSON));

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        // 4 chains are queried, each returns the same active market
        Assert.Equal(expected: 4, actual: markets.Count);
        Assert.Equal(expected: "0xmarket1", actual: markets[0].Address);
        Assert.True(markets[0].IsActive, userMessage: "Market should be marked as active");
    }

    [Fact]
    public async Task FetchMarketsAsync_InactiveMarket_IsIncludedAsync()
    {
        const string JSON =
            """{"total":1,"results":[{"address":"0xmarket1","chainId":1,"simpleSymbol":"PT-USDC","isActive":false,"aggregatedApy":0.05,"underlyingApy":0.03,"pendleApy":0.01,"lpRewardApy":0.005,"swapFeeApy":0.005}]}""";
        PendleMarketsClient client = CreateClientWithHandlerFactory(() => new FreshResponseHttpHandler(JSON));

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        // Raw fetch no longer filters by activity status; that is now the storage layer's responsibility
        Assert.NotEmpty(markets);
        Assert.False(markets[0].IsActive, userMessage: "Market should be marked as inactive");
    }

    [Fact]
    public async Task FetchMarketsAsync_HttpError_SkipsFailedChainsReturnsEmptyAsync()
    {
        PendleMarketsClient client = CreateClientWithHandlerFactory(() => new ErrorHttpHandler());

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.Empty(markets);
    }

    [Fact]
    public async Task FetchMarketsAsync_HttpErrorWithLoggingEnabled_LogsAndReturnsEmptyAsync()
    {
        PendleMarketsClient client = CreateClientWithHandlerFactory(() => new ErrorHttpHandler(), loggingEnabled: true);

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.Empty(markets);
    }

    [Fact]
    public async Task FetchMarketsAsync_ActiveMarketWithExpiry_PreservesRawExpiryAsync()
    {
        const string JSON =
            """{"total":1,"results":[{"address":"0xmarket1","chainId":1,"simpleSymbol":"PT-USDC","expiry":"2025-06-30T00:00:00Z","isActive":true,"aggregatedApy":0.08,"underlyingApy":0.05,"pendleApy":0.02,"lpRewardApy":0.005,"swapFeeApy":0.005}]}""";
        PendleMarketsClient client = CreateClientWithHandlerFactory(() => new FreshResponseHttpHandler(JSON));

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.NotEmpty(markets);
        Assert.Equal(expected: "2025-06-30T00:00:00Z", actual: markets[0].Expiry);
    }

    [Fact]
    public async Task FetchMarketsAsync_StableMarket_HasStablesCategoryAsync()
    {
        const string JSON =
            """{"total":1,"results":[{"address":"0xmarket1","chainId":1,"simpleSymbol":"PT-USDC","isActive":true,"categoryIds":["stables"],"aggregatedApy":0.05,"underlyingApy":0.03,"pendleApy":0.01,"lpRewardApy":0.005,"swapFeeApy":0.005}]}""";
        PendleMarketsClient client = CreateClientWithHandlerFactory(() => new FreshResponseHttpHandler(JSON));

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.NotEmpty(markets);
        Assert.NotNull(markets[0].CategoryIds);
        Assert.Contains(expected: "stables", collection: markets[0].CategoryIds!);
    }

    [Fact]
    public async Task FetchMarketsAsync_MarketWithTradingVolume_PreservesVolumeAsync()
    {
        const string JSON =
            """{"total":1,"results":[{"address":"0xmarket1","chainId":1,"simpleSymbol":"PT-USDC","isActive":true,"aggregatedApy":0.05,"underlyingApy":0.03,"pendleApy":0.01,"lpRewardApy":0.005,"swapFeeApy":0.005,"tradingVolume":{"usd":500}}]}""";
        PendleMarketsClient client = CreateClientWithHandlerFactory(() => new FreshResponseHttpHandler(JSON));

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.NotEmpty(markets);
        Assert.NotNull(markets[0].TradingVolume);
        Assert.Equal(expected: 500, actual: markets[0].TradingVolume!.Usd);
    }

    [Fact]
    public async Task FetchMarketsAsync_NullResultsResponse_ReturnsEmptyAsync()
    {
        const string JSON = """{"total":5,"results":null}""";
        PendleMarketsClient client = CreateClientWithHandlerFactory(() => new FreshResponseHttpHandler(JSON));

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.Empty(markets);
    }

    [Fact]
    public async Task FetchMarketsAsync_NullResponseBody_ReturnsEmptyAsync()
    {
        const string JSON = "null";
        PendleMarketsClient client = CreateClientWithHandlerFactory(() => new FreshResponseHttpHandler(JSON));

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        Assert.Empty(markets);
    }

    [Fact]
    public async Task FetchMarketsAsync_MultiPageResponse_ReturnsAllPagesAsync()
    {
        const int FIRST_PAGE_COUNT = PAGE_LIMIT;
        const int SECOND_PAGE_COUNT = 50;
        const int TOTAL = FIRST_PAGE_COUNT + SECOND_PAGE_COUNT;

        string firstPageJson = BuildMarketsPageJson(total: TOTAL, count: FIRST_PAGE_COUNT);
        string secondPageJson = BuildMarketsPageJson(total: TOTAL, count: SECOND_PAGE_COUNT);

        PendleMarketsClient client = CreateClientWithHandlerFactory(() =>
            new MultiResponseHttpHandler(firstPageJson, secondPageJson)
        );

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        // 4 chains queried, each paginating across two pages
        Assert.Equal(expected: 4 * TOTAL, actual: markets.Count);
    }

    [Fact]
    public async Task FetchMarketsAsync_OneChainFails_OtherChainsStillReturnedAsync()
    {
        const string JSON = """{"total":1,"results":[{"address":"0xmarket1","chainId":1,"isActive":true}]}""";

        int callCount = 0;
        PendleMarketsClient client = CreateClientWithHandlerFactory(() =>
            callCount++ == 0 ? new ErrorHttpHandler() : new FreshResponseHttpHandler(JSON)
        );

        IReadOnlyList<PendleMarket> markets = await client.FetchMarketsAsync(this.CancellationToken());

        // 3 of the 4 chains succeed; the failing chain is skipped rather than aborting the whole fetch
        Assert.Equal(expected: 3, actual: markets.Count);
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

    private sealed class MultiResponseHttpHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responses;

        public MultiResponseHttpHandler(params string[] responses) => this._responses = new(responses);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        this._responses.Dequeue(),
                        Encoding.UTF8,
                        mediaType: "application/json"
                    ),
                }
            );
        }
    }
}
