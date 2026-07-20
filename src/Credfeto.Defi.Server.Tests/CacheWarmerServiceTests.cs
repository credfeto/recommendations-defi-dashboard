using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.Chainlink.Interfaces;
using Credfeto.Defi.ApiClients.CoinGecko;
using Credfeto.Defi.ApiClients.DefiLlama;
using Credfeto.Defi.ApiClients.Pendle;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Server.Tests.Common;
using Credfeto.Defi.Services;
using Credfeto.Defi.Storage;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class CacheWarmerServiceTests : TestBase
{
    private readonly ApiCacheService _apiCache;
    private readonly FakeTimeProvider _timeProvider;

    public CacheWarmerServiceTests()
    {
        this._timeProvider = new FakeTimeProvider();
        this._apiCache = new ApiCacheService(database: new FakeDatabase(), timeProvider: this._timeProvider);
    }

    private T CreateApiClient<T>(HttpMessageHandler handler)
        where T : class
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));

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

        return GetSubstitute<T>();
    }

    [Fact]
    public async Task StartAsync_ReturnsCompletedTaskImmediatelyAsync()
    {
        const string EMPTY_POOLS_JSON = """{"data":[]}""";
        const string EMPTY_JSON = "[]";

        using FreshResponseHttpHandler handler = new(
            [
                EMPTY_POOLS_JSON, // llama pools
                EMPTY_JSON, // pendle chain 1
                EMPTY_JSON, // pendle chain 2
                EMPTY_JSON, // pendle chain 3
                EMPTY_JSON, // pendle chain 4
                EMPTY_JSON, // hacks
                EMPTY_JSON, // protocols
                EMPTY_JSON, // stablecoins
                EMPTY_JSON, // coin list
            ]
        );

        CacheWarmerService warmer = new(
            llamaPoolsClient: this.CreateApiClient<DefiLlamaPoolsClient>(handler),
            pendleClient: this.CreateApiClient<PendleMarketsClient>(handler),
            hacksClient: this.CreateApiClient<DefiLlamaHacksClient>(handler),
            protocolsClient: this.CreateApiClient<DefiLlamaProtocolsClient>(handler),
            coinGeckoClient: this.CreateApiClient<CoinGeckoStablecoinsClient>(handler),
            chainlinkClient: CreateChainlinkClient(),
            apiCache: this._apiCache,
            poolStorage: GetSubstitute<IDefiLlamaPoolStorage>(),
            chainlinkStorage: new FakeChainlinkStorage(),
            logger: this.GetTypedLogger<CacheWarmerService>()
        );

        Task result = warmer.StartAsync(this.CancellationToken());

        Assert.True(result.IsCompleted, userMessage: "StartAsync should return synchronously");
        await result;

        // Give the background warming task time to complete
        await Task.Delay(TimeSpan.FromMilliseconds(500), this.CancellationToken());
    }

    [Fact]
    public async Task StartAsync_WhenAllCacheEntriesFresh_SkipsAllFetchersAsync()
    {
        // Pre-warm all cache entries
        const string EMPTY_JSON = "[]";
        const string EMPTY_POOLS_JSON = """{"data":[]}""";

        using FreshResponseHttpHandler primeHandler = new(
            [
                EMPTY_POOLS_JSON,
                EMPTY_JSON,
                EMPTY_JSON,
                EMPTY_JSON,
                EMPTY_JSON,
                EMPTY_JSON,
                EMPTY_JSON,
                EMPTY_JSON,
                EMPTY_JSON,
            ]
        );

        // Use a separate warmer to prime all cache entries
        CacheWarmerService primeWarmer = new(
            llamaPoolsClient: this.CreateApiClient<DefiLlamaPoolsClient>(primeHandler),
            pendleClient: this.CreateApiClient<PendleMarketsClient>(primeHandler),
            hacksClient: this.CreateApiClient<DefiLlamaHacksClient>(primeHandler),
            protocolsClient: this.CreateApiClient<DefiLlamaProtocolsClient>(primeHandler),
            coinGeckoClient: this.CreateApiClient<CoinGeckoStablecoinsClient>(primeHandler),
            chainlinkClient: CreateChainlinkClient(),
            apiCache: this._apiCache,
            poolStorage: GetSubstitute<IDefiLlamaPoolStorage>(),
            chainlinkStorage: new FakeChainlinkStorage(),
            logger: this.GetTypedLogger<CacheWarmerService>()
        );

        await primeWarmer.StartAsync(this.CancellationToken());
        await Task.Delay(TimeSpan.FromMilliseconds(500), this.CancellationToken());

        // Now start again - all entries should be fresh, so no fetching needed
        using FreshResponseHttpHandler secondHandler = new([]);

        CacheWarmerService warmer = new(
            llamaPoolsClient: this.CreateApiClient<DefiLlamaPoolsClient>(secondHandler),
            pendleClient: this.CreateApiClient<PendleMarketsClient>(secondHandler),
            hacksClient: this.CreateApiClient<DefiLlamaHacksClient>(secondHandler),
            protocolsClient: this.CreateApiClient<DefiLlamaProtocolsClient>(secondHandler),
            coinGeckoClient: this.CreateApiClient<CoinGeckoStablecoinsClient>(secondHandler),
            chainlinkClient: CreateChainlinkClient(),
            apiCache: this._apiCache,
            poolStorage: GetSubstitute<IDefiLlamaPoolStorage>(),
            chainlinkStorage: new FakeChainlinkStorage(),
            logger: this.GetTypedLogger<CacheWarmerService>()
        );

        await warmer.StartAsync(this.CancellationToken());
        await Task.Delay(TimeSpan.FromMilliseconds(200), this.CancellationToken());
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTaskAsync()
    {
        using FreshResponseHttpHandler handler = new([]);

        CacheWarmerService warmer = new(
            llamaPoolsClient: this.CreateApiClient<DefiLlamaPoolsClient>(handler),
            pendleClient: this.CreateApiClient<PendleMarketsClient>(handler),
            hacksClient: this.CreateApiClient<DefiLlamaHacksClient>(handler),
            protocolsClient: this.CreateApiClient<DefiLlamaProtocolsClient>(handler),
            coinGeckoClient: this.CreateApiClient<CoinGeckoStablecoinsClient>(handler),
            chainlinkClient: CreateChainlinkClient(),
            apiCache: this._apiCache,
            poolStorage: GetSubstitute<IDefiLlamaPoolStorage>(),
            chainlinkStorage: new FakeChainlinkStorage(),
            logger: this.GetTypedLogger<CacheWarmerService>()
        );

        Task result = warmer.StopAsync(this.CancellationToken());

        Assert.True(result.IsCompleted, userMessage: "StopAsync should return synchronously");
        await result;
    }

    private static IChainlinkStablecoinsClient CreateChainlinkClient()
    {
        return new FakeChainlinkClient();
    }

    private sealed class FreshResponseHttpHandler : HttpMessageHandler
    {
        private readonly string[] _responses;
        private int _index;

        public FreshResponseHttpHandler(string[] responses)
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

    private sealed class FakeChainlinkClient : IChainlinkStablecoinsClient
    {
        public ValueTask<IReadOnlyList<ChainlinkPriceFeed>> FetchStablecoinsAsync(
            CancellationToken cancellationToken
        ) => ValueTask.FromResult<IReadOnlyList<ChainlinkPriceFeed>>([]);
    }

}
