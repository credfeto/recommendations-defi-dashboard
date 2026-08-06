using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.HoneypotIs;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.ApiClients.HoneypotIs.Tests;

public sealed class HoneypotIsClientTests : TestBase
{
    private static HoneypotIsClient CreateClient(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        ILogger<HoneypotIsClient> logger = GetSubstitute<ILogger<HoneypotIsClient>>();

        return new HoneypotIsClient(httpClientFactory: factory, logger: logger);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_UnknownChain_ReturnsEmptyMapAsync()
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: null);
        using HttpClient httpClient = new(handler);

        HoneypotIsClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, HoneypotIsResult> result = await client.FetchTokenSecurityAsync(
            chain: "UnknownChain",
            addresses: ["0xabc"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_EmptyAddresses_ReturnsEmptyMapAsync()
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: null);
        using HttpClient httpClient = new(handler);

        HoneypotIsClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, HoneypotIsResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: [],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_HoneypotDetected_ReturnsParsedDataAsync()
    {
        const string JSON =
            """{"simulationSuccess":true,"honeypotResult":{"isHoneypot":true},"simulationResult":{"buyTax":5.0,"sellTax":99.0}}""";
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: JSON);
        using HttpClient httpClient = new(handler);

        HoneypotIsClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, HoneypotIsResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        HoneypotIsResult info = result["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"];
        Assert.True(info.IsHoneypot);
        Assert.Equal(5.0, info.BuyTax);
        Assert.Equal(99.0, info.SellTax);
        Assert.True(info.SimulationSuccess);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_NotHoneypot_ReturnsParsedDataAsync()
    {
        const string JSON =
            """{"simulationSuccess":true,"honeypotResult":{"isHoneypot":false},"simulationResult":{"buyTax":1.0,"sellTax":1.0}}""";
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: JSON);
        using HttpClient httpClient = new(handler);

        HoneypotIsClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, HoneypotIsResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.False(result["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"].IsHoneypot);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_NullJsonResponse_ReturnsEmptyMapAsync()
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: "null");
        using HttpClient httpClient = new(handler);

        HoneypotIsClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, HoneypotIsResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_HttpError_ReturnsEmptyMapAsync()
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.InternalServerError, json: null);
        using HttpClient httpClient = new(handler);

        HoneypotIsClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, HoneypotIsResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("Ethereum")]
    [InlineData("Base")]
    [InlineData("BSC")]
    public async Task FetchTokenSecurityAsync_AllSupportedChains_CallsApiAsync(string chain)
    {
        const string JSON = """{"simulationSuccess":true,"honeypotResult":{"isHoneypot":false}}""";
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: JSON);
        using HttpClient httpClient = new(handler);

        HoneypotIsClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, HoneypotIsResult> result = await client.FetchTokenSecurityAsync(
            chain: chain,
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_MultipleAddresses_QueriesEachIndividuallyAsync()
    {
        const string JSON = """{"simulationSuccess":true,"honeypotResult":{"isHoneypot":false}}""";
        using FakeHttpHandler handler = new(
            firstStatusCode: HttpStatusCode.OK,
            firstJson: JSON,
            secondStatusCode: HttpStatusCode.OK,
            secondJson: JSON
        );
        using HttpClient httpClient = new(handler);

        HoneypotIsClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, HoneypotIsResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48", "0xdac17f958d2ee523a2206206994597c13d831ec7"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(2, result.Count);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task FetchTokenSecurityAsync_OneAddressFails_StillReturnsTheOtherAsync()
    {
        const string JSON = """{"simulationSuccess":true,"honeypotResult":{"isHoneypot":false}}""";
        using FakeHttpHandler handler = new(
            firstStatusCode: HttpStatusCode.InternalServerError,
            firstJson: null,
            secondStatusCode: HttpStatusCode.OK,
            secondJson: JSON
        );
        using HttpClient httpClient = new(handler);

        HoneypotIsClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, HoneypotIsResult> result = await client.FetchTokenSecurityAsync(
            chain: "Ethereum",
            addresses: ["0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48", "0xdac17f958d2ee523a2206206994597c13d831ec7"],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Contains("0xdac17f958d2ee523a2206206994597c13d831ec7", result);
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode StatusCode, string? Json)> _responses;

        public FakeHttpHandler(HttpStatusCode statusCode, string? json)
            : this([(statusCode, json)]) { }

        public FakeHttpHandler(
            HttpStatusCode firstStatusCode,
            string? firstJson,
            HttpStatusCode secondStatusCode,
            string? secondJson
        )
            : this([(firstStatusCode, firstJson), (secondStatusCode, secondJson)]) { }

        private FakeHttpHandler(IEnumerable<(HttpStatusCode StatusCode, string? Json)> responses)
        {
            this._responses = new(responses);
        }

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            this.RequestCount++;

            (HttpStatusCode statusCode, string? json) = this._responses.Dequeue();

            HttpResponseMessage response = new(statusCode);

            if (json is not null)
            {
                response.Content = new StringContent(json, Encoding.UTF8, mediaType: "application/json");
            }

            return Task.FromResult(response);
        }
    }
}
