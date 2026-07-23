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

    private static readonly ColumnDefinition[] COLUMNS =
    [
        new(name: "Chain", type: typeof(string)),
        new(name: "Address", type: typeof(string)),
        new(name: "ParentAddress", type: typeof(string)),
        new(name: "IsOpenSource", type: typeof(bool)),
        new(name: "IsHoneypot", type: typeof(bool)),
        new(name: "IsProxy", type: typeof(bool)),
        new(name: "BuyTax", type: typeof(double)),
        new(name: "SellTax", type: typeof(double)),
        new(name: "TransferTax", type: typeof(double)),
        new(name: "CannotBuy", type: typeof(bool)),
        new(name: "HoneypotWithSameCreator", type: typeof(bool)),
        new(name: "TokenName", type: typeof(string)),
        new(name: "TokenSymbol", type: typeof(string)),
    ];

    [SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007: Handle disposal correctly",
        Justification = "Disposed by owner"
    )]
    private static DataTable CreateTableHeader()
    {
        DataTable records = new(TABLE_TYPE);

        foreach (ColumnDefinition column in COLUMNS)
        {
            records.Columns.Add(columnName: column.Name, type: column.Type);
        }

        return records;
    }

    private readonly struct ColumnDefinition
    {
        public ColumnDefinition(
            string name,
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties
            )]
                Type type
        )
        {
            this.Name = name;
            this.Type = type;
        }

        public string Name { get; }

        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties
        )]
        public Type Type { get; }
    }
}
