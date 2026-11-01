using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.ApiClients.Webacy.Tests;

public sealed class WebacyRiskClientTests : TestBase
{
    private const string USDC = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";

    private const string USDT = "0xdac17f958d2ee523a2206206994597c13d831ec7";

    private const string LOW_RISK_JSON = """{"overallRisk":10.5,"count":3,"medium":1,"high":0}""";

    private static WebacyRiskClient CreateClient(HttpClient httpClient, string apiKey = "test-key")
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        ILogger<WebacyRiskClient> logger = GetSubstitute<ILogger<WebacyRiskClient>>();

        return new WebacyRiskClient(
            httpClientFactory: factory,
            options: Options.Create(new WebacyOptions { ApiKey = apiKey }),
            logger: logger
        );
    }

    [Fact]
    public async Task FetchTokenRiskAsync_UnknownChain_ReturnsEmptyMapAsync()
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: LOW_RISK_JSON);
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: "UnknownChain",
            addresses: [USDC],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task FetchTokenRiskAsync_EmptyAddresses_ReturnsEmptyMapAsync()
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: LOW_RISK_JSON);
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: "Ethereum",
            addresses: [],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FetchTokenRiskAsync_NoApiKey_MakesNoRequestsAsync(string apiKey)
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: LOW_RISK_JSON);
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient: httpClient, apiKey: apiKey);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: "Ethereum",
            addresses: [USDC],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task FetchTokenRiskAsync_Success_ReturnsParsedDataAsync()
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: LOW_RISK_JSON);
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: "Ethereum",
            addresses: ["0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48"],
            cancellationToken: this.CancellationToken()
        );

        WebacyResult info = Assert.Single(result).Value;
        Assert.Equal(expected: 10.5, actual: info.RiskScore);
        Assert.Equal(expected: "Low", actual: info.RiskLevel);
        Assert.Equal(expected: 3, actual: info.IssueCount);
        Assert.Equal(expected: 0, actual: info.HighRiskIssueCount);
        Assert.Equal(expected: 1, actual: info.MediumRiskIssueCount);
        Assert.Equal(expected: USDC, actual: Assert.Single(result.Keys));
    }

    [Theory]
    [InlineData("Ethereum", "eth")]
    [InlineData("Arbitrum", "arb")]
    [InlineData("Base", "base")]
    [InlineData("BSC", "bsc")]
    [InlineData("Polygon", "pol")]
    [InlineData("Optimism", "opt")]
    [InlineData("ethereum", "eth")]
    public async Task FetchTokenRiskAsync_SupportedChain_RequestsAddressWithChainCodeAsync(
        string chain,
        string chainCode
    )
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: LOW_RISK_JSON);
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: chain,
            addresses: [USDC],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Uri? requested = Assert.Single(handler.RequestUris);
        Assert.Equal(
            expected: new Uri("https://api.webacy.com/addresses/" + USDC + "?chain=" + chainCode),
            actual: requested
        );
    }

    [Theory]
    [InlineData("0", "Low")]
    [InlineData("23", "Low")]
    [InlineData("23.1", "Medium")]
    [InlineData("50", "Medium")]
    [InlineData("50.1", "High")]
    [InlineData("100", "High")]
    public async Task FetchTokenRiskAsync_RiskScore_MapsToBandAsync(string score, string expectedLevel)
    {
        string json = "{\"overallRisk\":" + score + "}";
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: json);
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: "Ethereum",
            addresses: [USDC],
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: expectedLevel, actual: result[USDC].RiskLevel);
    }

    [Fact]
    public async Task FetchTokenRiskAsync_NoRiskScore_LeavesRiskLevelNullAsync()
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: """{"count":0}""");
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: "Ethereum",
            addresses: [USDC],
            cancellationToken: this.CancellationToken()
        );

        WebacyResult info = result[USDC];
        Assert.Null(info.RiskScore);
        Assert.Null(info.RiskLevel);
        Assert.Equal(expected: 0, actual: info.IssueCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task FetchTokenRiskAsync_NonOkStatus_OmitsAddressAsync(HttpStatusCode statusCode)
    {
        using FakeHttpHandler handler = new(statusCode: statusCode, json: LOW_RISK_JSON);
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: "Ethereum",
            addresses: [USDC],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task FetchTokenRiskAsync_NullJsonResponse_ReturnsEmptyMapAsync()
    {
        using FakeHttpHandler handler = new(statusCode: HttpStatusCode.OK, json: "null");
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: "Ethereum",
            addresses: [USDC],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchTokenRiskAsync_MalformedJson_OmitsAddressAndContinuesAsync()
    {
        using FakeHttpHandler handler = new((HttpStatusCode.OK, "not json"), (HttpStatusCode.OK, LOW_RISK_JSON));
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: "Ethereum",
            addresses: [USDC, USDT],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Contains(USDT, result.Keys);
    }

    [Fact]
    public async Task FetchTokenRiskAsync_OneAddressFails_StillReturnsTheOtherAsync()
    {
        using FakeHttpHandler handler = new((HttpStatusCode.TooManyRequests, null), (HttpStatusCode.OK, LOW_RISK_JSON));
        using HttpClient httpClient = new(handler);

        WebacyRiskClient client = CreateClient(httpClient);

        IReadOnlyDictionary<string, WebacyResult> result = await client.FetchTokenRiskAsync(
            chain: "Ethereum",
            addresses: [USDC, USDT],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Contains(USDT, result.Keys);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task FetchTokenRiskAsync_CancelledToken_PropagatesAndStopsRatherThanContinuingAsync()
    {
        using FakeHttpHandler handler = new((HttpStatusCode.OK, LOW_RISK_JSON), (HttpStatusCode.OK, LOW_RISK_JSON));
        using HttpClient httpClient = new(handler);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        WebacyRiskClient client = CreateClient(httpClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client
                .FetchTokenRiskAsync(chain: "Ethereum", addresses: [USDC, USDT], cancellationToken: cts.Token)
                .AsTask()
        );

        Assert.True(handler.RequestCount < 2, "should not have attempted the second address after cancellation");
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly List<Uri?> _requestUris = [];
        private readonly Queue<(HttpStatusCode StatusCode, string? Json)> _responses;

        public FakeHttpHandler(HttpStatusCode statusCode, string? json)
            : this([(statusCode, json)]) { }

        public FakeHttpHandler(params (HttpStatusCode StatusCode, string? Json)[] responses)
        {
            this._responses = new(responses);
        }

        public int RequestCount => this._requestUris.Count;

        public IReadOnlyList<Uri?> RequestUris => this._requestUris;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            this._requestUris.Add(request.RequestUri);

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
