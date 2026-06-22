using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database.Interfaces;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage.Database;

internal static partial class DefiLlamaDatabase
{
    [SqlObjectMap("DefiLlama.Pool_Sync", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask Pool_SyncAsync(
        DbConnection connection,
        [SqlFieldMap<DefiLlamaPoolSyncRowMapper, IReadOnlyList<DefiLlamaPoolSyncRow>>]
            IReadOnlyList<DefiLlamaPoolSyncRow> rows,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    [SqlObjectMap("DefiLlama.PoolRewardToken_Sync", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask PoolRewardToken_SyncAsync(
        DbConnection connection,
        [SqlFieldMap<DefiLlamaPoolRewardTokenSyncRowMapper, IReadOnlyList<DefiLlamaPoolTokenSyncRow>>]
            IReadOnlyList<DefiLlamaPoolTokenSyncRow> rows,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    [SqlObjectMap(
        "DefiLlama.PoolUnderlyingToken_Sync",
        SqlObjectType.STORED_PROCEDURE,
        SqlDialect.MICROSOFT_SQL_SERVER
    )]
    public static partial ValueTask PoolUnderlyingToken_SyncAsync(
        DbConnection connection,
        [SqlFieldMap<DefiLlamaPoolUnderlyingTokenSyncRowMapper, IReadOnlyList<DefiLlamaPoolTokenSyncRow>>]
            IReadOnlyList<DefiLlamaPoolTokenSyncRow> rows,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    );

    [SqlObjectMap("DefiLlama.Pool_GetAll", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask<IReadOnlyList<DefiLlamaPoolRow>> Pool_GetAllAsync(
        DbConnection connection,
        CancellationToken cancellationToken
    );

    [SqlObjectMap("DefiLlama.PoolRewardToken_GetAll", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask<IReadOnlyList<DefiLlamaPoolRewardTokenRow>> PoolRewardToken_GetAllAsync(
        DbConnection connection,
        CancellationToken cancellationToken
    );

    [SqlObjectMap(
        "DefiLlama.PoolUnderlyingToken_GetAll",
        SqlObjectType.STORED_PROCEDURE,
        SqlDialect.MICROSOFT_SQL_SERVER
    )]
    public static partial ValueTask<IReadOnlyList<DefiLlamaPoolUnderlyingTokenRow>> PoolUnderlyingToken_GetAllAsync(
        DbConnection connection,
        CancellationToken cancellationToken
    );
}
