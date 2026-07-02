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
using Credfeto.Defi.Services;
using Credfeto.Defi.Storage;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class PoolEnrichmentServiceTests : TestBase
{
    private readonly ApiCacheService _apiCache;
    private readonly ContractSecurityCacheService _securityCache;
    private readonly FakeTimeProvider _timeProvider;

    public PoolEnrichmentServiceTests()
    {
        this._timeProvider = new FakeTimeProvider();
        FakeDatabase database = new();
        this._apiCache = new ApiCacheService(database: database, timeProvider: this._timeProvider);
        this._securityCache = new ContractSecurityCacheService(database: database, timeProvider: this._timeProvider);
    }

    private PoolEnrichmentService CreateEnrichmentService(
        HttpMessageHandler httpHandler,
        IDefiLlamaPoolStorage? poolStorage = null,
        IChainlinkPriceFeedStorageService? chainlinkStorage = null
    )
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(httpHandler));

        poolStorage ??= new FakePoolStorage();
        chainlinkStorage ??= new FakeChainlinkStorage();

        PendleMarketsClient pendleClient = new(
            httpClientFactory: factory,
            logger: this.GetTypedLogger<PendleMarketsClient>()
        );
        DefiLlamaHacksClient hacksClient = new(
            httpClientFactory: factory,
            logger: this.GetTypedLogger<DefiLlamaHacksClient>()
        );
        DefiLlamaProtocolsClient protocolsClient = new(
            httpClientFactory: factory,
            logger: this.GetTypedLogger<DefiLlamaProtocolsClient>()
        );
        CoinGeckoStablecoinsClient coinGeckoClient = new(
            httpClientFactory: factory,
            logger: this.GetTypedLogger<CoinGeckoStablecoinsClient>()
        );
        GoPlusClient goPlusClient = new(httpClientFactory: factory, logger: this.GetTypedLogger<GoPlusClient>());

        IOptions<RpcConfig> rpcOptions = Options.Create(new RpcConfig());
        ProxyResolverService proxyResolver = new(
            rpcConfig: rpcOptions,
            httpClientFactory: factory,
            logger: this.GetTypedLogger<ProxyResolverService>()
        );

        ContractSecurityService contractSecurity = new(
            goPlusClient: goPlusClient,
            cache: this._securityCache,
            proxyResolver: proxyResolver
        );

        return new PoolEnrichmentService(
            pendleClient: pendleClient,
            hacksClient: hacksClient,
            protocolsClient: protocolsClient,
            coinGeckoClient: coinGeckoClient,
            chainlinkStorage: chainlinkStorage,
            contractSecurityService: contractSecurity,
            poolStorage: poolStorage,
            cache: this._apiCache
        );
    }

    [Fact]
    public async Task GetAllPoolsAsync_EmptyData_ReturnsEmptyListAsync()
    {
        const string EMPTY_ARRAY = "[]";

        using MultiResponseHttpHandler handler = new(
            [
                EMPTY_ARRAY, // pendle chain 1
                EMPTY_ARRAY, // pendle chain 2
                EMPTY_ARRAY, // pendle chain 3
                EMPTY_ARRAY, // pendle chain 4
            ]
        );

        PoolEnrichmentService service = this.CreateEnrichmentService(handler);

        IReadOnlyList<RawPool> pools = await service.GetAllPoolsAsync(this.CancellationToken());

        Assert.Empty(pools);
    }

    [Fact]
    public async Task EnrichPoolsAsync_EmptyFilteredList_ReturnsEmptyAsync()
    {
        const string EMPTY_ARRAY = "[]";
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);

        PoolEnrichmentService service = this.CreateEnrichmentService(handler);

        IReadOnlyList<Pool> result = await service.EnrichPoolsAsync(
            filteredPools: [],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task EnrichPoolsAsync_SinglePoolNoDepeg_ReturnsEnrichedPoolAsync()
    {
        const string EMPTY_ARRAY = "[]";
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);

        PoolEnrichmentService service = this.CreateEnrichmentService(handler);

        RawPool pool = new()
        {
            Project = "aave-v3",
            Chain = "Ethereum",
            Symbol = "USDC",
            TvlUsd = 5_000_000,
            Apy = 5.0,
            Stablecoin = true,
            IlRisk = "no",
            PoolId = "abc123-uuid",
            Predictions = new RawPredictions(),
        };

        IReadOnlyList<Pool> result = await service.EnrichPoolsAsync(
            filteredPools: [pool],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Equal(expected: "aave-v3", actual: result[0].Project);
        Assert.Equal(expected: "Ethereum", actual: result[0].Chain);
        Assert.Equal(expected: "defillama", actual: result[0].DataSource);
    }

    [Fact]
    public async Task EnrichPoolsAsync_PendlePool_HasPendleDataSourceAsync()
    {
        const string EMPTY_ARRAY = "[]";
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);

        PoolEnrichmentService service = this.CreateEnrichmentService(handler);

        RawPool pool = new()
        {
            Project = "pendle",
            Chain = "Ethereum",
            Symbol = "PT-USDC",
            TvlUsd = 2_000_000,
            Apy = 8.0,
            Stablecoin = false,
            IlRisk = "no",
            PoolId = "0xC374f7eC85F8C7DE3207a10bB1978bA104bdA3B2",
            Predictions = new RawPredictions(),
        };

        IReadOnlyList<Pool> result = await service.EnrichPoolsAsync(
            filteredPools: [pool],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Equal(expected: "pendle", actual: result[0].DataSource);
    }

    [Fact]
    public async Task GetHackMapAsync_EmptyHacks_ReturnsEmptyMapAsync()
    {
        const string EMPTY_ARRAY = "[]";
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);

        PoolEnrichmentService service = this.CreateEnrichmentService(handler);

        IReadOnlyDictionary<string, List<HackInfo>> hackMap = await service.GetHackMapAsync(this.CancellationToken());

        Assert.Empty(hackMap);
    }

    [Fact]
    public async Task GetProtocolAuditMapAsync_EmptyProtocols_ReturnsEmptyMapAsync()
    {
        const string EMPTY_ARRAY = "[]";
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);

        PoolEnrichmentService service = this.CreateEnrichmentService(handler);

        IReadOnlyDictionary<string, AuditInfo> auditMap = await service.GetProtocolAuditMapAsync(
            this.CancellationToken()
        );

        Assert.Empty(auditMap);
    }

    [Fact]
    public async Task GetStablecoinPriceMapAsync_EmptyStablecoins_ReturnsEmptyMapAsync()
    {
        const string EMPTY_ARRAY = "[]";
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);

        PoolEnrichmentService service = this.CreateEnrichmentService(handler);

        IReadOnlyDictionary<string, decimal> priceMap = await service.GetStablecoinPriceMapAsync(
            this.CancellationToken()
        );

        Assert.Empty(priceMap);
    }

    [Fact]
    public async Task GetStablecoinAddressMapAsync_EmptyData_ReturnsEmptyMapAsync()
    {
        const string EMPTY_ARRAY = "[]";
        using FreshResponseHttpHandler handler = new(EMPTY_ARRAY);

        PoolEnrichmentService service = this.CreateEnrichmentService(handler);

        IReadOnlyDictionary<string, string> addressMap = await service.GetStablecoinAddressMapAsync(
            this.CancellationToken()
        );

        Assert.Empty(addressMap);
    }

    [Fact]
    public async Task EnrichPoolsAsync_PoolWithHacks_HacksIncludedInResultAsync()
    {
        // Return a hack for "aave" to ensure the non-empty ToArray path is exercised
        // RawHack fields: date (long unix timestamp), name, classification, technique, amount, source
        const string HACKS_JSON =
            """[{"date":1672531200,"name":"Aave","classification":"Protocol","technique":"Flash Loan","amount":1000000,"source":"defillama"}]""";
        const string EMPTY_ARRAY = "[]";

        using MultiResponseHttpHandler handler = new(
            [
                HACKS_JSON, // hacks (GetHackMapAsync is called first inside EnrichPoolsAsync)
                EMPTY_ARRAY, // protocols
                EMPTY_ARRAY, // stablecoins (price map)
                EMPTY_ARRAY, // stablecoins (address map)
                EMPTY_ARRAY, // coin list (address map)
            ]
        );

        PoolEnrichmentService service = this.CreateEnrichmentService(handler);

        RawPool pool = new()
        {
            Project = "aave",
            Chain = "Ethereum",
            Symbol = "USDC",
            TvlUsd = 5_000_000,
            Apy = 5.0,
            Stablecoin = true,
            IlRisk = "no",
            PoolId = "aave-pool-1",
            Predictions = new RawPredictions(),
        };

        IReadOnlyList<Pool> result = await service.EnrichPoolsAsync(
            filteredPools: [pool],
            cancellationToken: this.CancellationToken()
        );

        // Pool should be returned with hacks populated
        Assert.Single(result);
        Assert.NotEmpty(result[0].Hacks);
    }

    [Fact]
    public async Task EnrichPoolsAsync_PoolWithDepeggedToken_IsExcludedAsync()
    {
        // Stablecoin USDC at 0.94 is critically depegged
        const string STABLECOINS_JSON =
            """[{"id":"usd-coin","symbol":"USDC","name":"USD Coin","current_price":0.94}]""";
        using FreshResponseHttpHandler handler = new(STABLECOINS_JSON);

        PoolEnrichmentService service = this.CreateEnrichmentService(handler);

        RawPool pool = new()
        {
            Project = "aave-v3",
            Chain = "Ethereum",
            Symbol = "USDC", // USDC is depegged
            TvlUsd = 5_000_000,
            Apy = 5.0,
            Stablecoin = true,
            IlRisk = "no",
            PoolId = "abc123-uuid",
            Predictions = new RawPredictions(),
        };

        IReadOnlyList<Pool> result = await service.EnrichPoolsAsync(
            filteredPools: [pool],
            cancellationToken: this.CancellationToken()
        );

        // Pool with depegged stablecoin should be excluded
        Assert.Empty(result);
    }

    private sealed class FreshResponseHttpHandler : HttpMessageHandler
    {
        private readonly string _json;

        public FreshResponseHttpHandler(string json) => this._json = json;

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

    private sealed class FakePoolStorage : IDefiLlamaPoolStorage
    {
        private readonly IReadOnlyList<RawPool> _pools;

        public FakePoolStorage()
            : this([]) { }

        public FakePoolStorage(IReadOnlyList<RawPool> pools)
        {
            this._pools = pools;
        }

        public ValueTask StorePoolsAsync(
            IReadOnlyList<RawPool> pools,
            DateTimeOffset? dataDate,
            CancellationToken cancellationToken
        ) => ValueTask.CompletedTask;

        public ValueTask<IReadOnlyList<RawPool>> GetAllPoolsAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult(this._pools);
    }

    private sealed class FakeChainlinkStorage : IChainlinkPriceFeedStorageService
    {
        private readonly IReadOnlyList<ChainlinkPriceFeed> _feeds;

        public FakeChainlinkStorage()
            : this([]) { }

        public FakeChainlinkStorage(IReadOnlyList<ChainlinkPriceFeed> feeds)
        {
            this._feeds = feeds;
        }

        public ValueTask StoreAsync(IReadOnlyList<ChainlinkPriceFeed> feeds, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask<IReadOnlyList<ChainlinkPriceFeed>> GetAllAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult(this._feeds);
    }
}
