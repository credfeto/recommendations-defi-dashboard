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
internal sealed class HoneypotIsTokenSecuritySyncRowMapper : IMapper<IReadOnlyList<HoneypotIsTokenSecuritySyncRow>>
{
    private const string TABLE_TYPE = "HoneypotIs.TokenSecurityRow";

    public static IReadOnlyList<HoneypotIsTokenSecuritySyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to HoneypotIsTokenSecuritySyncRow list");
    }

    public static void MapToDb(IReadOnlyList<HoneypotIsTokenSecuritySyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (HoneypotIsTokenSecuritySyncRow row in value)
        {
            records.Rows.Add(row.Chain, row.Address, row.IsHoneypot, row.BuyTax, row.SellTax, row.SimulationSuccess);
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
        records.Columns.Add("IsHoneypot", typeof(bool));
        records.Columns.Add("BuyTax", typeof(double));
        records.Columns.Add("SellTax", typeof(double));
        records.Columns.Add("SimulationSuccess", typeof(bool));

        return records;
    }
}
