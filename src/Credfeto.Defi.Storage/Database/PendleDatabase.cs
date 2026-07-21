using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database.Interfaces;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage.Database;

internal static partial class PendleDatabase
{
    [SqlObjectMap("Pendle.Market_Sync", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask Market_SyncAsync(
        DbConnection connection,
        [SqlFieldMap<PendleMarketSyncRowMapper, IReadOnlyList<PendleMarketSyncRow>>]
            IReadOnlyList<PendleMarketSyncRow> markets,
        [SqlFieldMap<PendleMarketCategorySyncRowMapper, IReadOnlyList<PendleMarketCategorySyncRow>>]
            IReadOnlyList<PendleMarketCategorySyncRow> categories,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    [SqlObjectMap("Pendle.Market_GetAll", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask<IReadOnlyList<PendleMarketRow>> Market_GetAllAsync(
        DbConnection connection,
        CancellationToken cancellationToken
    );

    [SqlObjectMap("Pendle.MarketCategory_GetAll", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask<IReadOnlyList<PendleMarketCategoryRow>> MarketCategory_GetAllAsync(
        DbConnection connection,
        CancellationToken cancellationToken
    );
}
