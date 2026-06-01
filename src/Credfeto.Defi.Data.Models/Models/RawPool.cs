using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     Raw pool data as returned by the DefiLlama yields API before enrichment.
/// </summary>
[DebuggerDisplay("{Project}/{Symbol} ({Chain})")]
public sealed record RawPool
{
    [JsonPropertyName("chain")]
    public string Chain { get; init; } = string.Empty;

    [JsonPropertyName("project")]
    public string Project { get; init; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("tvlUsd")]
    public double TvlUsd { get; init; }

    [JsonPropertyName("apyBase")]
    public double? ApyBase { get; init; }

    [JsonPropertyName("apyReward")]
    public double? ApyReward { get; init; }

    [JsonPropertyName("apy")]
    public double Apy { get; init; }

    [JsonPropertyName("rewardTokens")]
    public string[]? RewardTokens { get; init; }

    [JsonPropertyName("pool")]
    public string PoolId { get; init; } = string.Empty;

    [JsonPropertyName("apyPct1D")]
    public double? ApyPct1D { get; init; }

    [JsonPropertyName("apyPct7D")]
    public double? ApyPct7D { get; init; }

    [JsonPropertyName("apyPct30D")]
    public double? ApyPct30D { get; init; }

    [JsonPropertyName("stablecoin")]
    public bool Stablecoin { get; init; }

    [JsonPropertyName("ilRisk")]
    public string IlRisk { get; init; } = string.Empty;

    [JsonPropertyName("exposure")]
    public string? Exposure { get; init; }

    [JsonPropertyName("predictions")]
    public RawPredictions? Predictions { get; init; }

    [JsonPropertyName("poolMeta")]
    public string? PoolMeta { get; init; }

    [JsonPropertyName("mu")]
    public double Mu { get; init; }

    [JsonPropertyName("sigma")]
    public double Sigma { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("outlier")]
    public bool Outlier { get; init; }

    [JsonPropertyName("underlyingTokens")]
    public string[]? UnderlyingTokens { get; init; }

    [JsonPropertyName("il7d")]
    public double? Il7d { get; init; }

    [JsonPropertyName("apyBase7d")]
    public double? ApyBase7d { get; init; }

    [JsonPropertyName("apyMean30d")]
    public double ApyMean30d { get; init; }

    [JsonPropertyName("volumeUsd1d")]
    public double? VolumeUsd1d { get; init; }

    [JsonPropertyName("volumeUsd7d")]
    public double? VolumeUsd7d { get; init; }

    [JsonPropertyName("apyBaseInception")]
    public double? ApyBaseInception { get; init; }
}
