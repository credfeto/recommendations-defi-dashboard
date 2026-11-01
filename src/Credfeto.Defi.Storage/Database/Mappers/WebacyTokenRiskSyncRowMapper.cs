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
internal sealed class WebacyTokenRiskSyncRowMapper : IMapper<IReadOnlyList<WebacyTokenRiskSyncRow>>
{
    private const string TABLE_TYPE = "Webacy.TokenRiskRow";

    public static IReadOnlyList<WebacyTokenRiskSyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to WebacyTokenRiskSyncRow list");
    }

    public static void MapToDb(IReadOnlyList<WebacyTokenRiskSyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (WebacyTokenRiskSyncRow row in value)
        {
            records.Rows.Add(
                row.Chain,
                row.Address,
                row.RiskScore,
                row.RiskLevel,
                row.IssueCount,
                row.HighRiskIssueCount,
                row.MediumRiskIssueCount
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

        records.Columns.Add("Chain", typeof(string));
        records.Columns.Add("Address", typeof(string));
        records.Columns.Add("RiskScore", typeof(double));
        records.Columns.Add("RiskLevel", typeof(string));
        records.Columns.Add("IssueCount", typeof(int));
        records.Columns.Add("HighRiskIssueCount", typeof(int));
        records.Columns.Add("MediumRiskIssueCount", typeof(int));

        return records;
    }
}
