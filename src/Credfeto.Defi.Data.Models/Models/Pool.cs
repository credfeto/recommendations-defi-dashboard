using System;
using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     Fully enriched pool record returned by the API.
/// </summary>
[DebuggerDisplay("{Project}/{Symbol} ({Chain}) APY={Apy}% TVL=${TvlUsd}")]
public sealed record Pool
{
    /// <summary>
    ///     Blockchain network name (e.g. "Ethereum").
    /// </summary>
    public required string Chain { get; init; }

    /// <summary>
    ///     Protocol identifier (e.g. "aave-v3").
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    ///     Pool token symbol string (e.g. "USDC-ETH").
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    ///     Data source identifier ("defillama" or "pendle").
    /// </summary>
    public required string DataSource { get; init; }

    /// <summary>
    ///     Total value locked in USD.
    /// </summary>
    public required double TvlUsd { get; init; }

    /// <summary>
    ///     Base APY (lending/staking yield), or null if not available.
    /// </summary>
    public double? ApyBase { get; init; }

    /// <summary>
    ///     Reward APY (token incentives), or null if not available.
    /// </summary>
    public double? ApyReward { get; init; }

    /// <summary>
    ///     Combined total APY.
    /// </summary>
    public required double Apy { get; init; }

    /// <summary>
    ///     Reward token contract addresses, or null if none.
    /// </summary>
    public string[]? RewardTokens { get; init; }

    /// <summary>
    ///     Unique pool identifier (UUID for DefiLlama, contract address for Pendle).
    /// </summary>
    public required string PoolId { get; init; }

    /// <summary>
    ///     1-day APY percentage change.
    /// </summary>
    public double? ApyPct1D { get; init; }

    /// <summary>
    ///     7-day APY percentage change.
    /// </summary>
    public double? ApyPct7D { get; init; }

    /// <summary>
    ///     30-day APY percentage change.
    /// </summary>
    public double? ApyPct30D { get; init; }

    /// <summary>
    ///     Whether the pool primarily holds stablecoins.
    /// </summary>
    public required bool Stablecoin { get; init; }

    /// <summary>
    ///     Impermanent-loss risk category ("no", "yes", etc.).
    /// </summary>
    public required string IlRisk { get; init; }

    /// <summary>
    ///     Exposure type.
    /// </summary>
    public string? Exposure { get; init; }

    /// <summary>
    ///     ML-based predictions.
    /// </summary>
    public required Predictions Predictions { get; init; }

    /// <summary>
    ///     Optional pool metadata string (e.g. "Maturity 30 Jun 2025").
    /// </summary>
    public string? PoolMeta { get; init; }

    /// <summary>
    ///     Mean APY.
    /// </summary>
    public double Mu { get; init; }

    /// <summary>
    ///     Standard deviation of APY.
    /// </summary>
    public double Sigma { get; init; }

    /// <summary>
    ///     Number of data-points.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    ///     Whether this pool is considered an outlier.
    /// </summary>
    public bool Outlier { get; init; }

    /// <summary>
    ///     Underlying token contract addresses, or null if none.
    /// </summary>
    public string[]? UnderlyingTokens { get; init; }

    /// <summary>
    ///     7-day impermanent loss.
    /// </summary>
    public double? Il7d { get; init; }

    /// <summary>
    ///     7-day base APY.
    /// </summary>
    public double? ApyBase7d { get; init; }

    /// <summary>
    ///     30-day mean APY.
    /// </summary>
    public double ApyMean30d { get; init; }

    /// <summary>
    ///     1-day trading volume in USD.
    /// </summary>
    public double? VolumeUsd1d { get; init; }

    /// <summary>
    ///     7-day trading volume in USD.
    /// </summary>
    public double? VolumeUsd7d { get; init; }

    /// <summary>
    ///     APY since inception.
    /// </summary>
    public double? ApyBaseInception { get; init; }

    // ── Enrichment fields ──────────────────────────────────────────────────

    /// <summary>
    ///     Recorded hacks or exploits for this protocol.
    /// </summary>
    public required HackInfo[] Hacks { get; init; } = [];

    /// <summary>
    ///     Stablecoin depeg alerts detected for tokens in this pool.
    /// </summary>
    public required DepegAlert[] DepegAlerts { get; init; } = [];

    /// <summary>
    ///     Audit information for the protocol, or null if not found.
    /// </summary>
    public AuditInfo? AuditInfo { get; init; }

    /// <summary>
    ///     GoPlus contract security results for each on-chain address.
    /// </summary>
    public required ContractSecurityInfo[] ContractSecurity { get; init; } = [];

    /// <summary>
    ///     Access and liquidity restrictions for this pool.
    /// </summary>
    public required PoolAccessInfo AccessInfo { get; init; }

    /// <summary>
    ///     Deduplicated on-chain contract addresses (underlying tokens, reward tokens, pool contract).
    /// </summary>
    public required string[] ContractAddresses { get; init; } = [];

    /// <summary>
    ///     Direct URL to the pool page, or null if not available.
    /// </summary>
    public Uri? Url { get; init; }
}
