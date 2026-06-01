using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.Config;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ProxyResolverServiceTests : TestBase
{
    private const string ZERO_HEX_64 = "0x" + "0000000000000000000000000000000000000000000000000000000000000000";

    private const string VALID_IMPL_SLOT = "0x0000000000000000000000001234567890abcdef1234567890abcdef12345678";

    private static ProxyResolverService CreateService(string chain, string rpcUrl, HttpClient httpClient)
    {
        RpcConfig config = new();

        if (string.Equals(a: chain, b: "Ethereum", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            config.Ethereum = rpcUrl;
        }
        else if (string.Equals(a: chain, b: "Arbitrum", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            config.Arbitrum = rpcUrl;
        }
        else if (string.Equals(a: chain, b: "Base", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            config.Base = rpcUrl;
        }
        else if (string.Equals(a: chain, b: "BSC", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            config.Bsc = rpcUrl;
        }

        IOptions<RpcConfig> options = Options.Create(config);
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        ILogger<ProxyResolverService> logger = GetSubstitute<ILogger<ProxyResolverService>>();

        return new ProxyResolverService(rpcConfig: options, httpClientFactory: factory, logger: logger);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_UnknownChain_ReturnsNullAsync()
    {
        using FakeHttpHandler handler = new(response: null!);
        using HttpClient httpClient = new(handler);

        ProxyResolverService service = CreateService(
            chain: "SomeUnknownChain",
            rpcUrl: "https://rpc.example.com",
            httpClient: httpClient
        );

        string? result = await service.ResolveProxyImplementationAsync(
            chain: "SomeUnknownChain",
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_EmptyRpcUrl_ReturnsNullAsync()
    {
        RpcConfig config = new(); // All RPC URLs empty
        IOptions<RpcConfig> options = Options.Create(config);
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        ILogger<ProxyResolverService> logger = GetSubstitute<ILogger<ProxyResolverService>>();

        ProxyResolverService service = new(rpcConfig: options, httpClientFactory: factory, logger: logger);

        string? result = await service.ResolveProxyImplementationAsync(
            chain: "Ethereum",
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Theory]
    [InlineData("Ethereum")]
    [InlineData("Arbitrum")]
    [InlineData("Base")]
    [InlineData("BSC")]
    public async Task ResolveProxyImplementationAsync_ValidChain_ValidSlot_ReturnsAddressAsync(string chain)
    {
        string responseJson = $$"""{"jsonrpc":"2.0","result":"{{VALID_IMPL_SLOT}}","id":1}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ProxyResolverService service = CreateService(
            chain: chain,
            rpcUrl: "https://rpc.example.com",
            httpClient: httpClient
        );

        string? result = await service.ResolveProxyImplementationAsync(
            chain: chain,
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.NotNull(result);
        Assert.StartsWith(expectedStartString: "0x", actualString: result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_AllSlotsZero_ReturnsNullAsync()
    {
        string responseJson = $$"""{"jsonrpc":"2.0","result":"{{ZERO_HEX_64}}","id":1}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ProxyResolverService service = CreateService(
            chain: "Ethereum",
            rpcUrl: "https://rpc.example.com",
            httpClient: httpClient
        );

        string? result = await service.ResolveProxyImplementationAsync(
            chain: "Ethereum",
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_HttpError_ReturnsNullAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using HttpClient httpClient = new(handler);

        ProxyResolverService service = CreateService(
            chain: "Ethereum",
            rpcUrl: "https://rpc.example.com",
            httpClient: httpClient
        );

        string? result = await service.ResolveProxyImplementationAsync(
            chain: "Ethereum",
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_EmptyResult_ReturnsNullAsync()
    {
        const string RESPONSE_JSON = """{"jsonrpc":"2.0","result":"","id":1}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RESPONSE_JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ProxyResolverService service = CreateService(
            chain: "Ethereum",
            rpcUrl: "https://rpc.example.com",
            httpClient: httpClient
        );

        string? result = await service.ResolveProxyImplementationAsync(
            chain: "Ethereum",
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_WrongHexLength_ReturnsNullAsync()
    {
        // 0x + 60 chars (not 64) — wrong length after stripping 0x
        const string RESPONSE_JSON =
            """{"jsonrpc":"2.0","result":"0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890ab","id":1}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RESPONSE_JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ProxyResolverService service = CreateService(
            chain: "Ethereum",
            rpcUrl: "https://rpc.example.com",
            httpClient: httpClient
        );

        string? result = await service.ResolveProxyImplementationAsync(
            chain: "Ethereum",
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_AllZerosAddress_ReturnsNullAsync()
    {
        // 0x + 24 zero-prefix + 40 zero address = all zeros 64-char value
        const string ALL_ZEROS_RESULT = "0x000000000000000000000000" + "0000000000000000000000000000000000000000";
        string properJson = $$"""{"jsonrpc":"2.0","result":"{{ALL_ZEROS_RESULT}}","id":1}""";

        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(properJson, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ProxyResolverService service = CreateService(
            chain: "Ethereum",
            rpcUrl: "https://rpc.example.com",
            httpClient: httpClient
        );

        string? result = await service.ResolveProxyImplementationAsync(
            chain: "Ethereum",
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_NonZeroPrefixButZeroAddress_ReturnsNullAsync()
    {
        // 0x + 24 non-zero prefix bytes + 40 zero address bytes (not all-64-zeros, so first check passes, but extracted address is zero)
        const string NON_ZERO_PREFIX_ZERO_ADDR =
            "0x123456789012345678901234" + "0000000000000000000000000000000000000000";
        string properJson = $$"""{"jsonrpc":"2.0","result":"{{NON_ZERO_PREFIX_ZERO_ADDR}}","id":1}""";

        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(properJson, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ProxyResolverService service = CreateService(
            chain: "Ethereum",
            rpcUrl: "https://rpc.example.com",
            httpClient: httpClient
        );

        string? result = await service.ResolveProxyImplementationAsync(
            chain: "Ethereum",
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_NetworkException_ReturnsNullAsync()
    {
        using ThrowingHttpHandler handler = new();
        using HttpClient httpClient = new(handler);

        ProxyResolverService service = CreateService(
            chain: "Ethereum",
            rpcUrl: "https://rpc.example.com",
            httpClient: httpClient
        );

        string? result = await service.ResolveProxyImplementationAsync(
            chain: "Ethereum",
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpHandler(HttpResponseMessage response)
        {
            this._response = response;
        }

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
                this._response?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class ThrowingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            throw new HttpRequestException("Network failure");
        }
    }
}
