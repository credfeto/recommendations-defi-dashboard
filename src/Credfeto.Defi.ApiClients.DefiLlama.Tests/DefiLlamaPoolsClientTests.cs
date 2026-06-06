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

public sealed class DefiLlamaPoolsClientTests : TestBase
{
    private static DefiLlamaPoolsClient CreateClient(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        ILogger<DefiLlamaPoolsClient> logger = GetSubstitute<ILogger<DefiLlamaPoolsClient>>();

        return new DefiLlamaPoolsClient(httpClientFactory: factory, logger: logger);
    }

    [Fact]
    public async Task FetchPoolsAsync_SuccessResponse_ReturnsParsedPoolsAsync()
    {
        const string JSON =
            """{"data":[{"pool":"id1","chain":"Ethereum","project":"aave-v3","symbol":"USDC","tvlUsd":1000000,"apy":5.0,"stablecoin":true,"ilRisk":"no","predictions":{}}]}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        DefiLlamaPoolsClient client = CreateClient(httpClient);

        IReadOnlyList<RawPool> pools = await client.FetchPoolsAsync(this.CancellationToken());

        Assert.Single(pools);
        Assert.Equal(expected: "aave-v3", actual: pools[0].Project);
    }

    [Fact]
    public async Task FetchPoolsAsync_PendlePoolsExcluded_NotReturnedAsync()
    {
        const string JSON =
            """{"data":[{"pool":"id1","chain":"Ethereum","project":"pendle","symbol":"PT","tvlUsd":1000000,"apy":5.0,"stablecoin":false,"ilRisk":"no","predictions":{}},{"pool":"id2","chain":"Ethereum","project":"aave-v3","symbol":"USDC","tvlUsd":1000000,"apy":5.0,"stablecoin":true,"ilRisk":"no","predictions":{}}]}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        DefiLlamaPoolsClient client = CreateClient(httpClient);

        IReadOnlyList<RawPool> pools = await client.FetchPoolsAsync(this.CancellationToken());

        Assert.Single(pools);
        Assert.Equal(expected: "aave-v3", actual: pools[0].Project);
    }

    [Fact]
    public async Task FetchPoolsAsync_NullDataInResponse_ReturnsEmptyListAsync()
    {
        const string JSON = """{"data":null}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        DefiLlamaPoolsClient client = CreateClient(httpClient);

        IReadOnlyList<RawPool> pools = await client.FetchPoolsAsync(this.CancellationToken());

        Assert.Empty(pools);
    }

    [Fact]
    public async Task FetchPoolsAsync_NullResponse_ReturnsEmptyListAsync()
    {
        const string JSON = "null";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        DefiLlamaPoolsClient client = CreateClient(httpClient);

        IReadOnlyList<RawPool> pools = await client.FetchPoolsAsync(this.CancellationToken());

        Assert.Empty(pools);
    }

    [Fact]
    public async Task FetchPoolsAsync_HttpError_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using HttpClient httpClient = new(handler);

        DefiLlamaPoolsClient client = CreateClient(httpClient);

        IReadOnlyList<RawPool> pools = await client.FetchPoolsAsync(this.CancellationToken());

        Assert.Empty(pools);
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
