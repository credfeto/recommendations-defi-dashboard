using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Config;
using Credfeto.Defi.Services;
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

    private const string VALID_IMPL_ADDRESS = "0x1234567890abcdef1234567890abcdef12345678";

    private const string BEACON_SLOT_VALUE = "0x000000000000000000000000beac0000000000000000000000000000000beac0";

    private const string BEACON_ADDRESS = "0xbeac0000000000000000000000000000000beac0";

    private const string BEACON_IMPL_SLOT_VALUE = "0x000000000000000000000000AAAA00000000000000000000000000000000AAAA";

    private const string BEACON_IMPL_ADDRESS = "0xaaaa00000000000000000000000000000000aaaa";

    private const string LEGACY_SLOT_VALUE = "0x0000000000000000000000001e9ac70000000000000000000000000001e9ac70";

    private const string LEGACY_ADDRESS = "0x1e9ac70000000000000000000000000001e9ac70";

    private const string SLOT_EIP1967 = "0x360894a13ba1a3210667c828492db98dca3e2076cc3735a920a3ca505d382bbc";

    private const string SLOT_EIP1967_BEACON = "0xa3f0ad74e5423aebfd80d3ef4346578335a9a72aeaee59ff6cb3582b35133d50";

    private const string SLOT_OZ_LEGACY = "0x7050c9e0f4ca769c69bd3a8ef740bc37934f8e2c036e5a723fd8ee048ed3f8c3";

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

    [Theory]
    [InlineData("Arbitrum")]
    [InlineData("Base")]
    [InlineData("BSC")]
    public async Task ResolveProxyImplementationAsync_EmptyRpcUrlForChain_ReturnsNullAsync(string chain)
    {
        RpcConfig config = new();
        IOptions<RpcConfig> options = Options.Create(config);
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        ILogger<ProxyResolverService> logger = GetSubstitute<ILogger<ProxyResolverService>>();

        ProxyResolverService service = new(rpcConfig: options, httpClientFactory: factory, logger: logger);

        string? result = await service.ResolveProxyImplementationAsync(
            chain: chain,
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_NullJsonResponse_ReturnsNullAsync()
    {
        const string RESPONSE_JSON = "null";
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
    public async Task ResolveProxyImplementationAsync_BeaconSlot_ReturnsBeaconImplementationAsync()
    {
        using RoutingHttpHandler handler = new(
            eip1967: () => Ok(ZERO_HEX_64),
            beacon: () => Ok(BEACON_SLOT_VALUE),
            legacy: () => Ok(LEGACY_SLOT_VALUE),
            ethCall: () => Ok(BEACON_IMPL_SLOT_VALUE)
        );

        string? result = await this.ResolveEthereumAsync(handler);

        Assert.Equal(expected: BEACON_IMPL_ADDRESS, actual: result);
        Assert.NotEqual(expected: BEACON_ADDRESS, actual: result);
        string ethCallBody = Assert.Single(handler.EthCallBodies);
        Assert.Contains(
            expectedSubstring: "\"to\":\"" + BEACON_ADDRESS + "\"",
            actualString: ethCallBody,
            StringComparison.Ordinal
        );
        Assert.Contains(
            expectedSubstring: "\"data\":\"0x5c60da1b\"",
            actualString: ethCallBody,
            StringComparison.Ordinal
        );
        Assert.Contains(expectedSubstring: "\"latest\"", actualString: ethCallBody, StringComparison.Ordinal);
        Assert.Equal(expected: 2, actual: handler.StorageReadCount);
    }

    [Theory]
    [InlineData("zero")]
    [InlineData("malformed")]
    [InlineData("null-result")]
    [InlineData("http-error")]
    [InlineData("throws")]
    public async Task ResolveProxyImplementationAsync_BeaconCallFails_FallsThroughToLegacyAsync(string failure)
    {
        using RoutingHttpHandler handler = new(
            eip1967: () => Ok(ZERO_HEX_64),
            beacon: () => Ok(BEACON_SLOT_VALUE),
            legacy: () => Ok(LEGACY_SLOT_VALUE),
            ethCall: () => FailedBeaconCall(failure)
        );

        string? result = await this.ResolveEthereumAsync(handler);

        Assert.Equal(expected: LEGACY_ADDRESS, actual: result);
        Assert.Single(handler.EthCallBodies);
        Assert.Equal(expected: 3, actual: handler.StorageReadCount);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_BeaconCallReturnsZeroAndLegacyZero_ReturnsNullAsync()
    {
        using RoutingHttpHandler handler = new(
            eip1967: () => Ok(ZERO_HEX_64),
            beacon: () => Ok(BEACON_SLOT_VALUE),
            legacy: () => Ok(ZERO_HEX_64),
            ethCall: () => Ok(ZERO_HEX_64)
        );

        string? result = await this.ResolveEthereumAsync(handler);

        Assert.Null(result);
        Assert.Single(handler.EthCallBodies);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_Eip1967Slot_ReturnsImplementationWithoutEthCallAsync()
    {
        using RoutingHttpHandler handler = new(
            eip1967: () => Ok(VALID_IMPL_SLOT),
            beacon: () => Ok(BEACON_SLOT_VALUE),
            legacy: () => Ok(LEGACY_SLOT_VALUE),
            ethCall: () => Ok(BEACON_IMPL_SLOT_VALUE)
        );

        string? result = await this.ResolveEthereumAsync(handler);

        Assert.Equal(expected: VALID_IMPL_ADDRESS, actual: result);
        Assert.Empty(handler.EthCallBodies);
        Assert.Equal(expected: 1, actual: handler.StorageReadCount);
    }

    [Fact]
    public async Task ResolveProxyImplementationAsync_LegacySlot_ReturnsImplementationWithoutEthCallAsync()
    {
        using RoutingHttpHandler handler = new(
            eip1967: () => Ok(ZERO_HEX_64),
            beacon: () => Ok(ZERO_HEX_64),
            legacy: () => Ok(LEGACY_SLOT_VALUE),
            ethCall: () => Ok(BEACON_IMPL_SLOT_VALUE)
        );

        string? result = await this.ResolveEthereumAsync(handler);

        Assert.Equal(expected: LEGACY_ADDRESS, actual: result);
        Assert.Empty(handler.EthCallBodies);
        Assert.Equal(expected: 3, actual: handler.StorageReadCount);
    }

    private async Task<string?> ResolveEthereumAsync(RoutingHttpHandler handler)
    {
        IOptions<RpcConfig> options = Options.Create(new RpcConfig { Ethereum = "https://rpc.example.com" });
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();

        // The service disposes each client after one request, so every request needs a fresh client over the shared handler.
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler: handler, disposeHandler: false));
        ILogger<ProxyResolverService> logger = GetSubstitute<ILogger<ProxyResolverService>>();

        ProxyResolverService service = new(rpcConfig: options, httpClientFactory: factory, logger: logger);

        return await service.ResolveProxyImplementationAsync(
            chain: "Ethereum",
            proxyAddress: "0xabcdef1234567890abcdef1234567890abcdef12",
            cancellationToken: this.CancellationToken()
        );
    }

    private static HttpResponseMessage Ok(string result)
    {
        string responseJson = $$"""{"jsonrpc":"2.0","result":"{{result}}","id":1}""";

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, mediaType: "application/json"),
        };
    }

    private static HttpResponseMessage FailedBeaconCall(string failure)
    {
        return failure switch
        {
            "zero" => Ok(ZERO_HEX_64),
            "malformed" => Ok("0x1234"),
            "null-result" => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"jsonrpc":"2.0","result":null,"id":1}""",
                    Encoding.UTF8,
                    mediaType: "application/json"
                ),
            },
            "http-error" => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            "throws" => throw new HttpRequestException("Beacon call failed"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(failure),
                actualValue: failure,
                message: "Unknown failure"
            ),
        };
    }

    private sealed class RoutingHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _beacon;
        private readonly Func<HttpResponseMessage> _eip1967;
        private readonly List<string> _ethCallBodies = [];
        private readonly Func<HttpResponseMessage> _ethCall;
        private readonly Func<HttpResponseMessage> _legacy;

        public RoutingHttpHandler(
            Func<HttpResponseMessage> eip1967,
            Func<HttpResponseMessage> beacon,
            Func<HttpResponseMessage> legacy,
            Func<HttpResponseMessage> ethCall
        )
        {
            this._eip1967 = eip1967;
            this._beacon = beacon;
            this._legacy = legacy;
            this._ethCall = ethCall;
        }

        public IReadOnlyList<string> EthCallBodies => this._ethCallBodies;

        public int StorageReadCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            string body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            if (body.Contains(value: "\"eth_call\"", comparisonType: StringComparison.Ordinal))
            {
                this._ethCallBodies.Add(body);

                return this._ethCall();
            }

            ++this.StorageReadCount;

            if (body.Contains(value: SLOT_EIP1967, comparisonType: StringComparison.Ordinal))
            {
                return this._eip1967();
            }

            if (body.Contains(value: SLOT_EIP1967_BEACON, comparisonType: StringComparison.Ordinal))
            {
                return this._beacon();
            }

            if (body.Contains(value: SLOT_OZ_LEGACY, comparisonType: StringComparison.Ordinal))
            {
                return this._legacy();
            }

            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }
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
