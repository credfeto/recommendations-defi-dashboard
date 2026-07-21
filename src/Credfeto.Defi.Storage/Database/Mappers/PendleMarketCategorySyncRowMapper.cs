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
internal sealed class PendleMarketCategorySyncRowMapper : IMapper<IReadOnlyList<PendleMarketCategorySyncRow>>
{
    private const string TABLE_TYPE = "Pendle.MarketCategoryRow";

    public static IReadOnlyList<PendleMarketCategorySyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to PendleMarketCategorySyncRow list");
    }

    public static void MapToDb(IReadOnlyList<PendleMarketCategorySyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (PendleMarketCategorySyncRow row in value)
        {
            records.Rows.Add(row.Address, row.ChainId, row.CategoryId);
        }

        SqlParameter tvpParam = (SqlParameter)parameter;
        tvpParam.SqlDbType = SqlDbType.Structured;
        tvpParam.Value = records;
    }

    [SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007: Handle disposal correctly",
        Justification = "Disposed by owner"
    )]
    private static DataTable CreateTableHeader()
    {
        DataTable records = new(TABLE_TYPE);
        records.Columns.Add("Address", typeof(string));
        records.Columns.Add("ChainId", typeof(int));
        records.Columns.Add("CategoryId", typeof(string));

        return records;
    }
}
