using System;
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

public sealed class CoinGeckoStablecoinStorageService : ICoinGeckoStablecoinStorageService
{
    private readonly IDatabase _database;

    public CoinGeckoStablecoinStorageService(IDatabase database)
    {
        this._database = database;
    }

    public async ValueTask StoreAsync(
        IReadOnlyList<CoinGeckoStablecoin> stablecoins,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<CoinGeckoStablecoinSyncRow> rows = BuildSyncRows(stablecoins);

        await this._database.ExecuteAsync(action: SyncAsync, cancellationToken: cancellationToken);

        ValueTask SyncAsync(DbConnection c, CancellationToken ct) =>
            CoinGeckoDatabase.Stablecoin_SyncAsync(
                connection: c,
                rows: rows,
                dataDate: dataDate,
                cancellationToken: ct
            );
    }

    public async ValueTask<IReadOnlyList<CoinGeckoStablecoin>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CoinGeckoStablecoinRow> rows = await this._database.ExecuteAsync(
            action: CoinGeckoDatabase.Stablecoin_GetAllAsync,
            cancellationToken: cancellationToken
        );

        return MapToStablecoins(rows);
    }

    private static IReadOnlyList<CoinGeckoStablecoinSyncRow> BuildSyncRows(
        IReadOnlyList<CoinGeckoStablecoin> stablecoins
    )
    {
        CoinGeckoStablecoinSyncRow[] rows = new CoinGeckoStablecoinSyncRow[stablecoins.Count];

        for (int i = 0; i < stablecoins.Count; i++)
        {
            CoinGeckoStablecoin coin = stablecoins[i];
            rows[i] = new CoinGeckoStablecoinSyncRow(
                Id: coin.Id,
                Symbol: coin.Symbol,
                Name: coin.Name,
                CurrentPrice: coin.CurrentPrice
            );
        }

        return rows;
    }

    private static IReadOnlyList<CoinGeckoStablecoin> MapToStablecoins(IReadOnlyList<CoinGeckoStablecoinRow> rows)
    {
        CoinGeckoStablecoin[] stablecoins = new CoinGeckoStablecoin[rows.Count];

        for (int i = 0; i < rows.Count; i++)
        {
            CoinGeckoStablecoinRow row = rows[i];
            stablecoins[i] = new CoinGeckoStablecoin
            {
                Id = row.Id,
                Symbol = row.Symbol,
                Name = row.Name,
                CurrentPrice = row.CurrentPrice,
            };
        }

        return stablecoins;
    }
}
