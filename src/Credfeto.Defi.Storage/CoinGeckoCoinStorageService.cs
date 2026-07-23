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

public sealed class CoinGeckoCoinStorageService : ICoinGeckoCoinStorageService
{
    private readonly IDatabase _database;

    public CoinGeckoCoinStorageService(IDatabase database)
    {
        this._database = database;
    }

    public async ValueTask StoreAsync(
        IReadOnlyList<CoinGeckoCoinPlatforms> coins,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<CoinGeckoCoinSyncRow> coinRows = BuildCoinRows(coins);
        IReadOnlyList<CoinGeckoCoinPlatformAddressSyncRow> addressRows = BuildAddressRows(coins);

        await this._database.ExecuteAsync(action: SyncAsync, cancellationToken: cancellationToken);

        ValueTask SyncAsync(DbConnection c, CancellationToken ct) =>
            CoinGeckoDatabase.Coin_SyncAsync(
                connection: c,
                coins: coinRows,
                addresses: addressRows,
                dataDate: dataDate,
                cancellationToken: ct
            );
    }

    public async ValueTask<IReadOnlyList<CoinGeckoCoinPlatforms>> GetAllAsync(CancellationToken cancellationToken)
    {
        ValueTask<IReadOnlyList<CoinGeckoCoinRow>> coinRowsTask = this._database.ExecuteAsync(
            action: CoinGeckoDatabase.Coin_GetAllAsync,
            cancellationToken: cancellationToken
        );

        ValueTask<IReadOnlyList<CoinGeckoCoinPlatformAddressRow>> addressRowsTask = this._database.ExecuteAsync(
            action: CoinGeckoDatabase.CoinPlatformAddress_GetAllAsync,
            cancellationToken: cancellationToken
        );

        IReadOnlyList<CoinGeckoCoinRow> coinRows = await coinRowsTask;
        IReadOnlyList<CoinGeckoCoinPlatformAddressRow> addressRows = await addressRowsTask;

        return MapToCoins(coinRows: coinRows, addressRows: addressRows);
    }

    private static IReadOnlyList<CoinGeckoCoinSyncRow> BuildCoinRows(IReadOnlyList<CoinGeckoCoinPlatforms> coins)
    {
        CoinGeckoCoinSyncRow[] rows = new CoinGeckoCoinSyncRow[coins.Count];

        for (int i = 0; i < coins.Count; i++)
        {
            CoinGeckoCoinPlatforms coin = coins[i];
            rows[i] = new CoinGeckoCoinSyncRow(Id: coin.Id, Symbol: coin.Symbol);
        }

        return rows;
    }

    private static IReadOnlyList<CoinGeckoCoinPlatformAddressSyncRow> BuildAddressRows(
        IReadOnlyList<CoinGeckoCoinPlatforms> coins
    )
    {
        List<CoinGeckoCoinPlatformAddressSyncRow> rows = [];

        foreach (CoinGeckoCoinPlatforms coin in coins)
        {
            if (coin.Platforms is null)
            {
                continue;
            }

            foreach (KeyValuePair<string, string> platform in coin.Platforms)
            {
                if (string.IsNullOrEmpty(platform.Value))
                {
                    continue;
                }

                rows.Add(
                    new CoinGeckoCoinPlatformAddressSyncRow(
                        CoinId: coin.Id,
                        Platform: platform.Key,
                        ContractAddress: platform.Value
                    )
                );
            }
        }

        return rows;
    }

    private static IReadOnlyList<CoinGeckoCoinPlatforms> MapToCoins(
        IReadOnlyList<CoinGeckoCoinRow> coinRows,
        IReadOnlyList<CoinGeckoCoinPlatformAddressRow> addressRows
    )
    {
        Dictionary<string, Dictionary<string, string>> platformsByCoin = GroupPlatformsByCoin(addressRows);

        CoinGeckoCoinPlatforms[] result = new CoinGeckoCoinPlatforms[coinRows.Count];

        for (int i = 0; i < coinRows.Count; i++)
        {
            CoinGeckoCoinRow row = coinRows[i];

            _ = platformsByCoin.TryGetValue(key: row.Id, value: out Dictionary<string, string>? platforms);

            result[i] = new CoinGeckoCoinPlatforms
            {
                Id = row.Id,
                Symbol = row.Symbol,
                Platforms = platforms,
            };
        }

        return result;
    }

    private static Dictionary<string, Dictionary<string, string>> GroupPlatformsByCoin(
        IReadOnlyList<CoinGeckoCoinPlatformAddressRow> addressRows
    )
    {
        Dictionary<string, Dictionary<string, string>> result = [];

        foreach (CoinGeckoCoinPlatformAddressRow row in addressRows)
        {
            if (!result.TryGetValue(key: row.CoinId, value: out Dictionary<string, string>? platforms))
            {
                platforms = [];
                result[row.CoinId] = platforms;
            }

            platforms[row.Platform] = row.ContractAddress;
        }

        return result;
    }
}
