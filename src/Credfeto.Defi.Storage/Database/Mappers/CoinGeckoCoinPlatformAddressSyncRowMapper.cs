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
internal sealed class CoinGeckoCoinPlatformAddressSyncRowMapper
    : IMapper<IReadOnlyList<CoinGeckoCoinPlatformAddressSyncRow>>
{
    private const string TABLE_TYPE = "CoinGecko.CoinPlatformAddressRow";

    public static IReadOnlyList<CoinGeckoCoinPlatformAddressSyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to CoinGeckoCoinPlatformAddressSyncRow list");
    }

    public static void MapToDb(IReadOnlyList<CoinGeckoCoinPlatformAddressSyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (CoinGeckoCoinPlatformAddressSyncRow row in value)
        {
            records.Rows.Add(row.CoinId, row.Platform, row.ContractAddress);
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
        records.Columns.Add("CoinId", typeof(string));
        records.Columns.Add("Platform", typeof(string));
        records.Columns.Add("ContractAddress", typeof(string));

        return records;
    }
}
