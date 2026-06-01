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
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class CoinGeckoStablecoinsClientTests : TestBase
{
    private static CoinGeckoStablecoinsClient CreateClient(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        ILogger<CoinGeckoStablecoinsClient> logger = GetSubstitute<ILogger<CoinGeckoStablecoinsClient>>();

        return new CoinGeckoStablecoinsClient(httpClientFactory: factory, logger: logger);
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
    public async Task FetchStablecoinsAsync_HttpError_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        using HttpClient httpClient = new(handler);

        CoinGeckoStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<CoinGeckoStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(stablecoins);
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
}
