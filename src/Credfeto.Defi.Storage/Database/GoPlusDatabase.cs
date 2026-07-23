using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database.Interfaces;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage.Database;

internal static partial class GoPlusDatabase
{
    [SqlObjectMap("GoPlus.TokenSecurity_Sync", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask TokenSecurity_SyncAsync(
        DbConnection connection,
        [SqlFieldMap<GoPlusTokenSecuritySyncRowMapper, IReadOnlyList<GoPlusTokenSecuritySyncRow>>]
            IReadOnlyList<GoPlusTokenSecuritySyncRow> rows,
        CancellationToken cancellationToken
    );

    [SqlObjectMap(
        "GoPlus.TokenSecurity_GetByChainAndAddress",
        SqlObjectType.STORED_PROCEDURE,
        SqlDialect.MICROSOFT_SQL_SERVER
    )]
    public static partial ValueTask<GoPlusTokenSecurityRow?> TokenSecurity_GetByChainAndAddressAsync(
        DbConnection connection,
        string chain,
        string address,
        CancellationToken cancellationToken
    );

    [SqlObjectMap(
        "GoPlus.TokenSecurity_GetChildrenByParentAddress",
        SqlObjectType.STORED_PROCEDURE,
        SqlDialect.MICROSOFT_SQL_SERVER
    )]
    public static partial ValueTask<
        IReadOnlyList<GoPlusTokenSecurityRow>
    > TokenSecurity_GetChildrenByParentAddressAsync(
        DbConnection connection,
        string chain,
        string parentAddress,
        CancellationToken cancellationToken
    );
}
