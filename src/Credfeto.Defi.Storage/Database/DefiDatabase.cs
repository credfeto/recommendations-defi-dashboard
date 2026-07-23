using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database.Interfaces;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage.Database;

internal static partial class DefiDatabase
{
    [SqlObjectMap("ApiCache_GetByKey", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask<ApiCacheRow?> ApiCache_GetByKeyAsync(
        DbConnection connection,
        string key,
        CancellationToken cancellationToken
    );

    [SqlObjectMap("ApiCache_Upsert", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask ApiCache_UpsertAsync(
        DbConnection connection,
        string key,
        string data,
        DateTimeOffset fetchedAt,
        CancellationToken cancellationToken
    );
}
