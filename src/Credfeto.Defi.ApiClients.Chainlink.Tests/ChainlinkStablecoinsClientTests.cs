using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.Chainlink;
using Credfeto.Defi.Data.Models.Config;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.ApiClients.Chainlink.Tests;

public sealed class ChainlinkStablecoinsClientTests : TestBase
{
    // answer = 100_000_000 (1.00 USD with 8 decimals), ABI-encoded as latestRoundData response
    private const string VALID_HEX_RESPONSE =
        """{"result":"0x00000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000005F5E10000000000000000000000000000000000000000000000000000000000650000000000000000000000000000000000000000000000000000000000000065000001000000000000000000000000000000000000000000000000000000000000000001"}""";

    private static ChainlinkStablecoinsClient CreateClient(
        HttpMessageHandler httpMessageHandler,
        string ethereumUrl = "https://eth-rpc.example.com"
    )
    {
        IOptions<RpcConfig> rpcOptions = Options.Create(new RpcConfig { Ethereum = ethereumUrl });
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(httpMessageHandler));
        ILogger<ChainlinkStablecoinsClient> logger = GetSubstitute<ILogger<ChainlinkStablecoinsClient>>();

        return new ChainlinkStablecoinsClient(rpcConfig: rpcOptions, httpClientFactory: factory, logger: logger);
    }

    private static ChainlinkStablecoinsClient CreateClientWithLoggingEnabled(
        HttpMessageHandler httpMessageHandler,
        string ethereumUrl = "https://eth-rpc.example.com"
    )
    {
        IOptions<RpcConfig> rpcOptions = Options.Create(new RpcConfig { Ethereum = ethereumUrl });
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(httpMessageHandler));
        ILogger<ChainlinkStablecoinsClient> logger = GetSubstitute<ILogger<ChainlinkStablecoinsClient>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        return new ChainlinkStablecoinsClient(rpcConfig: rpcOptions, httpClientFactory: factory, logger: logger);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_NoRpcUrl_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));

        ChainlinkStablecoinsClient client = CreateClient(handler, ethereumUrl: string.Empty);

        IReadOnlyList<ChainlinkPriceFeed> result = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_SuccessResponse_ReturnsParsedPricesAsync()
    {
        using FreshJsonResponseHandler handler = new(VALID_HEX_RESPONSE);

        ChainlinkStablecoinsClient client = CreateClient(handler);

        IReadOnlyList<ChainlinkPriceFeed> result = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.NotEmpty(result);

        foreach (ChainlinkPriceFeed feed in result)
        {
            Assert.Equal(expected: 1.0m, actual: feed.CurrentPrice);
        }
    }

    [Fact]
    public async Task FetchStablecoinsAsync_HttpError_ReturnsEmptyListAsync()
    {
        using FreshStatusHandler handler = new(HttpStatusCode.InternalServerError);

        ChainlinkStablecoinsClient client = CreateClient(handler);

        IReadOnlyList<ChainlinkPriceFeed> result = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_NullResult_SkipsFeedAsync()
    {
        const string NULL_RESULT_JSON = """{"result":null}""";
        using FreshJsonResponseHandler handler = new(NULL_RESULT_JSON);

        ChainlinkStablecoinsClient client = CreateClient(handler);

        IReadOnlyList<ChainlinkPriceFeed> result = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_NullJsonBody_SkipsFeedAsync()
    {
        using FreshJsonResponseHandler handler = new(json: "null");

        ChainlinkStablecoinsClient client = CreateClient(handler);

        IReadOnlyList<ChainlinkPriceFeed> result = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_ShortResponse_SkipsFeedAsync()
    {
        // Response hex is too short to contain answer field
        const string SHORT_RESULT_JSON = """{"result":"0x0000000000000000"}""";
        using FreshJsonResponseHandler handler = new(SHORT_RESULT_JSON);

        ChainlinkStablecoinsClient client = CreateClient(handler);

        IReadOnlyList<ChainlinkPriceFeed> result = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_InvalidHexInAnswer_SkipsFeedAsync()
    {
        // 'G' is not a valid hex character — BigInteger.TryParse will fail for the answer field
        // Response structure: roundId(64) + answer(64) + startedAt(64) + updatedAt(64) + answeredInRound(64) = 320 hex chars
        const string INVALID_HEX_JSON =
            """{"result":"0x0000000000000000000000000000000000000000000000000000000000000001GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000000001"}""";
        using FreshJsonResponseHandler handler = new(INVALID_HEX_JSON);

        ChainlinkStablecoinsClient client = CreateClient(handler);

        IReadOnlyList<ChainlinkPriceFeed> result = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_ZeroPrice_SkipsFeedAsync()
    {
        // answer = 0 (zero bytes in position 32-63)
        const string ZERO_ANSWER_JSON =
            """{"result":"0x00000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000000001"}""";
        using FreshJsonResponseHandler handler = new(ZERO_ANSWER_JSON);

        ChainlinkStablecoinsClient client = CreateClient(handler);

        IReadOnlyList<ChainlinkPriceFeed> result = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_HttpErrorWithLoggingEnabled_LogsAndSkipsFeedAsync()
    {
        using FreshStatusHandler handler = new(HttpStatusCode.InternalServerError);

        ChainlinkStablecoinsClient client = CreateClientWithLoggingEnabled(handler);

        IReadOnlyList<ChainlinkPriceFeed> result = await client.FetchStablecoinsAsync(this.CancellationToken());

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchStablecoinsAsync_ThrowingHandler_CatchesAndSkipsFeedAsync()
    {
        using ThrowingHttpHandler handler = new();

        ChainlinkStablecoinsClient client = CreateClientWithLoggingEnabled(handler);

        IReadOnlyList<ChainlinkPriceFeed> result = await client.FetchStablecoinsAsync(this.CancellationToken());

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

    private sealed class FreshJsonResponseHandler : HttpMessageHandler
    {
        private readonly string _json;

        public FreshJsonResponseHandler(string json) => this._json = json;

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

    private sealed class FreshStatusHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public FreshStatusHandler(HttpStatusCode statusCode) => this._statusCode = statusCode;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(new HttpResponseMessage(this._statusCode));
        }
    }

    private sealed class ThrowingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            throw new HttpRequestException(message: "Simulated network failure");
        }
    }
}
