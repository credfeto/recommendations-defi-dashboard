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
using Credfeto.Defi.Storage.Database.Rows;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class CacheWarmerServiceTests : TestBase
{
    private static readonly TimeSpan WarmupDelay = TimeSpan.FromMilliseconds(500);

    private readonly ApiCacheService _apiCache;
    private readonly FakeDatabase _database;
    private readonly FakeTimeProvider _timeProvider;

    public CacheWarmerServiceTests()
    {
        this._timeProvider = new FakeTimeProvider();
        this._database = new FakeDatabase();
        this._apiCache = new ApiCacheService(database: this._database, timeProvider: this._timeProvider);
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

    private CacheWarmerService CreateWarmer(
        HttpMessageHandler handler,
        IDefiLlamaPoolStorage? poolStorage = null,
        IPendleMarketStorageService? pendleStorage = null,
        IChainlinkPriceFeedStorageService? chainlinkStorage = null,
        IDefiLlamaHackStorageService? hackStorage = null,
        IDefiLlamaProtocolStorageService? protocolStorage = null
    )
    {
        return new CacheWarmerService(
            llamaPoolsClient: this.CreateApiClient<DefiLlamaPoolsClient>(handler),
            pendleClient: this.CreateApiClient<PendleMarketsClient>(handler),
            hacksClient: this.CreateApiClient<DefiLlamaHacksClient>(handler),
            protocolsClient: this.CreateApiClient<DefiLlamaProtocolsClient>(handler),
            coinGeckoClient: this.CreateApiClient<CoinGeckoStablecoinsClient>(handler),
            chainlinkClient: CreateChainlinkClient(),
            apiCache: this._apiCache,
            poolStorage: poolStorage ?? GetSubstitute<IDefiLlamaPoolStorage>(),
            protocolStorage: protocolStorage ?? GetSubstitute<IDefiLlamaProtocolStorageService>(),
            hackStorage: hackStorage ?? GetSubstitute<IDefiLlamaHackStorageService>(),
            pendleStorage: pendleStorage ?? GetSubstitute<IPendleMarketStorageService>(),
            chainlinkStorage: chainlinkStorage ?? new FakeChainlinkStorage(),
            coinGeckoStorage: new FakeCoinGeckoCoinStorage(),
            coinGeckoStablecoinStorage: new FakeCoinGeckoStablecoinStorage(),
            logger: this.GetTypedLogger<CacheWarmerService>()
        );
    }

    [Fact]
    public async Task StartAsync_ReturnsCompletedTaskImmediatelyAsync()
    {
        using FreshResponseHttpHandler handler = new(CreateAllFetcherResponses());

        CacheWarmerService warmer = this.CreateWarmer(handler);

        Task result = warmer.StartAsync(this.CancellationToken());

        Assert.True(result.IsCompleted, userMessage: "StartAsync should return synchronously");
        await result;

        // Give the background warming task time to complete
        await Task.Delay(WarmupDelay, this.CancellationToken());
    }

    [Fact]
    public async Task StartAsync_OnEveryCall_AlwaysRefetchesStorageBackedEntriesAsync()
    {
        // Regression test for #406: WarmLlamaPoolsAsync, WarmPendlePoolsAsync and
        // WarmChainlinkPriceFeedsAsync write straight to their own storage services rather than
        // through ApiCacheService, so nothing ever marks their key fresh; they must always run
        // on every StartAsync call rather than being skipped by a freshness check.
        IDefiLlamaPoolStorage poolStorage = GetSubstitute<IDefiLlamaPoolStorage>();
        IPendleMarketStorageService pendleStorage = GetSubstitute<IPendleMarketStorageService>();
        IChainlinkPriceFeedStorageService chainlinkStorage = GetSubstitute<IChainlinkPriceFeedStorageService>();

        async Task RunWarmerAsync()
        {
            using FreshResponseHttpHandler handler = new(CreateAllFetcherResponses());

            CacheWarmerService warmer = this.CreateWarmer(
                handler: handler,
                poolStorage: poolStorage,
                pendleStorage: pendleStorage,
                chainlinkStorage: chainlinkStorage
            );

            await warmer.StartAsync(this.CancellationToken());
            await Task.Delay(WarmupDelay, this.CancellationToken());
        }

        await RunWarmerAsync();
        await RunWarmerAsync();

        await AssertStorageBackedFetchersRanAsync(
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            chainlinkStorage: chainlinkStorage,
            expectedCalls: 2
        );
    }

    [Fact]
    public async Task StartAsync_WhenGatedCacheEntriesFresh_SkipsOnlyGatedFetchersAsync()
    {
        // The ApiCache-backed keys (hacks, protocols, stablecoins, coin list) should still be
        // skipped once fresh; only the three storage-backed keys always refetch regardless of
        // ApiCache freshness (#406).
        ApiCacheRow freshRow = new("irrelevant-key", "[]", this._timeProvider.GetUtcNow() - TimeSpan.FromMinutes(10));

        this._database.WithReturn<ApiCacheRow?>(freshRow) // defillama_hacks
            .WithReturn<ApiCacheRow?>(freshRow) // defillama_protocols
            .WithReturn<ApiCacheRow?>(freshRow) // coingecko_stablecoins
            .WithReturn<ApiCacheRow?>(freshRow); // coingecko_coin_list

        IDefiLlamaPoolStorage poolStorage = GetSubstitute<IDefiLlamaPoolStorage>();
        IPendleMarketStorageService pendleStorage = GetSubstitute<IPendleMarketStorageService>();
        IChainlinkPriceFeedStorageService chainlinkStorage = GetSubstitute<IChainlinkPriceFeedStorageService>();
        IDefiLlamaHackStorageService hackStorage = GetSubstitute<IDefiLlamaHackStorageService>();
        IDefiLlamaProtocolStorageService protocolStorage = GetSubstitute<IDefiLlamaProtocolStorageService>();

        using FreshResponseHttpHandler handler = new(CreateAllFetcherResponses());

        CacheWarmerService warmer = this.CreateWarmer(
            handler: handler,
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            chainlinkStorage: chainlinkStorage,
            hackStorage: hackStorage,
            protocolStorage: protocolStorage
        );

        await warmer.StartAsync(this.CancellationToken());
        await Task.Delay(WarmupDelay, this.CancellationToken());

        await AssertGatedFetchersSkippedAsync(hackStorage: hackStorage, protocolStorage: protocolStorage);
        await AssertStorageBackedFetchersRanAsync(
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            chainlinkStorage: chainlinkStorage,
            expectedCalls: 1
        );
    }

    private static async Task AssertGatedFetchersSkippedAsync(
        IDefiLlamaHackStorageService hackStorage,
        IDefiLlamaProtocolStorageService protocolStorage
    )
    {
        await hackStorage
            .DidNotReceive()
            .StoreHacksAsync(
                hacks: Arg.Any<IReadOnlyList<RawHack>>(),
                dataDate: Arg.Any<DateTimeOffset?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
        await protocolStorage
            .DidNotReceive()
            .StoreProtocolsAsync(
                protocols: Arg.Any<IReadOnlyList<RawProtocol>>(),
                dataDate: Arg.Any<DateTimeOffset?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    private static async Task AssertStorageBackedFetchersRanAsync(
        IDefiLlamaPoolStorage poolStorage,
        IPendleMarketStorageService pendleStorage,
        IChainlinkPriceFeedStorageService chainlinkStorage,
        int expectedCalls
    )
    {
        await poolStorage
            .Received(expectedCalls)
            .StorePoolsAsync(
                pools: Arg.Any<IReadOnlyList<RawPool>>(),
                dataDate: Arg.Any<DateTimeOffset?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
        await pendleStorage
            .Received(expectedCalls)
            .StoreMarketsAsync(
                markets: Arg.Any<IReadOnlyList<PendleMarket>>(),
                dataDate: Arg.Any<DateTimeOffset?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
        await chainlinkStorage
            .Received(expectedCalls)
            .StoreAsync(
                feeds: Arg.Any<IReadOnlyList<ChainlinkPriceFeed>>(),
                dataDate: Arg.Any<DateTimeOffset?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    private static string[] CreateAllFetcherResponses()
    {
        const string EMPTY_JSON = "[]";
        const string EMPTY_POOLS_JSON = """{"data":[]}""";

        return
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
        ];
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTaskAsync()
    {
        using FreshResponseHttpHandler handler = new([]);

        CacheWarmerService warmer = this.CreateWarmer(handler);

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
