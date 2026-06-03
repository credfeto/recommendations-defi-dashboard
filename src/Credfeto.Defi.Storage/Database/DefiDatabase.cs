using System;
using System.Collections.Generic;
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

    [SqlObjectMap(
        "ContractSecurity_GetByChainAndAddress",
        SqlObjectType.STORED_PROCEDURE,
        SqlDialect.MICROSOFT_SQL_SERVER
    )]
    public static partial ValueTask<ContractSecurityRow?> ContractSecurity_GetByChainAndAddressAsync(
        DbConnection connection,
        string chain,
        string address,
        CancellationToken cancellationToken
    );

    [SqlObjectMap(
        "ContractSecurity_GetChildrenByParentAddress",
        SqlObjectType.STORED_PROCEDURE,
        SqlDialect.MICROSOFT_SQL_SERVER
    )]
    public static partial ValueTask<IReadOnlyList<ContractSecurityRow>> ContractSecurity_GetChildrenByParentAddressAsync(
        DbConnection connection,
        string chain,
        string parentAddress,
        CancellationToken cancellationToken
    );

    [SqlObjectMap("ContractSecurity_Upsert", SqlObjectType.STORED_PROCEDURE, SqlDialect.MICROSOFT_SQL_SERVER)]
    public static partial ValueTask ContractSecurity_UpsertAsync(
        DbConnection connection,
        string chain,
        string address,
        string? parentAddress,
        bool? isOpenSource,
        bool? isHoneypot,
        bool? isProxy,
        double? buyTax,
        double? sellTax,
        double? transferTax,
        bool? cannotBuy,
        bool? honeypotWithSameCreator,
        string? tokenName,
        string? tokenSymbol,
        DateTimeOffset checkedAt,
        CancellationToken cancellationToken
    );
}
