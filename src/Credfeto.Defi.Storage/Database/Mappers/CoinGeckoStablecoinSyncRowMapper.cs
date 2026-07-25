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
internal sealed class CoinGeckoStablecoinSyncRowMapper : IMapper<IReadOnlyList<CoinGeckoStablecoinSyncRow>>
{
    private const string TABLE_TYPE = "CoinGecko.StablecoinRow";

    public static IReadOnlyList<CoinGeckoStablecoinSyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to CoinGeckoStablecoinSyncRow list");
    }

    public static void MapToDb(IReadOnlyList<CoinGeckoStablecoinSyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (CoinGeckoStablecoinSyncRow row in value)
        {
            records.Rows.Add(
                row.Id,
                row.Symbol,
                row.Name,
                row.CurrentPrice.HasValue ? row.CurrentPrice.Value : DBNull.Value
            );
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
        records.Columns.Add("Id", typeof(string));
        records.Columns.Add("Symbol", typeof(string));
        records.Columns.Add("Name", typeof(string));
        records.Columns.Add("CurrentPrice", typeof(decimal));

        return records;
    }
}
