using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.ApiClients.GoPlus;
using Credfeto.Defi.Server.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class GoPlusClientTests : TestBase
{
    private static GoPlusClient CreateClient(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        ILogger<GoPlusClient> logger = GetSubstitute<ILogger<GoPlusClient>>();

        return new GoPlusClient(httpClientFactory: factory, logger: logger);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_UnknownChain_ReturnsEmptyMapAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);

        GoPlusClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, GoPlusTokenResult> result = await client.FetchTokenSecurityAsync(
            chain: "UnknownChain",
            addresses: ["0xabc"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_EmptyAddresses_ReturnsEmptyMapAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);

        GoPlusClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, GoPlusTokenResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: [],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_ValidRequest_ReturnsParsedDataAsync()
    {
        const string JSON =
            """{"code":1,"result":{"0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48":{"is_open_source":"1","is_honeypot":"0"}}}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        GoPlusClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, GoPlusTokenResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.True(
            result.ContainsKey("0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"),
            userMessage: "Result should contain the queried address"
        );
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_NullResult_ReturnsEmptyMapAsync()
    {
        const string JSON = """{"code":1,"result":null}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        GoPlusClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, GoPlusTokenResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_HttpError_ReturnsEmptyMapAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using HttpClient httpClient = new(handler);

        GoPlusClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, GoPlusTokenResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("Arbitrum")]
    [InlineData("Base")]
    [InlineData("BSC")]
    public async Task FetchTokenSecurityAsync_AllSupportedChains_CallsApiAsync(string chain)
    {
        const string JSON = """{"code":1,"result":{}}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        GoPlusClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, GoPlusTokenResult> result = await client.FetchTokenSecurityAsync(
            chain: chain,
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_MultipleAddresses_JoinsWithCommaAsync()
    {
        const string JSON = """{"code":1,"result":{}}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        GoPlusClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, GoPlusTokenResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48", "0xdac17f958d2ee523a2206206994597c13d831ec7"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
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
