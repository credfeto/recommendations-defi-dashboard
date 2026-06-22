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
internal sealed class DefiLlamaPoolUnderlyingTokenSyncRowMapper : IMapper<IReadOnlyList<DefiLlamaPoolTokenSyncRow>>
{
    private const string TABLE_TYPE = "DefiLlama.PoolUnderlyingTokenRow";

    public static IReadOnlyList<DefiLlamaPoolTokenSyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to DefiLlamaPoolTokenSyncRow list");
    }

    public static void MapToDb(IReadOnlyList<DefiLlamaPoolTokenSyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (DefiLlamaPoolTokenSyncRow row in value)
        {
            records.Rows.Add(row.PoolId, row.SortOrder, row.TokenAddress);
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
        records.Columns.Add("PoolId", typeof(string));
        records.Columns.Add("SortOrder", typeof(int));
        records.Columns.Add("TokenAddress", typeof(string));

        return records;
    }
}
