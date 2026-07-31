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
internal sealed class DefiLlamaHackSyncRowMapper : IMapper<IReadOnlyList<DefiLlamaHackSyncRow>>
{
    private const string TABLE_TYPE = "DefiLlama.HackRow";

    public static IReadOnlyList<DefiLlamaHackSyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to DefiLlamaHackSyncRow list");
    }

    public static void MapToDb(IReadOnlyList<DefiLlamaHackSyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (DefiLlamaHackSyncRow row in value)
        {
            records.Rows.Add(
                row.Name,
                row.HackDate,
                row.Classification is null ? DBNull.Value : row.Classification,
                row.Technique is null ? DBNull.Value : row.Technique,
                row.Amount,
                row.Source,
                row.ParentProtocolId is null ? DBNull.Value : row.ParentProtocolId
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
        records.Columns.Add("Name", typeof(string));
        records.Columns.Add("HackDate", typeof(DateTimeOffset));
        records.Columns.Add("Classification", typeof(string));
        records.Columns.Add("Technique", typeof(string));
        records.Columns.Add("Amount", typeof(decimal));
        records.Columns.Add("Source", typeof(string));
        records.Columns.Add("ParentProtocolId", typeof(string));

        return records;
    }
}
