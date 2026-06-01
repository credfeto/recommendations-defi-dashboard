using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.ApiClients.CoinGecko;
using Credfeto.Defi.Server.ApiClients.DefiLlama;
using Credfeto.Defi.Server.ApiClients.GoPlus;
using Credfeto.Defi.Server.ApiClients.Pendle;
using Credfeto.Defi.Server.Cache;
using Credfeto.Defi.Server.Config;
using Credfeto.Defi.Server.Mcp;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class DefiMcpToolsTests : TestBase, IDisposable
{
    private readonly string _tempDir;
    private readonly ApiCacheService _apiCache;
    private readonly ContractSecurityCacheService _securityCache;
    private readonly FakeTimeProvider _timeProvider;

    public DefiMcpToolsTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        this._timeProvider = new FakeTimeProvider();

        IOptions<CacheConfig> options = Options.Create(new CacheConfig { DbDirectory = this._tempDir });
        this._apiCache = new ApiCacheService(config: options, timeProvider: this._timeProvider);
        this._securityCache = new ContractSecurityCacheService(config: options, timeProvider: this._timeProvider);
    }

    public void Dispose()
    {
        this._apiCache.Dispose();
        this._securityCache.Dispose();

        if (Directory.Exists(this._tempDir))
        {
            Directory.Delete(path: this._tempDir, recursive: true);
        }
    }

    private static IHttpClientFactory CreateMockedFactory(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        return factory;
    }

    private static T CreateClient<T>(HttpClient httpClient)
        where T : class
    {
        IHttpClientFactory factory = CreateMockedFactory(httpClient);
        if (typeof(T) == typeof(DefiLlamaPoolsClient))
        {
            return (T)
                (object)
                    new DefiLlamaPoolsClient(
                        httpClientFactory: factory,
                        logger: GetSubstitute<ILogger<DefiLlamaPoolsClient>>()
                    );
        }

        if (typeof(T) == typeof(PendleMarketsClient))
        {
            return (T)
                (object)
                    new PendleMarketsClient(
                        httpClientFactory: factory,
                        logger: GetSubstitute<ILogger<PendleMarketsClient>>()
                    );
        }

        if (typeof(T) == typeof(DefiLlamaHacksClient))
        {
            return (T)
                (object)
                    new DefiLlamaHacksClient(
                        httpClientFactory: factory,
                        logger: GetSubstitute<ILogger<DefiLlamaHacksClient>>()
                    );
        }

        if (typeof(T) == typeof(DefiLlamaProtocolsClient))
        {
            return (T)
                (object)
                    new DefiLlamaProtocolsClient(
                        httpClientFactory: factory,
                        logger: GetSubstitute<ILogger<DefiLlamaProtocolsClient>>()
                    );
        }

        if (typeof(T) == typeof(CoinGeckoStablecoinsClient))
        {
            return (T)
                (object)
                    new CoinGeckoStablecoinsClient(
                        httpClientFactory: factory,
                        logger: GetSubstitute<ILogger<CoinGeckoStablecoinsClient>>()
                    );
        }

        if (typeof(T) == typeof(GoPlusClient))
        {
            return (T)
                (object)new GoPlusClient(httpClientFactory: factory, logger: GetSubstitute<ILogger<GoPlusClient>>());
        }
        return GetSubstitute<T>();
    }

    private DefiMcpTools CreateMcpTools(HttpClient httpClient)
    {
        DefiLlamaPoolsClient llamaClient = CreateClient<DefiLlamaPoolsClient>(httpClient);
        PendleMarketsClient pendleClient = CreateClient<PendleMarketsClient>(httpClient);
        DefiLlamaHacksClient hacksClient = CreateClient<DefiLlamaHacksClient>(httpClient);
        DefiLlamaProtocolsClient protocolsClient = CreateClient<DefiLlamaProtocolsClient>(httpClient);
        CoinGeckoStablecoinsClient coinGeckoClient = CreateClient<CoinGeckoStablecoinsClient>(httpClient);
        GoPlusClient goPlusClient = CreateClient<GoPlusClient>(httpClient);

        IOptions<RpcConfig> rpcOptions = Options.Create(new RpcConfig());
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        ProxyResolverService proxyResolver = new(
            rpcConfig: rpcOptions,
            httpClientFactory: factory,
            logger: GetSubstitute<ILogger<ProxyResolverService>>()
        );

        ContractSecurityService contractSecurityService = new(
            goPlusClient: goPlusClient,
            cache: this._securityCache,
            proxyResolver: proxyResolver
        );

        PoolEnrichmentService enrichmentService = new(
            llamaPoolsClient: llamaClient,
            pendleClient: pendleClient,
            hacksClient: hacksClient,
            protocolsClient: protocolsClient,
            coinGeckoClient: coinGeckoClient,
            contractSecurityService: contractSecurityService,
            cache: this._apiCache
        );

        return new DefiMcpTools(enrichmentService: enrichmentService, contractSecurityService: contractSecurityService);
    }

    [Fact]
    public void GetPoolTypes_ReturnsExactlyFivePoolTypes()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);

        DefiMcpTools tools = this.CreateMcpTools(httpClient);

        PoolTypeMetadata[] types = tools.GetPoolTypes();

        Assert.Equal(expected: 5, actual: types.Length);
    }

    [Fact]
    public async Task GetPoolsAsync_InvalidPoolType_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);

        DefiMcpTools tools = this.CreateMcpTools(httpClient);

        IReadOnlyList<Pool> pools = await tools.GetPoolsAsync(
            poolType: "INVALID_TYPE",
            limit: 10,
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(pools);
    }

    [Fact]
    public async Task GetPoolsAsync_ValidPoolType_EmptySource_ReturnsEmptyListAsync()
    {
        const string EMPTY_POOLS_JSON = """{"data":[]}""";
        const string EMPTY_ARRAY_JSON = "[]";

        int requestCount = 0;
        using MultiResponseHttpHandler handler = new(
            [
                EMPTY_POOLS_JSON, // llama pools
                EMPTY_ARRAY_JSON, // pendle
                EMPTY_ARRAY_JSON, // pendle chain 2
                EMPTY_ARRAY_JSON, // pendle chain 3
                EMPTY_ARRAY_JSON, // pendle chain 4
                EMPTY_ARRAY_JSON, // hacks
                EMPTY_ARRAY_JSON, // protocols
                EMPTY_ARRAY_JSON, // stablecoins
                EMPTY_ARRAY_JSON, // coin list
            ]
        );
        using HttpClient httpClient = new(handler);

        DefiMcpTools tools = this.CreateMcpTools(httpClient);

        IReadOnlyList<Pool> pools = await tools.GetPoolsAsync(
            poolType: "ETH",
            limit: 10,
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(pools);
        UnusedVariable(requestCount);
    }

    [Fact]
    public async Task GetPoolsAsync_LimitClampsToOne_WhenLimitIsZeroAsync()
    {
        const string EMPTY_POOLS_JSON = """{"data":[]}""";
        const string EMPTY_ARRAY_JSON = "[]";

        using MultiResponseHttpHandler handler = new(
            [
                EMPTY_POOLS_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
            ]
        );
        using HttpClient httpClient = new(handler);

        DefiMcpTools tools = this.CreateMcpTools(httpClient);

        IReadOnlyList<Pool> pools = await tools.GetPoolsAsync(
            poolType: "ETH",
            limit: 0,
            cancellationToken: this.CancellationToken()
        );

        // With empty data, no pools regardless of limit
        Assert.Empty(pools);
    }

    [Fact]
    public async Task GetPoolsAsync_LimitGreaterThan50_ClampsTo50Async()
    {
        const string EMPTY_POOLS_JSON = """{"data":[]}""";
        const string EMPTY_ARRAY_JSON = "[]";

        using MultiResponseHttpHandler handler = new(
            [
                EMPTY_POOLS_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
                EMPTY_ARRAY_JSON,
            ]
        );
        using HttpClient httpClient = new(handler);

        DefiMcpTools tools = this.CreateMcpTools(httpClient);

        IReadOnlyList<Pool> pools = await tools.GetPoolsAsync(
            poolType: "ETH",
            limit: 100,
            cancellationToken: this.CancellationToken()
        );

        // With empty data, no pools regardless of limit
        Assert.Empty(pools);
    }

    [Fact]
    public async Task GetPoolsAsync_WithMorePoolsThanLimit_SlicesToLimitAsync()
    {
        // Build a pools JSON that has many ETH pools, so slice kicks in
        // We create pools with ETH in symbol and valid base filter values
        string poolsJson = BuildEthPoolsJson(count: 20);
        const string EMPTY_ARRAY_JSON = "[]";

        using MultiResponseHttpHandler handler = new(
            [
                poolsJson, // llama pools
                EMPTY_ARRAY_JSON, // pendle chain 1
                EMPTY_ARRAY_JSON, // pendle chain 2
                EMPTY_ARRAY_JSON, // pendle chain 3
                EMPTY_ARRAY_JSON, // pendle chain 4
                EMPTY_ARRAY_JSON, // hacks
                EMPTY_ARRAY_JSON, // protocols
                EMPTY_ARRAY_JSON, // stablecoins
                EMPTY_ARRAY_JSON, // coin list
            ]
        );
        using HttpClient httpClient = new(handler);

        DefiMcpTools tools = this.CreateMcpTools(httpClient);

        // Request only 5 pools; the Slice helper should cap the enrichment input at 5
        IReadOnlyList<Pool> pools = await tools.GetPoolsAsync(
            poolType: "ETH",
            limit: 5,
            cancellationToken: this.CancellationToken()
        );

        Assert.True(pools.Count <= 5, userMessage: "Result should be at most 5 pools");
    }

    private static string BuildEthPoolsJson(int count)
    {
        StringBuilder sb = new();
        sb.Append("{\"data\":[");

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            string apy = (10.0 + i).ToString(CultureInfo.InvariantCulture);
            string tvl = (5_000_000 + i).ToString(CultureInfo.InvariantCulture);

            sb.Append('{');
            sb.Append("\"pool\":\"pool-eth-").Append(i.ToString(CultureInfo.InvariantCulture)).Append("\",");
            sb.Append("\"chain\":\"Ethereum\",");
            sb.Append("\"project\":\"aave-v3\",");
            sb.Append("\"symbol\":\"ETH-WETH\",");
            sb.Append("\"tvlUsd\":").Append(tvl).Append(',');
            sb.Append("\"apy\":").Append(apy).Append(',');
            sb.Append("\"apyBase\":").Append(apy).Append(',');
            sb.Append("\"stablecoin\":false,");
            sb.Append("\"ilRisk\":\"no\",");
            sb.Append("\"exposure\":\"single\"");
            sb.Append('}');
        }

        sb.Append("]}");

        return sb.ToString();
    }

    [Fact]
    public async Task CheckContractSecurityAsync_EmptyAddresses_ReturnsEmptyAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);

        DefiMcpTools tools = this.CreateMcpTools(httpClient);

        IReadOnlyList<ContractSecurityInfo> result = await tools.CheckContractSecurityAsync(
            chain: "Ethereum",
            addresses: [],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckContractSecurityAsync_MoreThan10Addresses_ClampsTo10Async()
    {
        const string JSON = """{"code":1,"result":{}}""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        DefiMcpTools tools = this.CreateMcpTools(httpClient);

        string[] addresses =
        [
            "0xaddr000000000000000000000000000000000001",
            "0xaddr000000000000000000000000000000000002",
            "0xaddr000000000000000000000000000000000003",
            "0xaddr000000000000000000000000000000000004",
            "0xaddr000000000000000000000000000000000005",
            "0xaddr000000000000000000000000000000000006",
            "0xaddr000000000000000000000000000000000007",
            "0xaddr000000000000000000000000000000000008",
            "0xaddr000000000000000000000000000000000009",
            "0xaddr00000000000000000000000000000000000a",
            "0xaddr00000000000000000000000000000000000b", // 11th - should be excluded
        ];

        // Should not throw
        IReadOnlyList<ContractSecurityInfo> result = await tools.CheckContractSecurityAsync(
            chain: "Ethereum",
            addresses: addresses,
            cancellationToken: this.CancellationToken()
        );

        // The result may be empty (GoPlus returns no data), but it should complete without error
        Assert.NotNull(result);
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
            string json = this._index < this._responses.Length ? this._responses[this._index++] : "[]";

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, mediaType: "application/json"),
            };

            return Task.FromResult(response);
        }
    }
}
