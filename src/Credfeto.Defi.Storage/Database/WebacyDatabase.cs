using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database.Interfaces;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage.Database;

internal static partial class WebacyDatabase
{
    [SqlObjectMap("Webacy.TokenRisk_Sync", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask TokenRisk_SyncAsync(
        DbConnection connection,
        [SqlFieldMap<WebacyTokenRiskSyncRowMapper, IReadOnlyList<WebacyTokenRiskSyncRow>>]
            IReadOnlyList<WebacyTokenRiskSyncRow> rows,
        CancellationToken cancellationToken
    );

    [SqlObjectMap(
        "Webacy.TokenRisk_GetByChainAndAddress",
        SqlObjectType.STORED_PROCEDURE,
        SqlDialect.MICROSOFT_SQL_SERVER
    )]
    public static partial ValueTask<WebacyTokenRiskRow?> TokenRisk_GetByChainAndAddressAsync(
        DbConnection connection,
        string chain,
        string address,
        CancellationToken cancellationToken
    );
}
