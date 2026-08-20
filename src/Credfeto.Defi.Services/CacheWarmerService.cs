using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.Chainlink.Interfaces;
using Credfeto.Defi.ApiClients.CoinGecko.Interfaces;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
using Credfeto.Defi.ApiClients.Pendle.Interfaces;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Services.LoggingExtensions;
using Credfeto.Defi.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.Services;

/// <summary>
///     Background service that warms the API cache on startup.
///     Each entry is fetched independently; errors are logged and skipped
///     so a failure in one API does not block the others.
/// </summary>
public sealed class CacheWarmerService : IHostedService
{
    private readonly ApiCacheService _apiCache;
    private readonly IChainlinkStablecoinsClient _chainlinkClient;
    private readonly IChainlinkPriceFeedStorageService _chainlinkStorage;
    private readonly ICoinGeckoCoinStorageService _coinGeckoStorage;
    private readonly ICoinGeckoStablecoinsClient _coinGeckoClient;
    private readonly ICoinGeckoStablecoinStorageService _coinGeckoStablecoinStorage;
    private readonly IDefiLlamaHackStorageService _hackStorage;
    private readonly IDefiLlamaHacksClient _hacksClient;
    private readonly IDefiLlamaPoolsClient _llamaPoolsClient;
    private readonly ILogger<CacheWarmerService> _logger;
    private readonly IPendleMarketsClient _pendleClient;
    private readonly IPendleMarketStorageService _pendleStorage;
    private readonly IDefiLlamaPoolStorage _poolStorage;
    private readonly IDefiLlamaProtocolsClient _protocolsClient;
    private readonly IDefiLlamaProtocolStorageService _protocolStorage;

    /// <summary>
    ///     Initialises a new instance of <see cref="CacheWarmerService" />.
    /// </summary>
    public CacheWarmerService(
        IDefiLlamaPoolsClient llamaPoolsClient,
        IPendleMarketsClient pendleClient,
        IDefiLlamaHacksClient hacksClient,
        IDefiLlamaProtocolsClient protocolsClient,
        ICoinGeckoStablecoinsClient coinGeckoClient,
        IChainlinkStablecoinsClient chainlinkClient,
        ApiCacheService apiCache,
        IDefiLlamaPoolStorage poolStorage,
        IDefiLlamaProtocolStorageService protocolStorage,
        IDefiLlamaHackStorageService hackStorage,
        IPendleMarketStorageService pendleStorage,
        IChainlinkPriceFeedStorageService chainlinkStorage,
        ICoinGeckoCoinStorageService coinGeckoStorage,
        ICoinGeckoStablecoinStorageService coinGeckoStablecoinStorage,
        ILogger<CacheWarmerService> logger
    )
    {
        this._llamaPoolsClient = llamaPoolsClient;
        this._pendleClient = pendleClient;
        this._hacksClient = hacksClient;
        this._protocolsClient = protocolsClient;
        this._coinGeckoClient = coinGeckoClient;
        this._chainlinkClient = chainlinkClient;
        this._apiCache = apiCache;
        this._poolStorage = poolStorage;
        this._protocolStorage = protocolStorage;
        this._hackStorage = hackStorage;
        this._pendleStorage = pendleStorage;
        this._chainlinkStorage = chainlinkStorage;
        this._coinGeckoStorage = coinGeckoStorage;
        this._coinGeckoStablecoinStorage = coinGeckoStablecoinStorage;
        this._logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Fire and forget — warming is best-effort and must not block startup
        _ = Task.Run(() => this.WarmCacheAsync(cancellationToken), cancellationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task WarmCacheAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<FetcherRegistration> fetchers = this.BuildFetchers();

        List<Task> tasks = [];

        foreach ((string key, Func<CancellationToken, Task> fetcher, bool bypassFreshnessGate) in fetchers)
        {
            bool isFresh = !bypassFreshnessGate && await this._apiCache.IsFreshAsync(key);

            if (!isFresh)
            {
                Task warmingTask = WarmEntryAsync(
                    key: key,
                    fetcher: fetcher,
                    logger: this._logger,
                    cancellationToken: cancellationToken
                );
                tasks.Add(warmingTask);
            }
        }

        await Task.WhenAll(tasks);
    }

    private static async Task WarmEntryAsync(
        string key,
        Func<CancellationToken, Task> fetcher,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        await fetcher(cancellationToken);
        logger.CacheWarmed(key);
    }

    private IReadOnlyList<FetcherRegistration> BuildFetchers()
    {
        return
        [
            new("defillama_pools", this.WarmLlamaPoolsAsync, BypassFreshnessGate: true),
            new("pendle_pools", this.WarmPendlePoolsAsync, BypassFreshnessGate: true),
            new("defillama_hacks", this.WarmHacksAsync, BypassFreshnessGate: false),
            new("defillama_protocols", this.WarmProtocolsAsync, BypassFreshnessGate: false),
            new("coingecko_stablecoins", this.WarmStablecoinsAsync, BypassFreshnessGate: false),
            new("coingecko_coin_list", this.WarmCoinListAsync, BypassFreshnessGate: false),
            new("chainlink_price_feeds", this.WarmChainlinkPriceFeedsAsync, BypassFreshnessGate: true),
        ];
    }

    private async Task WarmLlamaPoolsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RawPool> data = await this._llamaPoolsClient.FetchPoolsAsync(cancellationToken);
        await this._poolStorage.StorePoolsAsync(pools: data, dataDate: null, cancellationToken: cancellationToken);
    }

    private async Task WarmPendlePoolsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PendleMarket> data = await this._pendleClient.FetchMarketsAsync(cancellationToken);
        await this._pendleStorage.StoreMarketsAsync(
            markets: data,
            dataDate: null,
            cancellationToken: cancellationToken
        );
    }

    private async Task WarmHacksAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RawHack> data = await this._hacksClient.FetchHacksAsync(cancellationToken);
        await this._hackStorage.StoreHacksAsync(hacks: data, dataDate: null, cancellationToken: cancellationToken);
    }

    private async Task WarmProtocolsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RawProtocol> data = await this._protocolsClient.FetchProtocolsAsync(cancellationToken);
        await this._protocolStorage.StoreProtocolsAsync(
            protocols: data,
            dataDate: null,
            cancellationToken: cancellationToken
        );
    }

    private async Task WarmStablecoinsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CoinGeckoStablecoin> data = await this._coinGeckoClient.FetchStablecoinsAsync(cancellationToken);
        await this._coinGeckoStablecoinStorage.StoreAsync(
            stablecoins: data,
            dataDate: null,
            cancellationToken: cancellationToken
        );
    }

    private async Task WarmCoinListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CoinGeckoCoinPlatforms> data = await this._coinGeckoClient.FetchCoinListAsync(cancellationToken);
        await this._coinGeckoStorage.StoreAsync(coins: data, dataDate: null, cancellationToken: cancellationToken);
    }

    private async Task WarmChainlinkPriceFeedsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ChainlinkPriceFeed> data = await this._chainlinkClient.FetchStablecoinsAsync(cancellationToken);
        await this._chainlinkStorage.StoreAsync(feeds: data, dataDate: null, cancellationToken: cancellationToken);
    }

    /// <summary>
    ///     A cache-warming fetcher and its freshness-gating behaviour.
    /// </summary>
    /// <param name="Key">The cache key used for freshness checks and logging.</param>
    /// <param name="Fetcher">The delegate that performs the fetch and store.</param>
    /// <param name="BypassFreshnessGate">
    ///     <see langword="true" /> when the fetcher writes straight to a dedicated storage
    ///     service rather than through <see cref="ApiCacheService" />, so nothing ever marks it
    ///     fresh and it must always run rather than being skipped by the freshness check.
    /// </param>
    [DebuggerDisplay("{Key} bypass={BypassFreshnessGate}")]
    private readonly record struct FetcherRegistration(
        string Key,
        Func<CancellationToken, Task> Fetcher,
        bool BypassFreshnessGate
    );
}
