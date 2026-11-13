using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.Chainlink.Interfaces;
using Credfeto.Defi.ApiClients.CoinGecko;
using Credfeto.Defi.ApiClients.DefiLlama;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
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
using NSubstitute.Core;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class CacheWarmerServiceTests : TestBase
{
    private const int ALL_FETCHER_COUNT = 8;

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
        IDefiLlamaProtocolStorageService? protocolStorage = null,
        ICoinGeckoCoinStorageService? coinGeckoStorage = null,
        ICoinGeckoStablecoinStorageService? coinGeckoStablecoinStorage = null,
        IDefiLlamaStablecoinsClient? llamaStablecoinsClient = null,
        ILogger<CacheWarmerService>? logger = null
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
            coinGeckoStorage: coinGeckoStorage ?? new FakeCoinGeckoCoinStorage(),
            coinGeckoStablecoinStorage: coinGeckoStablecoinStorage ?? new FakeCoinGeckoStablecoinStorage(),
            llamaStablecoinsClient: llamaStablecoinsClient ?? new FakeDefiLlamaStablecoinsClient(),
            logger: logger ?? this.GetTypedLogger<CacheWarmerService>()
        );
    }

    [Fact]
    public async Task StartAsync_ReturnsCompletedTaskImmediatelyAsync()
    {
        using FreshResponseHttpHandler handler = new(CreateAllFetcherResponses());

        IDefiLlamaPoolStorage poolStorage = GetSubstitute<IDefiLlamaPoolStorage>();
        IPendleMarketStorageService pendleStorage = GetSubstitute<IPendleMarketStorageService>();
        IChainlinkPriceFeedStorageService chainlinkStorage = GetSubstitute<IChainlinkPriceFeedStorageService>();
        using StorageCallSignal storageCalls = new(
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            chainlinkStorage: chainlinkStorage
        );

        CacheWarmerService warmer = this.CreateWarmer(
            handler: handler,
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            chainlinkStorage: chainlinkStorage
        );

        Task result = warmer.StartAsync(this.CancellationToken());

        Assert.True(result.IsCompleted, userMessage: "StartAsync should return synchronously");
        await result;

        await storageCalls.WaitForStorageBackedFetchersAsync(this.CancellationToken());
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
        using StorageCallSignal storageCalls = new(
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            chainlinkStorage: chainlinkStorage
        );

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
            await storageCalls.WaitForStorageBackedFetchersAsync(this.CancellationToken());
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
        // The ApiCache-backed keys (hacks, protocols, stablecoins, coin list, DefiLlama
        // stablecoins) should still be skipped once fresh; only the three storage-backed keys
        // always refetch regardless of ApiCache freshness (#406).
        ApiCacheRow freshRow = new("irrelevant-key", "[]", this._timeProvider.GetUtcNow() - TimeSpan.FromMinutes(10));

        this._database.WithReturn<ApiCacheRow?>(freshRow) // defillama_hacks
            .WithReturn<ApiCacheRow?>(freshRow) // defillama_protocols
            .WithReturn<ApiCacheRow?>(freshRow) // coingecko_stablecoins
            .WithReturn<ApiCacheRow?>(freshRow) // coingecko_coin_list
            .WithReturn<ApiCacheRow?>(freshRow); // defillama_stablecoins

        IDefiLlamaStablecoinsClient llamaStablecoinsClient = GetSubstitute<IDefiLlamaStablecoinsClient>();

        IDefiLlamaPoolStorage poolStorage = GetSubstitute<IDefiLlamaPoolStorage>();
        IPendleMarketStorageService pendleStorage = GetSubstitute<IPendleMarketStorageService>();
        IChainlinkPriceFeedStorageService chainlinkStorage = GetSubstitute<IChainlinkPriceFeedStorageService>();
        IDefiLlamaHackStorageService hackStorage = GetSubstitute<IDefiLlamaHackStorageService>();
        IDefiLlamaProtocolStorageService protocolStorage = GetSubstitute<IDefiLlamaProtocolStorageService>();
        ICoinGeckoStablecoinStorageService coinGeckoStablecoinStorage =
            GetSubstitute<ICoinGeckoStablecoinStorageService>();
        ICoinGeckoCoinStorageService coinGeckoStorage = GetSubstitute<ICoinGeckoCoinStorageService>();
        using StorageCallSignal storageCalls = new(
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            chainlinkStorage: chainlinkStorage
        );

        using FreshResponseHttpHandler handler = new(CreateAllFetcherResponses());

        CacheWarmerService warmer = this.CreateWarmer(
            handler: handler,
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            chainlinkStorage: chainlinkStorage,
            hackStorage: hackStorage,
            protocolStorage: protocolStorage,
            coinGeckoStorage: coinGeckoStorage,
            coinGeckoStablecoinStorage: coinGeckoStablecoinStorage,
            llamaStablecoinsClient: llamaStablecoinsClient
        );

        await warmer.StartAsync(this.CancellationToken());
        await storageCalls.WaitForStorageBackedFetchersAsync(this.CancellationToken());

        await llamaStablecoinsClient.DidNotReceive().FetchStablecoinsAsync(Arg.Any<CancellationToken>());
        await AssertGatedFetchersSkippedAsync(
            hackStorage: hackStorage,
            protocolStorage: protocolStorage,
            coinGeckoStablecoinStorage: coinGeckoStablecoinStorage,
            coinGeckoStorage: coinGeckoStorage
        );
        await AssertStorageBackedFetchersRanAsync(
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            chainlinkStorage: chainlinkStorage,
            expectedCalls: 1
        );
    }

    [Fact]
    public async Task StartAsync_WhenOneEntryFails_StillWarmsRemainingEntriesAsync()
    {
        IDefiLlamaPoolStorage poolStorage = GetSubstitute<IDefiLlamaPoolStorage>();
        MockDefiLlamaPoolStorageStorePools(
            poolStorage: poolStorage,
            result: Task.FromException(new InvalidOperationException("Pool storage unavailable"))
        );
        IPendleMarketStorageService pendleStorage = GetSubstitute<IPendleMarketStorageService>();
        IChainlinkPriceFeedStorageService chainlinkStorage = GetSubstitute<IChainlinkPriceFeedStorageService>();
        ILogger<CacheWarmerService> logger = this.CreateEnabledLogger();
        using LogCallSignal logCalls = new(logger);

        using FreshResponseHttpHandler handler = new(CreateAllFetcherResponses());

        CacheWarmerService warmer = this.CreateWarmer(
            handler: handler,
            poolStorage: poolStorage,
            pendleStorage: pendleStorage,
            chainlinkStorage: chainlinkStorage,
            logger: logger
        );

        await warmer.StartAsync(this.CancellationToken());
        await logCalls.WaitForLogEntriesAsync(count: ALL_FETCHER_COUNT, cancellationToken: this.CancellationToken());

        await pendleStorage
            .Received(1)
            .StoreMarketsAsync(
                markets: Arg.Any<IReadOnlyList<PendleMarket>>(),
                dataDate: Arg.Any<DateTimeOffset?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
        await chainlinkStorage
            .Received(1)
            .StoreAsync(
                feeds: Arg.Any<IReadOnlyList<ChainlinkPriceFeed>>(),
                dataDate: Arg.Any<DateTimeOffset?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
        AssertWarmedCount(expected: ALL_FETCHER_COUNT - 1, logger: logger);
    }

    [Fact]
    public async Task StartAsync_WhenOneEntryFails_LogsErrorWithKeyAndExceptionAsync()
    {
        InvalidOperationException failure = new("Pool storage unavailable");
        IDefiLlamaPoolStorage poolStorage = GetSubstitute<IDefiLlamaPoolStorage>();
        MockDefiLlamaPoolStorageStorePools(poolStorage: poolStorage, result: Task.FromException(failure));
        ILogger<CacheWarmerService> logger = this.CreateEnabledLogger();
        using LogCallSignal logCalls = new(logger);

        using FreshResponseHttpHandler handler = new(CreateAllFetcherResponses());

        CacheWarmerService warmer = this.CreateWarmer(handler: handler, poolStorage: poolStorage, logger: logger);

        await warmer.StartAsync(this.CancellationToken());
        await logCalls.WaitForLogEntriesAsync(count: ALL_FETCHER_COUNT, cancellationToken: this.CancellationToken());

        ICall errorCall = Assert.Single(GetLogCalls(level: LogLevel.Error, logger: logger));
        object?[] arguments = errorCall.GetArguments();
        EventId eventId = Assert.IsType<EventId>(arguments[1]);
        Assert.Equal(expected: 2, actual: eventId.Id);
        IReadOnlyList<KeyValuePair<string, object?>> state = Assert.IsAssignableFrom<
            IReadOnlyList<KeyValuePair<string, object?>>
        >(arguments[2]);
        KeyValuePair<string, object?> key = Assert.Single(
            state,
            predicate: static kvp => StringComparer.Ordinal.Equals(x: kvp.Key, y: "Key")
        );
        Assert.Equal(expected: "defillama_pools", actual: key.Value);
        Assert.Same(expected: failure, actual: arguments[3]);
    }

    [Fact]
    public async Task StartAsync_WhenCancelledDuringWarmup_DoesNotLogFailureAsync()
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationToken());
        TaskCompletionSource poolStoreGate = new();
        IDefiLlamaPoolStorage poolStorage = GetSubstitute<IDefiLlamaPoolStorage>();
        MockDefiLlamaPoolStorageStorePools(poolStorage: poolStorage, result: poolStoreGate.Task);
        ILogger<CacheWarmerService> logger = this.CreateEnabledLogger();
        using LogCallSignal logCalls = new(logger);

        using FreshResponseHttpHandler handler = new(CreateAllFetcherResponses());

        CacheWarmerService warmer = this.CreateWarmer(handler: handler, poolStorage: poolStorage, logger: logger);

        await warmer.StartAsync(cts.Token);
        await logCalls.WaitForLogEntriesAsync(
            count: ALL_FETCHER_COUNT - 1,
            cancellationToken: this.CancellationToken()
        );

        // Completing the gate normally runs the pool entry's continuation inline, so its catch
        // filter has already been evaluated by the time TrySetCanceled returns.
        await cts.CancelAsync();
        bool cancelled = poolStoreGate.TrySetCanceled(cts.Token);

        Assert.True(cancelled, userMessage: "Pool store should still have been pending");
        Assert.Empty(GetLogCalls(level: LogLevel.Error, logger: logger));
        AssertWarmedCount(expected: ALL_FETCHER_COUNT - 1, logger: logger);
    }

    private ILogger<CacheWarmerService> CreateEnabledLogger()
    {
        ILogger<CacheWarmerService> logger = this.GetTypedLogger<CacheWarmerService>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        return logger;
    }

    private static void MockDefiLlamaPoolStorageStorePools(IDefiLlamaPoolStorage poolStorage, Task result)
    {
        ReturnFromStore(
            storeCall: poolStorage.StorePoolsAsync(
                pools: Arg.Any<IReadOnlyList<RawPool>>(),
                dataDate: Arg.Any<DateTimeOffset?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            ),
            result: result
        );
    }

    private static void ReturnFromStore(ValueTask storeCall, Task result)
    {
        storeCall.Returns(_ => new ValueTask(result));
    }

    private static void AssertWarmedCount(int expected, ILogger<CacheWarmerService> logger)
    {
        Assert.Equal(expected: expected, actual: GetLogCalls(level: LogLevel.Information, logger: logger).Count);
    }

    private static IReadOnlyList<ICall> GetLogCalls(LogLevel level, ILogger<CacheWarmerService> logger)
    {
        return
        [
            .. logger
                .ReceivedCalls()
                .Where(c =>
                    StringComparer.Ordinal.Equals(x: c.GetMethodInfo().Name, y: nameof(ILogger.Log))
                    && c.GetArguments()[0] is LogLevel callLevel
                    && callLevel == level
                ),
        ];
    }

    private static async Task AssertGatedFetchersSkippedAsync(
        IDefiLlamaHackStorageService hackStorage,
        IDefiLlamaProtocolStorageService protocolStorage,
        ICoinGeckoStablecoinStorageService coinGeckoStablecoinStorage,
        ICoinGeckoCoinStorageService coinGeckoStorage
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
        await coinGeckoStablecoinStorage
            .DidNotReceive()
            .StoreAsync(
                stablecoins: Arg.Any<IReadOnlyList<CoinGeckoStablecoin>>(),
                dataDate: Arg.Any<DateTimeOffset?>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
        await coinGeckoStorage
            .DidNotReceive()
            .StoreAsync(
                coins: Arg.Any<IReadOnlyList<CoinGeckoCoinPlatforms>>(),
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

    // StartAsync warms in a fire-and-forget task, so tests wait for each storage-backed fetcher
    // to store rather than sleeping for a fixed time, which flaked when the machine was loaded.
    private sealed class StorageCallSignal : IDisposable
    {
        private const int STORAGE_BACKED_FETCHER_COUNT = 3;

        // Upper bound only: the wait ends as soon as the stores happen, normally in milliseconds.
        private static readonly TimeSpan WarmupTimeout = TimeSpan.FromSeconds(30);

        private readonly SemaphoreSlim _calls = new(initialCount: 0);

        public StorageCallSignal(
            IDefiLlamaPoolStorage poolStorage,
            IPendleMarketStorageService pendleStorage,
            IChainlinkPriceFeedStorageService chainlinkStorage
        )
        {
            this.SignalOn(
                poolStorage.StorePoolsAsync(
                    pools: Arg.Any<IReadOnlyList<RawPool>>(),
                    dataDate: Arg.Any<DateTimeOffset?>(),
                    cancellationToken: Arg.Any<CancellationToken>()
                )
            );
            this.SignalOn(
                pendleStorage.StoreMarketsAsync(
                    markets: Arg.Any<IReadOnlyList<PendleMarket>>(),
                    dataDate: Arg.Any<DateTimeOffset?>(),
                    cancellationToken: Arg.Any<CancellationToken>()
                )
            );
            this.SignalOn(
                chainlinkStorage.StoreAsync(
                    feeds: Arg.Any<IReadOnlyList<ChainlinkPriceFeed>>(),
                    dataDate: Arg.Any<DateTimeOffset?>(),
                    cancellationToken: Arg.Any<CancellationToken>()
                )
            );
        }

        public void Dispose()
        {
            this._calls.Dispose();
        }

        private void SignalOn(ValueTask substituteCall)
        {
            substituteCall.Returns(_ => this.RecordAsync());
        }

        private ValueTask RecordAsync()
        {
            this._calls.Release();

            return ValueTask.CompletedTask;
        }

        public async Task WaitForStorageBackedFetchersAsync(CancellationToken cancellationToken)
        {
            for (int call = 0; call < STORAGE_BACKED_FETCHER_COUNT; call++)
            {
                bool signalled = await this._calls.WaitAsync(
                    timeout: WarmupTimeout,
                    cancellationToken: cancellationToken
                );
                Assert.True(signalled, userMessage: "Timed out waiting for the cache warmer to store data");
            }
        }
    }

    // Every warmed entry logs exactly once, as warmed or failed, so tests wait for that many log
    // entries instead of sleeping while the fire-and-forget warm-up runs.
    private sealed class LogCallSignal : IDisposable
    {
        private static readonly TimeSpan WarmupTimeout = TimeSpan.FromSeconds(30);

        private readonly SemaphoreSlim _calls = new(initialCount: 0);

        public LogCallSignal(ILogger logger)
        {
            logger
                .When(static logger =>
                    logger.Log(
                        logLevel: Arg.Any<LogLevel>(),
                        eventId: Arg.Any<EventId>(),
                        state: Arg.Any<Arg.AnyType>(),
                        exception: Arg.Any<Exception?>(),
                        formatter: Arg.Any<Func<Arg.AnyType, Exception?, string>>()
                    )
                )
                .Do(_ => this._calls.Release());
        }

        public void Dispose()
        {
            this._calls.Dispose();
        }

        public async Task WaitForLogEntriesAsync(int count, CancellationToken cancellationToken)
        {
            for (int call = 0; call < count; call++)
            {
                bool signalled = await this._calls.WaitAsync(
                    timeout: WarmupTimeout,
                    cancellationToken: cancellationToken
                );
                Assert.True(signalled, userMessage: "Timed out waiting for the cache warmer to log");
            }
        }
    }

    private sealed class FakeChainlinkClient : IChainlinkStablecoinsClient
    {
        public ValueTask<IReadOnlyList<ChainlinkPriceFeed>> FetchStablecoinsAsync(
            CancellationToken cancellationToken
        ) => ValueTask.FromResult<IReadOnlyList<ChainlinkPriceFeed>>([]);
    }
}
