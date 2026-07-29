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
internal sealed class DefiLlamaProtocolSyncRowMapper : IMapper<IReadOnlyList<DefiLlamaProtocolSyncRow>>
{
    private const string TABLE_TYPE = "DefiLlama.ProtocolRow";

    public static IReadOnlyList<DefiLlamaProtocolSyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to DefiLlamaProtocolSyncRow list");
    }

    public static void MapToDb(IReadOnlyList<DefiLlamaProtocolSyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (DefiLlamaProtocolSyncRow row in value)
        {
            records.Rows.Add(row.Slug, row.Audits is null ? DBNull.Value : row.Audits);
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
        records.Columns.Add("Slug", typeof(string));
        records.Columns.Add("Audits", typeof(string));

        return records;
    }
}
