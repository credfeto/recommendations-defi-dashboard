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
internal sealed class GoPlusTokenSecuritySyncRowMapper : IMapper<IReadOnlyList<GoPlusTokenSecuritySyncRow>>
{
    private const string TABLE_TYPE = "GoPlus.TokenSecurityRow";

    public static IReadOnlyList<GoPlusTokenSecuritySyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to GoPlusTokenSecuritySyncRow list");
    }

    public static void MapToDb(IReadOnlyList<GoPlusTokenSecuritySyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (GoPlusTokenSecuritySyncRow row in value)
        {
            records.Rows.Add(
                row.Chain,
                row.Address,
                row.ParentAddress,
                row.IsOpenSource,
                row.IsHoneypot,
                row.IsProxy,
                row.BuyTax,
                row.SellTax,
                row.TransferTax,
                row.CannotBuy,
                row.HoneypotWithSameCreator,
                row.TokenName,
                row.TokenSymbol
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
        records.Columns.Add("ParentAddress", typeof(string));
        records.Columns.Add("IsOpenSource", typeof(bool));
        records.Columns.Add("IsHoneypot", typeof(bool));
        records.Columns.Add("IsProxy", typeof(bool));
        records.Columns.Add("BuyTax", typeof(double));
        records.Columns.Add("SellTax", typeof(double));
        records.Columns.Add("TransferTax", typeof(double));
        records.Columns.Add("CannotBuy", typeof(bool));
        records.Columns.Add("HoneypotWithSameCreator", typeof(bool));
        records.Columns.Add("TokenName", typeof(string));
        records.Columns.Add("TokenSymbol", typeof(string));

        return records;
    }
}
