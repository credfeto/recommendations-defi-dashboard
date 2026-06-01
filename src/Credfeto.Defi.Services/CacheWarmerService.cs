using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.CoinGecko.Interfaces;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
using Credfeto.Defi.ApiClients.Pendle.Interfaces;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Database;
using Credfeto.Defi.Services.LoggingExtensions;
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
    private readonly ICoinGeckoStablecoinsClient _coinGeckoClient;
    private readonly IDefiLlamaHacksClient _hacksClient;
    private readonly IDefiLlamaPoolsClient _llamaPoolsClient;
    private readonly ILogger<CacheWarmerService> _logger;
    private readonly IPendleMarketsClient _pendleClient;
    private readonly IDefiLlamaProtocolsClient _protocolsClient;

    /// <summary>
    ///     Initialises a new instance of <see cref="CacheWarmerService" />.
    /// </summary>
    public CacheWarmerService(
        IDefiLlamaPoolsClient llamaPoolsClient,
        IPendleMarketsClient pendleClient,
        IDefiLlamaHacksClient hacksClient,
        IDefiLlamaProtocolsClient protocolsClient,
        ICoinGeckoStablecoinsClient coinGeckoClient,
        ApiCacheService apiCache,
        ILogger<CacheWarmerService> logger
    )
    {
        this._llamaPoolsClient = llamaPoolsClient;
        this._pendleClient = pendleClient;
        this._hacksClient = hacksClient;
        this._protocolsClient = protocolsClient;
        this._coinGeckoClient = coinGeckoClient;
        this._apiCache = apiCache;
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
        IReadOnlyList<(string Key, Func<CancellationToken, Task> Fetcher)> fetchers = this.BuildFetchers();

        List<Task> tasks = [];

        foreach ((string key, Func<CancellationToken, Task> fetcher) in fetchers)
        {
            bool isFresh = await this._apiCache.IsFreshAsync(key);

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

    private IReadOnlyList<(string Key, Func<CancellationToken, Task> Fetcher)> BuildFetchers()
    {
        return
        [
            ("defillama_pools", this.WarmLlamaPoolsAsync),
            ("pendle_pools", this.WarmPendlePoolsAsync),
            ("defillama_hacks", this.WarmHacksAsync),
            ("defillama_protocols", this.WarmProtocolsAsync),
            ("coingecko_stablecoins", this.WarmStablecoinsAsync),
            ("coingecko_coin_list", this.WarmCoinListAsync),
        ];
    }

    private async Task WarmLlamaPoolsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RawPool> data = await this._llamaPoolsClient.FetchPoolsAsync(cancellationToken);
        _ = await this._apiCache.GetOrFetchAsync(
            key: "defillama_pools",
            fetcher: _ => new ValueTask<IReadOnlyList<RawPool>>(data),
            typeInfo: AppJsonContext.Default.IReadOnlyListRawPool,
            cancellationToken: cancellationToken
        );
    }

    private async Task WarmPendlePoolsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RawPool> data = await this._pendleClient.FetchMarketsAsync(cancellationToken);
        _ = await this._apiCache.GetOrFetchAsync(
            key: "pendle_pools",
            fetcher: _ => new ValueTask<IReadOnlyList<RawPool>>(data),
            typeInfo: AppJsonContext.Default.IReadOnlyListRawPool,
            cancellationToken: cancellationToken
        );
    }

    private async Task WarmHacksAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RawHack> data = await this._hacksClient.FetchHacksAsync(cancellationToken);
        _ = await this._apiCache.GetOrFetchAsync(
            key: "defillama_hacks",
            fetcher: _ => new ValueTask<IReadOnlyList<RawHack>>(data),
            typeInfo: AppJsonContext.Default.IReadOnlyListRawHack,
            cancellationToken: cancellationToken
        );
    }

    private async Task WarmProtocolsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RawProtocol> data = await this._protocolsClient.FetchProtocolsAsync(cancellationToken);
        _ = await this._apiCache.GetOrFetchAsync(
            key: "defillama_protocols",
            fetcher: _ => new ValueTask<IReadOnlyList<RawProtocol>>(data),
            typeInfo: AppJsonContext.Default.IReadOnlyListRawProtocol,
            cancellationToken: cancellationToken
        );
    }

    private async Task WarmStablecoinsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CoinGeckoStablecoin> data = await this._coinGeckoClient.FetchStablecoinsAsync(cancellationToken);
        _ = await this._apiCache.GetOrFetchAsync(
            key: "coingecko_stablecoins",
            fetcher: _ => new ValueTask<IReadOnlyList<CoinGeckoStablecoin>>(data),
            typeInfo: AppJsonContext.Default.IReadOnlyListCoinGeckoStablecoin,
            cancellationToken: cancellationToken
        );
    }

    private async Task WarmCoinListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CoinGeckoCoinPlatforms> data = await this._coinGeckoClient.FetchCoinListAsync(cancellationToken);
        _ = await this._apiCache.GetOrFetchAsync(
            key: "coingecko_coin_list",
            fetcher: _ => new ValueTask<IReadOnlyList<CoinGeckoCoinPlatforms>>(data),
            typeInfo: AppJsonContext.Default.IReadOnlyListCoinGeckoCoinPlatforms,
            cancellationToken: cancellationToken
        );
    }
}
