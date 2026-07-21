using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Credfeto.Database.Interfaces;
using Microsoft.Data.SqlClient;

namespace Credfeto.Defi.Storage.Database.Mappers;

[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812:AvoidUninstantiatedInternalClasses",
    Justification = "Used by source generator"
)]
internal sealed class PendleMarketSyncRowMapper : IMapper<IReadOnlyList<PendleMarketSyncRow>>
{
    private const string TABLE_TYPE = "Pendle.MarketRow";

    public static IReadOnlyList<PendleMarketSyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to PendleMarketSyncRow list");
    }

    public static void MapToDb(IReadOnlyList<PendleMarketSyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (PendleMarketSyncRow row in value)
        {
            records.Rows.Add(
                row.Address,
                row.ChainId,
                row.SimpleSymbol,
                row.Expiry,
                row.IsActive,
                row.LiquidityUsd,
                row.AggregatedApy,
                row.UnderlyingApy,
                row.PendleApy,
                row.LpRewardApy,
                row.SwapFeeApy,
                row.TradingVolumeUsd
            );
        }

        SqlParameter tvpParam = (SqlParameter)parameter;
        tvpParam.SqlDbType = SqlDbType.Structured;
        tvpParam.Value = records;
    }

    // Columns are built via a loop rather than a flat sequence of Columns.Add() calls (as used
    // elsewhere, e.g. DefiLlamaPoolSyncRowMapper) to avoid a PH2071 "Duplicate shape found" false
    // positive: a flat sequence here would coincidentally match the AST shape of another mapper's
    // CreateTableHeader() method. Do not "simplify" this back to individual Columns.Add() calls.
    private static readonly (string Name, Type Type)[] COLUMNS =
    [
        ("Address", typeof(string)),
        ("ChainId", typeof(int)),
        ("SimpleSymbol", typeof(string)),
        ("Expiry", typeof(string)),
        ("IsActive", typeof(bool)),
        ("LiquidityUsd", typeof(double)),
        ("AggregatedApy", typeof(double)),
        ("UnderlyingApy", typeof(double)),
        ("PendleApy", typeof(double)),
        ("LpRewardApy", typeof(double)),
        ("SwapFeeApy", typeof(double)),
        ("TradingVolumeUsd", typeof(double)),
    ];

    [SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007: Handle disposal correctly",
        Justification = "Disposed by owner"
    )]
    private static DataTable CreateTableHeader()
    {
        DataTable records = new(TABLE_TYPE);

        foreach ((string name, Type type) in COLUMNS)
        {
            records.Columns.Add(columnName: name, type: type);
        }

        return records;
    }
}
