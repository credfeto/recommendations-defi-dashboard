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
internal sealed class ChainlinkPriceFeedSyncRowMapper : IMapper<IReadOnlyList<ChainlinkPriceFeedSyncRow>>
{
    private const string TABLE_TYPE = "Chainlink.PriceFeedRow";

    public static IReadOnlyList<ChainlinkPriceFeedSyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to ChainlinkPriceFeedSyncRow list");
    }

    public static void MapToDb(IReadOnlyList<ChainlinkPriceFeedSyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (ChainlinkPriceFeedSyncRow row in value)
        {
            records.Rows.Add(row.Symbol, row.CurrentPrice);
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
        records.Columns.Add("Symbol", typeof(string));
        records.Columns.Add("CurrentPrice", typeof(decimal));

        return records;
    }
}
