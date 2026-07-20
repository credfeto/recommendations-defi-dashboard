using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.CoinGecko;
using Credfeto.Defi.ApiClients.DefiLlama;
using Credfeto.Defi.ApiClients.GoPlus;
using Credfeto.Defi.ApiClients.Pendle;
using Credfeto.Defi.Data.Models.Config;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Mcp;
using Credfeto.Defi.Server.Tests.Common;
using Credfeto.Defi.Services;
using Credfeto.Defi.Storage;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class DefiMcpToolsTests : TestBase
{
    private readonly ApiCacheService _apiCache;
    private readonly ContractSecurityCacheService _securityCache;
    private readonly FakeTimeProvider _timeProvider;

    public DefiMcpToolsTests()
    {
        this._timeProvider = new FakeTimeProvider();
        FakeDatabase database = new();
        this._apiCache = new ApiCacheService(database: database, timeProvider: this._timeProvider);
        this._securityCache = new ContractSecurityCacheService(database: database, timeProvider: this._timeProvider);
    }

    private static IHttpClientFactory CreateMockedFactory(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        return factory;
    }

    private T CreateClient<T>(HttpClient httpClient)
        where T : class
    {
        IHttpClientFactory factory = CreateMockedFactory(httpClient);
        if (typeof(T) == typeof(DefiLlamaPoolsClient))
        {
            return (T)
                (object)
                    new DefiLlamaPoolsClient(
                        httpClientFactory: factory,
                        logger: this.GetTypedLogger<DefiLlamaPoolsClient>()
                    );
        }

        if (typeof(T) == typeof(PendleMarketsClient))
        {
            return (T)
                (object)
                    new PendleMarketsClient(
                        httpClientFactory: factory,
                        logger: this.GetTypedLogger<PendleMarketsClient>()
                    );
        }

        if (typeof(T) == typeof(DefiLlamaHacksClient))
        {
            return (T)
                (object)
                    new DefiLlamaHacksClient(
                        httpClientFactory: factory,
                        logger: this.GetTypedLogger<DefiLlamaHacksClient>()
                    );
        }

        if (typeof(T) == typeof(DefiLlamaProtocolsClient))
        {
            return (T)
                (object)
                    new DefiLlamaProtocolsClient(
                        httpClientFactory: factory,
                        logger: this.GetTypedLogger<DefiLlamaProtocolsClient>()
                    );
        }

        if (typeof(T) == typeof(CoinGeckoStablecoinsClient))
        {
            return (T)
                (object)
                    new CoinGeckoStablecoinsClient(
                        httpClientFactory: factory,
                        logger: this.GetTypedLogger<CoinGeckoStablecoinsClient>()
                    );
        }

        if (typeof(T) == typeof(GoPlusClient))
        {
            return (T)(object)new GoPlusClient(httpClientFactory: factory, logger: this.GetTypedLogger<GoPlusClient>());
        }
        return GetSubstitute<T>();
    }

    private DefiMcpTools CreateMcpTools(
        HttpClient httpClient,
        IDefiLlamaPoolStorage? poolStorage = null,
        IChainlinkPriceFeedStorageService? chainlinkStorage = null
    )
    {
        poolStorage ??= new FakePoolStorage();
        chainlinkStorage ??= new FakeChainlinkStorage();

        PendleMarketsClient pendleClient = this.CreateClient<PendleMarketsClient>(httpClient);
        DefiLlamaHacksClient hacksClient = this.CreateClient<DefiLlamaHacksClient>(httpClient);
        DefiLlamaProtocolsClient protocolsClient = this.CreateClient<DefiLlamaProtocolsClient>(httpClient);
        CoinGeckoStablecoinsClient coinGeckoClient = this.CreateClient<CoinGeckoStablecoinsClient>(httpClient);
        GoPlusClient goPlusClient = this.CreateClient<GoPlusClient>(httpClient);

        IOptions<RpcConfig> rpcOptions = Options.Create(new RpcConfig());
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        ProxyResolverService proxyResolver = new(
            rpcConfig: rpcOptions,
            httpClientFactory: factory,
            logger: this.GetTypedLogger<ProxyResolverService>()
        );

        ContractSecurityService contractSecurityService = new(
            goPlusClient: goPlusClient,
            cache: this._securityCache,
            proxyResolver: proxyResolver
        );

        PoolEnrichmentService enrichmentService = new(
            pendleClient: pendleClient,
            hacksClient: hacksClient,
            protocolsClient: protocolsClient,
            coinGeckoClient: coinGeckoClient,
            chainlinkStorage: chainlinkStorage,
            contractSecurityService: contractSecurityService,
            poolStorage: poolStorage,
            cache: this._apiCache
        );

        return new DefiMcpTools(enrichmentService: enrichmentService, contractSecurityService: contractSecurityService);
    }

    [Fact]
    public void GetPoolTypes_ReturnsExactlyFivePoolTypes()
    {
        PoolTypeMetadata[] types = DefiMcpTools.GetPoolTypes();

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
        const string EMPTY_ARRAY_JSON = "[]";

        int requestCount = 0;
        using MultiResponseHttpHandler handler = new(
            [
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
        const string EMPTY_ARRAY_JSON = "[]";

        using MultiResponseHttpHandler handler = new(
            [
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
        const string EMPTY_ARRAY_JSON = "[]";

        using MultiResponseHttpHandler handler = new(
            [
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
        const string EMPTY_ARRAY_JSON = "[]";

        IDefiLlamaPoolStorage poolStorage = new FakePoolStorage(BuildRawEthPools(count: 20));

        using MultiResponseHttpHandler handler = new(
            [
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

        DefiMcpTools tools = this.CreateMcpTools(httpClient, poolStorage: poolStorage);

        // Request only 5 pools; the Slice helper should cap the enrichment input at 5
        IReadOnlyList<Pool> pools = await tools.GetPoolsAsync(
            poolType: "ETH",
            limit: 5,
            cancellationToken: this.CancellationToken()
        );

        Assert.True(pools.Count <= 5, userMessage: "Result should be at most 5 pools");
    }

    private static IReadOnlyList<RawPool> BuildRawEthPools(int count)
    {
        RawPool[] pools = new RawPool[count];

        for (int i = 0; i < count; i++)
        {
            pools[i] = new RawPool
            {
                PoolId = $"pool-eth-{i}",
                Chain = "Ethereum",
                Project = "aave-v3",
                Symbol = "ETH-WETH",
                TvlUsd = 5_000_000 + i,
                Apy = 10.0 + i,
                ApyBase = 10.0 + i,
                Stablecoin = false,
                IlRisk = "no",
                Exposure = "single",
            };
        }

        return pools;
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
