using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.ApiClients.GoPlus;
using Credfeto.Defi.Server.Cache;
using Credfeto.Defi.Server.Config;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ContractSecurityServiceTests : TestBase, IDisposable
{
    private readonly string _tempDir;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ContractSecurityCacheService _cacheService;

    public ContractSecurityServiceTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        this._timeProvider = new FakeTimeProvider();
        IOptions<CacheConfig> options = Options.Create(new CacheConfig { DbDirectory = this._tempDir });
        this._cacheService = new ContractSecurityCacheService(config: options, timeProvider: this._timeProvider);
    }

    public void Dispose()
    {
        this._cacheService.Dispose();

        if (Directory.Exists(this._tempDir))
        {
            Directory.Delete(path: this._tempDir, recursive: true);
        }
    }

    private static GoPlusClient CreateGoPlusClient(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        ILogger<GoPlusClient> logger = GetSubstitute<ILogger<GoPlusClient>>();

        return new GoPlusClient(httpClientFactory: factory, logger: logger);
    }

    private static ProxyResolverService CreateNoOpProxyResolver()
    {
        IOptions<RpcConfig> options = Options.Create(new RpcConfig());
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        ILogger<ProxyResolverService> logger = GetSubstitute<ILogger<ProxyResolverService>>();

        return new ProxyResolverService(rpcConfig: options, httpClientFactory: factory, logger: logger);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_EmptyAddresses_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);

        GoPlusClient goPlusClient = CreateGoPlusClient(httpClient);
        ProxyResolverService proxyResolver = CreateNoOpProxyResolver();

        ContractSecurityService service = new(
            goPlusClient: goPlusClient,
            cache: this._cacheService,
            proxyResolver: proxyResolver
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_CacheHit_DoesNotCallGoPlusAsync()
    {
        const string ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";

        ContractSecurityInfo cachedInfo = new()
        {
            Chain = "Ethereum",
            Address = ADDRESS,
            IsOpenSource = 1.0,
            IsHoneypot = 0.0,
            IsProxy = 0.0,
        };

        await this._cacheService.SetAsync(info: cachedInfo, cancellationToken: this.CancellationToken());

        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using HttpClient httpClient = new(handler);

        GoPlusClient goPlusClient = CreateGoPlusClient(httpClient);
        ProxyResolverService proxyResolver = CreateNoOpProxyResolver();

        ContractSecurityService service = new(
            goPlusClient: goPlusClient,
            cache: this._cacheService,
            proxyResolver: proxyResolver
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Equal(expected: ADDRESS, actual: result[0].Address);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_CacheMiss_FetchesFromGoPlusAsync()
    {
        const string ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
        const string JSON =
            """{"code":1,"result":{"0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48":{"is_open_source":"1","is_honeypot":"0","is_proxy":"0"}}}""";

        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        GoPlusClient goPlusClient = CreateGoPlusClient(httpClient);
        ProxyResolverService proxyResolver = CreateNoOpProxyResolver();

        ContractSecurityService service = new(
            goPlusClient: goPlusClient,
            cache: this._cacheService,
            proxyResolver: proxyResolver
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Equal(expected: ADDRESS, actual: result[0].Address);
        Assert.Equal(expected: 1.0, actual: result[0].IsOpenSource);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_CachedProxyContract_IncludesChildrenAsync()
    {
        const string PROXY_ADDRESS = "0xproxy00000000000000000000000000000000aa";
        const string IMPL_ADDRESS = "0ximpl000000000000000000000000000000000bb";

        ContractSecurityInfo proxyInfo = new()
        {
            Chain = "Ethereum",
            Address = PROXY_ADDRESS,
            IsProxy = 1.0, // Is a proxy
        };

        ContractSecurityInfo implInfo = new()
        {
            Chain = "Ethereum",
            Address = IMPL_ADDRESS,
            ParentAddress = PROXY_ADDRESS,
            IsProxy = 0.0,
        };

        await this._cacheService.SetAsync(info: proxyInfo, cancellationToken: this.CancellationToken());
        await this._cacheService.SetAsync(info: implInfo, cancellationToken: this.CancellationToken());

        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);

        GoPlusClient goPlusClient = CreateGoPlusClient(httpClient);
        ProxyResolverService proxyResolver = CreateNoOpProxyResolver();

        ContractSecurityService service = new(
            goPlusClient: goPlusClient,
            cache: this._cacheService,
            proxyResolver: proxyResolver
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [PROXY_ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        // Should include both proxy and implementation
        Assert.Equal(expected: 2, actual: result.Count);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_GoPlusAddressNotFound_SkipsItAsync()
    {
        const string ADDRESS = "0xdeadbeef0000000000000000000000000000dead";

        // GoPlus returns nothing for this address
        const string JSON = """{"code":1,"result":{}}""";

        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        GoPlusClient goPlusClient = CreateGoPlusClient(httpClient);
        ProxyResolverService proxyResolver = CreateNoOpProxyResolver();

        ContractSecurityService service = new(
            goPlusClient: goPlusClient,
            cache: this._cacheService,
            proxyResolver: proxyResolver
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_ProxyContract_NoRpcConfigured_ImplNotResolvedAsync()
    {
        const string PROXY_ADDRESS = "0xc0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";

        // GoPlus returns is_proxy=1 for PROXY_ADDRESS
        string goPlusJson =
            "{\"code\":1,\"result\":{\""
            + PROXY_ADDRESS
            + "\":{\"is_open_source\":\"1\",\"is_honeypot\":\"0\",\"is_proxy\":\"1\"}}}";

        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(goPlusJson, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        GoPlusClient goPlusClient = CreateGoPlusClient(httpClient);

        // No-op proxy resolver: RpcConfig has no URL so ResolveProxyImplementationAsync returns null
        ProxyResolverService proxyResolver = CreateNoOpProxyResolver();

        ContractSecurityService service = new(
            goPlusClient: goPlusClient,
            cache: this._cacheService,
            proxyResolver: proxyResolver
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [PROXY_ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        // Should include the proxy itself, but no implementation (resolver returned null)
        Assert.Single(result);
        Assert.Equal(expected: PROXY_ADDRESS, actual: result[0].Address);
        Assert.Equal(expected: 1.0, actual: result[0].IsProxy);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_ProxyContract_GoPlusReturnsNoDataForImplAddress_OnlyProxyReturnedAsync()
    {
        const string PROXY_ADDRESS = "0xb0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";

        // GoPlus returns is_proxy=1 for PROXY_ADDRESS
        string goPlusJson1 =
            "{\"code\":1,\"result\":{\""
            + PROXY_ADDRESS
            + "\":{\"is_open_source\":\"1\",\"is_honeypot\":\"0\",\"is_proxy\":\"1\"}}}";
        // RPC returns implementation slot for the proxy
        const string IMPL_SLOT = "0x0000000000000000000000009999567890abcdef9999567890abcdef99995678";
        const string RPC_JSON = "{\"jsonrpc\":\"2.0\",\"result\":\"" + IMPL_SLOT + "\",\"id\":1}";
        // GoPlus returns an EMPTY result for the impl address (address not found)
        const string EMPTY_GOPLUS_JSON = "{\"code\":1,\"result\":{}}";

        using MultiResponseHttpHandler goPlusHandler = new([goPlusJson1, EMPTY_GOPLUS_JSON]);

        IHttpClientFactory goPlusFactory = GetSubstitute<IHttpClientFactory>();
        goPlusFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(goPlusHandler));
        GoPlusClient goPlusClient = new(
            httpClientFactory: goPlusFactory,
            logger: GetSubstitute<ILogger<GoPlusClient>>()
        );

        RpcConfig rpcConfig = new() { Ethereum = "https://rpc.example.com" };
        IOptions<RpcConfig> rpcOptions = Options.Create(rpcConfig);

        using MultiResponseHttpHandler rpcHandler = new([RPC_JSON, RPC_JSON, RPC_JSON]);
        IHttpClientFactory rpcFactory = GetSubstitute<IHttpClientFactory>();
        rpcFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(rpcHandler));

        ProxyResolverService proxyResolver = new(
            rpcConfig: rpcOptions,
            httpClientFactory: rpcFactory,
            logger: GetSubstitute<ILogger<ProxyResolverService>>()
        );

        ContractSecurityService service = new(
            goPlusClient: goPlusClient,
            cache: this._cacheService,
            proxyResolver: proxyResolver
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [PROXY_ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        // Should include the proxy, but no implementation (GoPlus returned empty for impl)
        Assert.Single(result);
        Assert.Equal(expected: PROXY_ADDRESS, actual: result[0].Address);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_ProxyContract_ResolvesImplementationAsync()
    {
        const string PROXY_ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
        const string IMPL_ADDRESS = "0x1234567890abcdef1234567890abcdef12345678";

        // GoPlus returns is_proxy=1 for PROXY_ADDRESS
        string goPlusJson1 =
            "{\"code\":1,\"result\":{\""
            + PROXY_ADDRESS
            + "\":{\"is_open_source\":\"1\",\"is_honeypot\":\"0\",\"is_proxy\":\"1\"}}}";
        // RPC returns implementation slot
        const string IMPL_SLOT = "0x0000000000000000000000001234567890abcdef1234567890abcdef12345678";
        const string RPC_JSON = "{\"jsonrpc\":\"2.0\",\"result\":\"" + IMPL_SLOT + "\",\"id\":1}";
        // GoPlus returns info for IMPL_ADDRESS (second GoPlus call, NOT interleaved with RPC calls)
        string goPlusJson2 =
            "{\"code\":1,\"result\":{\""
            + IMPL_ADDRESS
            + "\":{\"is_open_source\":\"1\",\"is_honeypot\":\"0\",\"is_proxy\":\"0\"}}}";

        using MultiResponseHttpHandler goPlusHandler = new([goPlusJson1, goPlusJson2]);

        IHttpClientFactory goPlusFactory = GetSubstitute<IHttpClientFactory>();
        goPlusFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(goPlusHandler));
        GoPlusClient goPlusClient = new(
            httpClientFactory: goPlusFactory,
            logger: GetSubstitute<ILogger<GoPlusClient>>()
        );

        RpcConfig rpcConfig = new() { Ethereum = "https://rpc.example.com" };
        IOptions<RpcConfig> rpcOptions = Options.Create(rpcConfig);

        using MultiResponseHttpHandler rpcHandler = new([RPC_JSON, RPC_JSON, RPC_JSON]);
        IHttpClientFactory rpcFactory = GetSubstitute<IHttpClientFactory>();
        rpcFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(rpcHandler));

        ProxyResolverService proxyResolver = new(
            rpcConfig: rpcOptions,
            httpClientFactory: rpcFactory,
            logger: GetSubstitute<ILogger<ProxyResolverService>>()
        );

        ContractSecurityService service = new(
            goPlusClient: goPlusClient,
            cache: this._cacheService,
            proxyResolver: proxyResolver
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [PROXY_ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        // Should include at least the proxy itself
        Assert.NotEmpty(result);
    }

    private sealed class MultiResponseHttpHandler : HttpMessageHandler
    {
        private readonly string[] _responses;
        private int _index;

        public MultiResponseHttpHandler(string[] responses)
        {
            this._responses = responses;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            string json =
                this._index < this._responses.Length ? this._responses[this._index++] : """{"code":1,"result":{}}""";

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, mediaType: "application/json"),
            };

            return Task.FromResult(response);
        }
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
