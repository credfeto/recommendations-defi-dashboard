using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database;
using Credfeto.Defi.ApiClients.Chainlink.Interfaces;
using Credfeto.Defi.ApiClients.CoinGecko.Interfaces;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
using Credfeto.Defi.ApiClients.Pendle.Interfaces;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage;
using Credfeto.Defi.Storage.Database.Rows;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Services.Tests;

public sealed class CacheWarmerServiceTests : TestBase
{
    private static readonly TimeSpan WarmupTimeout = TimeSpan.FromSeconds(30);

    private readonly IDatabase _database;
    private readonly IDefiLlamaHacksClient _hacksClient;
    private readonly IDefiLlamaStablecoinsClient _llamaStablecoinsClient;
    private readonly TaskCompletionSource _lastGatedFetcherStarted;
    private readonly FakeTimeProvider _timeProvider;

    public CacheWarmerServiceTests()
    {
        this._timeProvider = MockDateTimeSources.Past;
        this._database = GetSubstitute<IDatabase>();
        this._hacksClient = GetSubstitute<IDefiLlamaHacksClient>();
        this._llamaStablecoinsClient = GetSubstitute<IDefiLlamaStablecoinsClient>();
        this._lastGatedFetcherStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // DefiLlama stablecoins is the last registration, so its fetch starting means every freshness
        // check has already run.
        ReturnFrom(call: this._llamaStablecoinsClient.FetchStablecoinsAsync(Arg.Any<CancellationToken>()), result: () =>
        {
            this._lastGatedFetcherStarted.TrySetResult();

            return [];
        });
    }

    [Fact]
    public async Task StartAsync_WhenGatedEntriesFresh_SkipsThemAsync()
    {
        ApiCacheRow freshRow = new(Key: "key", Data: "[]", FetchedAt: this._timeProvider.GetUtcNow());

        // Fresh for hacks, protocols, CoinGecko stablecoins and coin list; the last gated entry is left as a
        // miss so its fetch signals that the freshness checks are complete.
        this.MockApiCacheLookups(freshRow, freshRow, freshRow, freshRow);

        CancellationToken cancellationToken = this.CancellationToken();

        await this.WarmAsync(cancellationToken);

        _ = await this._hacksClient.DidNotReceive().FetchHacksAsync(Arg.Any<CancellationToken>());
        await this.AssertDatabaseOnlyCalledWithAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_WhenGatedEntriesStale_FetchesThemAsync()
    {
        this.MockApiCacheLookups();

        CancellationToken cancellationToken = this.CancellationToken();

        await this.WarmAsync(cancellationToken);

        _ = await this._hacksClient.Received(1).FetchHacksAsync(cancellationToken);
        await this.AssertDatabaseOnlyCalledWithAsync(cancellationToken);
    }

    private void MockApiCacheLookups(params ApiCacheRow?[] rows)
    {
        // Rows are returned in lookup order; once they run out every further lookup is a cache miss.
        Queue<ApiCacheRow?> lookups = new(rows);

        ReturnFrom(
            call: this._database.ExecuteAsync(
                action: Arg.Any<Func<DbConnection, CancellationToken, ValueTask<ApiCacheRow?>>>(),
                cancellationToken: Arg.Any<CancellationToken>()
            ),
            result: () => lookups.TryDequeue(out ApiCacheRow? row) ? row : null
        );
    }

    private static void ReturnFrom<T>(ValueTask<T> call, Func<T> result)
    {
        call.Returns(_ => ValueTask.FromResult(result()));
    }

    private async Task WarmAsync(CancellationToken cancellationToken)
    {
        CacheWarmerService warmer = new(
            llamaPoolsClient: GetSubstitute<IDefiLlamaPoolsClient>(),
            pendleClient: GetSubstitute<IPendleMarketsClient>(),
            hacksClient: this._hacksClient,
            protocolsClient: GetSubstitute<IDefiLlamaProtocolsClient>(),
            coinGeckoClient: GetSubstitute<ICoinGeckoStablecoinsClient>(),
            chainlinkClient: GetSubstitute<IChainlinkStablecoinsClient>(),
            apiCache: new ApiCacheService(database: this._database, timeProvider: this._timeProvider),
            poolStorage: GetSubstitute<IDefiLlamaPoolStorage>(),
            protocolStorage: GetSubstitute<IDefiLlamaProtocolStorageService>(),
            hackStorage: GetSubstitute<IDefiLlamaHackStorageService>(),
            pendleStorage: GetSubstitute<IPendleMarketStorageService>(),
            chainlinkStorage: GetSubstitute<IChainlinkPriceFeedStorageService>(),
            coinGeckoStorage: GetSubstitute<ICoinGeckoCoinStorageService>(),
            coinGeckoStablecoinStorage: GetSubstitute<ICoinGeckoStablecoinStorageService>(),
            llamaStablecoinsClient: this._llamaStablecoinsClient,
            logger: GetSubstitute<ILogger<CacheWarmerService>>()
        );

        await warmer.StartAsync(cancellationToken);
        await this._lastGatedFetcherStarted.Task.WaitAsync(
            timeout: WarmupTimeout,
            cancellationToken: cancellationToken
        );
    }

    private async Task AssertDatabaseOnlyCalledWithAsync(CancellationToken cancellationToken)
    {
        // Five freshness checks plus the DefiLlama stablecoins cache lookup, every one with the caller's token.
        _ = await this
            ._database.Received(6)
            .ExecuteAsync(
                action: Arg.Any<Func<DbConnection, CancellationToken, ValueTask<ApiCacheRow?>>>(),
                cancellationToken: cancellationToken
            );
        _ = await this
            ._database.DidNotReceive()
            .ExecuteAsync(
                action: Arg.Any<Func<DbConnection, CancellationToken, ValueTask<ApiCacheRow?>>>(),
                cancellationToken: Arg.Is<CancellationToken>(token => token != cancellationToken)
            );
    }
}
