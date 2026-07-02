using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database.Interfaces;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage.Database;

internal static partial class ChainlinkDatabase
{
    [SqlObjectMap("Chainlink.PriceFeed_Sync", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask PriceFeed_SyncAsync(
        DbConnection connection,
        [SqlFieldMap<ChainlinkPriceFeedSyncRowMapper, IReadOnlyList<ChainlinkPriceFeedSyncRow>>]
            IReadOnlyList<ChainlinkPriceFeedSyncRow> rows,
        CancellationToken cancellationToken
    );

    [SqlObjectMap("Chainlink.PriceFeed_GetAll", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask<IReadOnlyList<ChainlinkPriceFeedRow>> PriceFeed_GetAllAsync(
        DbConnection connection,
        CancellationToken cancellationToken
    );
}
