using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database.Interfaces;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage.Database;

internal static partial class CoinGeckoDatabase
{
    [SqlObjectMap("CoinGecko.Coin_Sync", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask Coin_SyncAsync(
        DbConnection connection,
        [SqlFieldMap<CoinGeckoCoinSyncRowMapper, IReadOnlyList<CoinGeckoCoinSyncRow>>]
            IReadOnlyList<CoinGeckoCoinSyncRow> coins,
        [SqlFieldMap<CoinGeckoCoinPlatformAddressSyncRowMapper, IReadOnlyList<CoinGeckoCoinPlatformAddressSyncRow>>]
            IReadOnlyList<CoinGeckoCoinPlatformAddressSyncRow> addresses,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    [SqlObjectMap("CoinGecko.Coin_GetAll", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask<IReadOnlyList<CoinGeckoCoinRow>> Coin_GetAllAsync(
        DbConnection connection,
        CancellationToken cancellationToken
    );

    [SqlObjectMap(
        "CoinGecko.CoinPlatformAddress_GetAll",
        SqlObjectType.STORED_PROCEDURE,
        SqlDialect.MICROSOFT_SQL_SERVER
    )]
    public static partial ValueTask<IReadOnlyList<CoinGeckoCoinPlatformAddressRow>> CoinPlatformAddress_GetAllAsync(
        DbConnection connection,
        CancellationToken cancellationToken
    );
}
