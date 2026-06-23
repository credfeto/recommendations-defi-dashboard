using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{PoolId} ({Project}/{Symbol}) Chain={Chain}")]
public sealed record DefiLlamaPoolRow(
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
    double? ApyBaseInception,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);
