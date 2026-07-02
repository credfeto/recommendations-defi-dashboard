using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage;

public sealed class ChainlinkPriceFeedStorageService : IChainlinkPriceFeedStorageService
{
    private readonly IDatabase _database;

    public ChainlinkPriceFeedStorageService(IDatabase database)
    {
        this._database = database;
    }

    public async ValueTask StoreAsync(IReadOnlyList<ChainlinkPriceFeed> feeds, CancellationToken cancellationToken)
    {
        IReadOnlyList<ChainlinkPriceFeedSyncRow> rows = BuildSyncRows(feeds);

        await this._database.ExecuteAsync(action: SyncAsync, cancellationToken: cancellationToken);

        ValueTask SyncAsync(DbConnection c, CancellationToken ct) =>
            ChainlinkDatabase.PriceFeed_SyncAsync(connection: c, rows: rows, cancellationToken: ct);
    }

    public async ValueTask<IReadOnlyList<ChainlinkPriceFeed>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ChainlinkPriceFeedRow> rows = await this._database.ExecuteAsync(
            action: ChainlinkDatabase.PriceFeed_GetAllAsync,
            cancellationToken: cancellationToken
        );

        return MapToFeeds(rows);
    }

    private static IReadOnlyList<ChainlinkPriceFeedSyncRow> BuildSyncRows(IReadOnlyList<ChainlinkPriceFeed> feeds)
    {
        ChainlinkPriceFeedSyncRow[] rows = new ChainlinkPriceFeedSyncRow[feeds.Count];

        for (int i = 0; i < feeds.Count; i++)
        {
            ChainlinkPriceFeed feed = feeds[i];
            rows[i] = new ChainlinkPriceFeedSyncRow(Symbol: feed.Symbol, CurrentPrice: feed.CurrentPrice);
        }

        return rows;
    }

    private static IReadOnlyList<ChainlinkPriceFeed> MapToFeeds(IReadOnlyList<ChainlinkPriceFeedRow> rows)
    {
        ChainlinkPriceFeed[] feeds = new ChainlinkPriceFeed[rows.Count];

        for (int i = 0; i < rows.Count; i++)
        {
            ChainlinkPriceFeedRow row = rows[i];
            feeds[i] = new ChainlinkPriceFeed(Symbol: row.Symbol, CurrentPrice: row.CurrentPrice);
        }

        return feeds;
    }
}
