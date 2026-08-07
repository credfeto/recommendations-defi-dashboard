using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database.Interfaces;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage.Database;

internal static partial class HoneypotIsDatabase
{
    [SqlObjectMap("HoneypotIs.TokenSecurity_Sync", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask TokenSecurity_SyncAsync(
        DbConnection connection,
        [SqlFieldMap<HoneypotIsTokenSecuritySyncRowMapper, IReadOnlyList<HoneypotIsTokenSecuritySyncRow>>]
            IReadOnlyList<HoneypotIsTokenSecuritySyncRow> rows,
        CancellationToken cancellationToken
    );

    [SqlObjectMap(
        "HoneypotIs.TokenSecurity_GetByChainAndAddress",
        SqlObjectType.STORED_PROCEDURE,
        SqlDialect.MICROSOFT_SQL_SERVER
    )]
    public static partial ValueTask<HoneypotIsTokenSecurityRow?> TokenSecurity_GetByChainAndAddressAsync(
        DbConnection connection,
        string chain,
        string address,
        CancellationToken cancellationToken
    );
}
