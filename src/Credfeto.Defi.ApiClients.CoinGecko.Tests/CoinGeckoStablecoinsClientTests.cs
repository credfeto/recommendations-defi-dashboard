using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.CoinGecko;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace Credfeto.Defi.ApiClients.CoinGecko.Tests;

public sealed class CoinGeckoStablecoinsClientTests : TestBase
{
    private const int FULL_PAGE_SIZE = 250;

    private static CoinGeckoStablecoinsClient CreateClient(HttpClient httpClient)
    {
        return CreateClient(httpClient: httpClient, logger: GetSubstitute<ILogger<CoinGeckoStablecoinsClient>>());
    }

    private static CoinGeckoStablecoinsClient CreateClientWithLoggingEnabled(HttpClient httpClient)
    {
        return CreateClient(httpClient: httpClient, logger: CreateEnabledLogger());
    }

    private static string BuildStablecoinJson(int count)
    {
        StringBuilder sb = new();
        sb.Append('[');

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append("{\"id\":\"stablecoin-");
            sb.Append(i);
            sb.Append("\",\"symbol\":\"SC");
            sb.Append(i);
            sb.Append("\",\"name\":\"Stablecoin ");
            sb.Append(i);
            sb.Append("\",\"current_price\":1.0}");
        }

        sb.Append(']');

        return sb.ToString();
    }

    [Fact]
    public async Task FetchStablecoinsAsync_SinglePage_ReturnsParsedStablecoinsAsync()
    {
        const string JSON = """[{"id":"usd-coin","symbol":"USDC","name":"USD Coin","current_price":1.0}]""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Single(stablecoins);
        Assert.Equal(expected: "USDC", actual: stablecoins[0].Symbol);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_EmptyResponse_ReturnsEmptyListAsync()
    {
        const string JSON = "[]";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(stablecoins);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_NullResponse_ReturnsEmptyListAsync()
    {
        const string JSON = "null";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(stablecoins);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_HttpError_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(stablecoins);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_MultiPage_ReturnsCombinedStablecoinsAsync()
    {
        string firstPageJson = BuildStablecoinJson(FULL_PAGE_SIZE);
        const string secondPageJson =
            """[{"id":"last-stablecoin","symbol":"LAST","name":"Last Stablecoin","current_price":1.0}]""";

        using MultiResponseHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(firstPageJson, Encoding.UTF8, mediaType: "application/json"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(secondPageJson, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Equal(expected: FULL_PAGE_SIZE + 1, actual: stablecoins.Count);
    }

    [Fact]
    public async Task FetchCoinListAsync_SuccessResponse_ReturnsParsedCoinsAsync()
    {
        const string JSON =
            """[{"id":"usd-coin","symbol":"usdc","platforms":{"ethereum":"0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"}}]""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoCoinPlatforms> coins = await client.FetchCoinListAsync(this.CancellationToken());

        Assert.Single(coins);
        Assert.Equal(expected: "usd-coin", actual: coins[0].Id);
    }

    [Fact]
    public async Task FetchCoinListAsync_NullResponse_ReturnsEmptyListAsync()
    {
        const string JSON = "null";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoCoinPlatforms> coins = await client.FetchCoinListAsync(this.CancellationToken());

        Assert.Empty(coins);
    }

    [Fact]
    public async Task FetchCoinListAsync_HttpError_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoCoinPlatforms> coins = await client.FetchCoinListAsync(this.CancellationToken());

        Assert.Empty(coins);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_HttpErrorWithLoggingEnabled_LogsAndReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClientWithLoggingEnabled(httpClient);

        IReadOnlyList<CoinGeckoStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(stablecoins);
    }

    [Fact]
    public async Task FetchCoinListAsync_HttpErrorWithLoggingEnabled_LogsAndReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClientWithLoggingEnabled(httpClient);

        IReadOnlyList<CoinGeckoCoinPlatforms> coins = await client.FetchCoinListAsync(this.CancellationToken());

        Assert.Empty(coins);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_Forbidden_LogsStatusAndBodyAndReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("blocked") }
        );
        using HttpClient httpClient = new(handler);
        ILogger<CoinGeckoStablecoinsClient> logger = CreateEnabledLogger();

        CoinGeckoStablecoinsClient client = CreateClient(httpClient: httpClient, logger: logger);

        IReadOnlyList<CoinGeckoStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(stablecoins);
        IReadOnlyList<KeyValuePair<string, object?>> state = GetSingleWarningState(logger);
        Assert.Equal(expected: HttpStatusCode.Forbidden, actual: GetStateValue(state: state, key: "StatusCode"));
        Assert.Equal(expected: "blocked", actual: GetStateValue(state: state, key: "Body"));
    }

    [Fact]
    public async Task FetchStablecoinsAsync_ForbiddenOnLaterPage_KeepsEarlierPagesAsync()
    {
        string firstPageJson = BuildStablecoinJson(FULL_PAGE_SIZE);

        using MultiResponseHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(firstPageJson, Encoding.UTF8, mediaType: "application/json"),
            },
            new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("blocked") }
        );
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Equal(expected: FULL_PAGE_SIZE, actual: stablecoins.Count);
    }

    [Fact]
    public async Task FetchCoinListAsync_Forbidden_LogsStatusAndBodyAndReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("blocked") }
        );
        using HttpClient httpClient = new(handler);
        ILogger<CoinGeckoStablecoinsClient> logger = CreateEnabledLogger();

        CoinGeckoStablecoinsClient client = CreateClient(httpClient: httpClient, logger: logger);

        IReadOnlyList<CoinGeckoCoinPlatforms> coins = await client.FetchCoinListAsync(this.CancellationToken());

        Assert.Empty(coins);
        IReadOnlyList<KeyValuePair<string, object?>> state = GetSingleWarningState(logger);
        Assert.Equal(expected: HttpStatusCode.Forbidden, actual: GetStateValue(state: state, key: "StatusCode"));
        Assert.Equal(expected: "blocked", actual: GetStateValue(state: state, key: "Body"));
    }

    [Fact]
    public async Task FetchCoinListAsync_ForbiddenWithLongBody_LogsTruncatedBodyAsync()
    {
        string longBody = new('x', count: 2000);
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent(longBody) }
        );
        using HttpClient httpClient = new(handler);
        ILogger<CoinGeckoStablecoinsClient> logger = CreateEnabledLogger();

        CoinGeckoStablecoinsClient client = CreateClient(httpClient: httpClient, logger: logger);

        _ = await client.FetchCoinListAsync(this.CancellationToken());

        IReadOnlyList<KeyValuePair<string, object?>> state = GetSingleWarningState(logger);
        string? loggedBody = GetStateValue(state: state, key: "Body") as string;
        Assert.NotNull(loggedBody);
        Assert.Equal(expected: 512, actual: loggedBody.Length);
    }

    [Fact]
    public async Task FetchCoinListAsync_ForbiddenWithLoggingDisabled_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("blocked") }
        );
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoCoinPlatforms> coins = await client.FetchCoinListAsync(this.CancellationToken());

        Assert.Empty(coins);
    }

    private static ILogger<CoinGeckoStablecoinsClient> CreateEnabledLogger()
    {
        ILogger<CoinGeckoStablecoinsClient> logger = GetSubstitute<ILogger<CoinGeckoStablecoinsClient>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        return logger;
    }

    private static CoinGeckoStablecoinsClient CreateClient(
        HttpClient httpClient,
        ILogger<CoinGeckoStablecoinsClient> logger
    )
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        return new CoinGeckoStablecoinsClient(httpClientFactory: factory, logger: logger);
    }

    private static IReadOnlyList<KeyValuePair<string, object?>> GetSingleWarningState(
        ILogger<CoinGeckoStablecoinsClient> logger
    )
    {
        ICall call = Assert.Single(
            collection: logger.ReceivedCalls(),
            predicate: static c =>
                StringComparer.Ordinal.Equals(x: c.GetMethodInfo().Name, y: nameof(ILogger.Log))
                && c.GetArguments()[0] is LogLevel.Warning
        );

        return Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(call.GetArguments()[2]);
    }

    private static object? GetStateValue(IReadOnlyList<KeyValuePair<string, object?>> state, string key)
    {
        return Assert.Single(state, predicate: kvp => StringComparer.Ordinal.Equals(x: kvp.Key, y: key)).Value;
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpHandler(HttpResponseMessage response) => this._response = response;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(this._response);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._response.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class MultiResponseHttpHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public MultiResponseHttpHandler(params HttpResponseMessage[] responses)
        {
            this._responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(this._responses.Dequeue());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (HttpResponseMessage response in this._responses)
                {
                    response.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
