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
    private static readonly ColumnDefinition[] COLUMNS =
    [
        new(name: "Address", type: typeof(string)),
        new(name: "ChainId", type: typeof(int)),
        new(name: "SimpleSymbol", type: typeof(string)),
        new(name: "Expiry", type: typeof(string)),
        new(name: "IsActive", type: typeof(bool)),
        new(name: "LiquidityUsd", type: typeof(double)),
        new(name: "AggregatedApy", type: typeof(double)),
        new(name: "UnderlyingApy", type: typeof(double)),
        new(name: "PendleApy", type: typeof(double)),
        new(name: "LpRewardApy", type: typeof(double)),
        new(name: "SwapFeeApy", type: typeof(double)),
        new(name: "TradingVolumeUsd", type: typeof(double)),
    ];

    [SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007: Handle disposal correctly",
        Justification = "Disposed by owner"
    )]
    private static DataTable CreateTableHeader()
    {
        DataTable records = new(TABLE_TYPE);

        foreach (ColumnDefinition column in COLUMNS)
        {
            records.Columns.Add(columnName: column.Name, type: column.Type);
        }

        return records;
    }

    private readonly struct ColumnDefinition
    {
        public ColumnDefinition(
            string name,
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties
            )]
                Type type
        )
        {
            this.Name = name;
            this.Type = type;
        }

        public string Name { get; }

        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties
        )]
        public Type Type { get; }
    }
}
