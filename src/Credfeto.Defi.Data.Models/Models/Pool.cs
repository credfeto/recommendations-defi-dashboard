using System;
using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("{Project}/{Symbol} ({Chain}) APY={Apy}% TVL=${TvlUsd}")]
public sealed record Pool
{
    public required string Chain { get; init; }

    public required string Project { get; init; }

    public required string Symbol { get; init; }

    public required string DataSource { get; init; }

    public required double TvlUsd { get; init; }

    public double? ApyBase { get; init; }

    public double? ApyReward { get; init; }

    public required double Apy { get; init; }

    public string[]? RewardTokens { get; init; }

    public required string PoolId { get; init; }

    public double? ApyPct1D { get; init; }

    public double? ApyPct7D { get; init; }

    public double? ApyPct30D { get; init; }

    public required bool Stablecoin { get; init; }

    public required string IlRisk { get; init; }

    public string? Exposure { get; init; }

    public required Predictions Predictions { get; init; }

    public string? PoolMeta { get; init; }

    public double Mu { get; init; }

    public double Sigma { get; init; }

    public int Count { get; init; }

    public bool Outlier { get; init; }

    public string[]? UnderlyingTokens { get; init; }

    public double? Il7d { get; init; }

    public double? ApyBase7d { get; init; }

    public double ApyMean30d { get; init; }

    public double? VolumeUsd1d { get; init; }

    public double? VolumeUsd7d { get; init; }

    public double? ApyBaseInception { get; init; }

    // ── Enrichment fields ──────────────────────────────────────────────────

    public required HackInfo[] Hacks { get; init; } = [];

    public required DepegAlert[] DepegAlerts { get; init; } = [];

    public AuditInfo? AuditInfo { get; init; }

    public required ContractSecurityInfo[] ContractSecurity { get; init; } = [];

    public required PoolAccessInfo AccessInfo { get; init; }

    public required string[] ContractAddresses { get; init; } = [];

    public Uri? Url { get; init; }
}
