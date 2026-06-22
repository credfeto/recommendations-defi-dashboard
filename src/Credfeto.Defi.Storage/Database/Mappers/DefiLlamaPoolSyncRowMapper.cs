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
internal sealed class DefiLlamaPoolSyncRowMapper : IMapper<IReadOnlyList<DefiLlamaPoolSyncRow>>
{
    private const string TABLE_TYPE = "DefiLlama.PoolRow";

    public static IReadOnlyList<DefiLlamaPoolSyncRow> MapFromDb(object value)
    {
        throw new NotSupportedException("Cannot map from database to DefiLlamaPoolSyncRow list");
    }

    public static void MapToDb(IReadOnlyList<DefiLlamaPoolSyncRow> value, DbParameter parameter)
    {
        DataTable records = CreateTableHeader();

        foreach (DefiLlamaPoolSyncRow row in value)
        {
            records.Rows.Add(
                row.PoolId,
                row.Chain,
                row.Project,
                row.Symbol,
                row.TvlUsd,
                row.ApyBase,
                row.ApyReward,
                row.Apy,
                row.ApyPct1D,
                row.ApyPct7D,
                row.ApyPct30D,
                row.Stablecoin,
                row.IlRisk,
                row.Exposure,
                row.PredictedClass,
                row.PredictedProbability,
                row.BinnedConfidence,
                row.PoolMeta,
                row.Mu,
                row.Sigma,
                row.Count,
                row.Outlier,
                row.Il7d,
                row.ApyBase7d,
                row.ApyMean30d,
                row.VolumeUsd1d,
                row.VolumeUsd7d,
                row.ApyBaseInception
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
        records.Columns.Add("PoolId", typeof(string));
        records.Columns.Add("Chain", typeof(string));
        records.Columns.Add("Project", typeof(string));
        records.Columns.Add("Symbol", typeof(string));
        records.Columns.Add("TvlUsd", typeof(double));
        records.Columns.Add("ApyBase", typeof(double));
        records.Columns.Add("ApyReward", typeof(double));
        records.Columns.Add("Apy", typeof(double));
        records.Columns.Add("ApyPct1D", typeof(double));
        records.Columns.Add("ApyPct7D", typeof(double));
        records.Columns.Add("ApyPct30D", typeof(double));
        records.Columns.Add("Stablecoin", typeof(bool));
        records.Columns.Add("IlRisk", typeof(string));
        records.Columns.Add("Exposure", typeof(string));
        records.Columns.Add("PredictedClass", typeof(string));
        records.Columns.Add("PredictedProbability", typeof(double));
        records.Columns.Add("BinnedConfidence", typeof(double));
        records.Columns.Add("PoolMeta", typeof(string));
        records.Columns.Add("Mu", typeof(double));
        records.Columns.Add("Sigma", typeof(double));
        records.Columns.Add("Count", typeof(int));
        records.Columns.Add("Outlier", typeof(bool));
        records.Columns.Add("Il7d", typeof(double));
        records.Columns.Add("ApyBase7d", typeof(double));
        records.Columns.Add("ApyMean30d", typeof(double));
        records.Columns.Add("VolumeUsd1d", typeof(double));
        records.Columns.Add("VolumeUsd7d", typeof(double));
        records.Columns.Add("ApyBaseInception", typeof(double));

        return records;
    }
}
