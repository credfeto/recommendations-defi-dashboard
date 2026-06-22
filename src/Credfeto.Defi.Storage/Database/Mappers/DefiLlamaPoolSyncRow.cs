using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{PoolId} ({Project}/{Symbol}) Chain={Chain}")]
[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812:AvoidUninstantiatedInternalClasses",
    Justification = "Instantiated in DefiLlamaPoolStorageService"
)]
internal sealed record DefiLlamaPoolSyncRow(
    string PoolId,
    string Chain,
    string Project,
    string Symbol,
    double TvlUsd,
    double? ApyBase,
    double? ApyReward,
    double Apy,
    double? ApyPct1D,
    double? ApyPct7D,
    double? ApyPct30D,
    bool Stablecoin,
    string IlRisk,
    string? Exposure,
    string? PredictedClass,
    double? PredictedProbability,
    double? BinnedConfidence,
    string? PoolMeta,
    double Mu,
    double Sigma,
    int Count,
    bool Outlier,
    double? Il7d,
    double? ApyBase7d,
    double ApyMean30d,
    double? VolumeUsd1d,
    double? VolumeUsd7d,
    double? ApyBaseInception
);
