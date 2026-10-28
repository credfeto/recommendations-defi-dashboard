using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.DefiLlama;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.ApiClients.DefiLlama.Tests;

public sealed class DefiLlamaStablecoinsClientTests : TestBase
{
    private static DefiLlamaStablecoinsClient CreateClient(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        ILogger<DefiLlamaStablecoinsClient> logger = GetSubstitute<ILogger<DefiLlamaStablecoinsClient>>();

        return new DefiLlamaStablecoinsClient(httpClientFactory: factory, logger: logger);
    }

    private static FakeHttpHandler CreateJsonHandler(string json)
    {
        return new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, mediaType: "application/json"),
            }
        );
    }

    [Fact]
    public async Task FetchStablecoinsAsync_SuccessResponse_ReturnsParsedStablecoinsAsync()
    {
        const string JSON =
            """{"peggedAssets":[{"id":"2","name":"USD Coin","symbol":"USDC","pegType":"peggedUSD","pegMechanism":"fiat-backed","price":0.9998,"circulating":{"peggedUSD":60000000000},"chainCirculating":{"Ethereum":{"current":{"peggedUSD":40000000000}}}},{"id":"3","name":"Algo Dollar","symbol":"ALGOUSD","pegType":"peggedUSD","pegMechanism":"algorithmic","price":null}]}""";
        using FakeHttpHandler handler = CreateJsonHandler(JSON);
        using HttpClient httpClient = new(handler);

        DefiLlamaStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<DefiLlamaStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Equal(expected: 2, actual: stablecoins.Count);

        DefiLlamaStablecoin usdc = stablecoins[0];
        Assert.Equal(expected: "USDC", actual: usdc.Symbol);
        Assert.Equal(expected: "peggedUSD", actual: usdc.PegType);
        Assert.Equal(expected: "fiat-backed", actual: usdc.PegMechanism);
        Assert.Equal(expected: 0.9998m, actual: usdc.Price);
        Assert.NotNull(usdc.Circulating);
        Assert.Equal(expected: 60000000000d, actual: usdc.Circulating["peggedUSD"]);
        Assert.NotNull(usdc.ChainCirculating);
        Assert.Equal(expected: 40000000000d, actual: usdc.ChainCirculating["Ethereum"].Current?["peggedUSD"]);

        DefiLlamaStablecoin algo = stablecoins[1];
        Assert.Equal(expected: "algorithmic", actual: algo.PegMechanism);
        Assert.Null(algo.Price);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_NullPeggedAssetsInResponse_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = CreateJsonHandler("""{"peggedAssets":null}""");
        using HttpClient httpClient = new(handler);

        DefiLlamaStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<DefiLlamaStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(stablecoins);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_NullResponse_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = CreateJsonHandler("null");
        using HttpClient httpClient = new(handler);

        DefiLlamaStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<DefiLlamaStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(stablecoins);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_HttpError_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using HttpClient httpClient = new(handler);

        DefiLlamaStablecoinsClient client = CreateClient(httpClient);

        IReadOnlyList<DefiLlamaStablecoin> stablecoins = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(stablecoins);
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
