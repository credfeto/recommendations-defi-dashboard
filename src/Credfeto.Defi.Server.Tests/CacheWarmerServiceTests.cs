using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.ApiClients.CoinGecko;
using Credfeto.Defi.Server.ApiClients.DefiLlama;
using Credfeto.Defi.Server.ApiClients.Pendle;
using Credfeto.Defi.Server.Cache;
using Credfeto.Defi.Server.Config;
using Credfeto.Defi.Server.Services;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class CacheWarmerServiceTests : TestBase, IDisposable
{
    private readonly string _tempDir;
    private readonly ApiCacheService _apiCache;
    private readonly FakeTimeProvider _timeProvider;

    public CacheWarmerServiceTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        this._timeProvider = new FakeTimeProvider();

        IOptions<CacheConfig> options = Options.Create(new CacheConfig { DbDirectory = this._tempDir });
        this._apiCache = new ApiCacheService(config: options, timeProvider: this._timeProvider);
    }

    public void Dispose()
    {
        this._apiCache.Dispose();

        if (Directory.Exists(this._tempDir))
        {
            Directory.Delete(path: this._tempDir, recursive: true);
        }
    }

    private static T CreateApiClient<T>(HttpMessageHandler handler)
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
            llamaPoolsClient: CreateApiClient<DefiLlamaPoolsClient>(handler),
            pendleClient: CreateApiClient<PendleMarketsClient>(handler),
            hacksClient: CreateApiClient<DefiLlamaHacksClient>(handler),
            protocolsClient: CreateApiClient<DefiLlamaProtocolsClient>(handler),
            coinGeckoClient: CreateApiClient<CoinGeckoStablecoinsClient>(handler),
            apiCache: this._apiCache,
            logger: GetSubstitute<ILogger<CacheWarmerService>>()
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
            llamaPoolsClient: CreateApiClient<DefiLlamaPoolsClient>(primeHandler),
            pendleClient: CreateApiClient<PendleMarketsClient>(primeHandler),
            hacksClient: CreateApiClient<DefiLlamaHacksClient>(primeHandler),
            protocolsClient: CreateApiClient<DefiLlamaProtocolsClient>(primeHandler),
            coinGeckoClient: CreateApiClient<CoinGeckoStablecoinsClient>(primeHandler),
            apiCache: this._apiCache,
            logger: GetSubstitute<ILogger<CacheWarmerService>>()
        );

        await primeWarmer.StartAsync(this.CancellationToken());
        await Task.Delay(TimeSpan.FromMilliseconds(500), this.CancellationToken());

        // Now start again - all entries should be fresh, so no fetching needed
        using FreshResponseHttpHandler secondHandler = new([]);

        CacheWarmerService warmer = new(
            llamaPoolsClient: CreateApiClient<DefiLlamaPoolsClient>(secondHandler),
            pendleClient: CreateApiClient<PendleMarketsClient>(secondHandler),
            hacksClient: CreateApiClient<DefiLlamaHacksClient>(secondHandler),
            protocolsClient: CreateApiClient<DefiLlamaProtocolsClient>(secondHandler),
            coinGeckoClient: CreateApiClient<CoinGeckoStablecoinsClient>(secondHandler),
            apiCache: this._apiCache,
            logger: GetSubstitute<ILogger<CacheWarmerService>>()
        );

        await warmer.StartAsync(this.CancellationToken());
        await Task.Delay(TimeSpan.FromMilliseconds(200), this.CancellationToken());
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTaskAsync()
    {
        using FreshResponseHttpHandler handler = new([]);

        CacheWarmerService warmer = new(
            llamaPoolsClient: CreateApiClient<DefiLlamaPoolsClient>(handler),
            pendleClient: CreateApiClient<PendleMarketsClient>(handler),
            hacksClient: CreateApiClient<DefiLlamaHacksClient>(handler),
            protocolsClient: CreateApiClient<DefiLlamaProtocolsClient>(handler),
            coinGeckoClient: CreateApiClient<CoinGeckoStablecoinsClient>(handler),
            apiCache: this._apiCache,
            logger: GetSubstitute<ILogger<CacheWarmerService>>()
        );

        Task result = warmer.StopAsync(this.CancellationToken());

        Assert.True(result.IsCompleted, userMessage: "StopAsync should return synchronously");
        await result;
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
}
