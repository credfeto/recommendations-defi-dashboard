using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     A single market entry from the Pendle API.
/// </summary>
[DebuggerDisplay("{SimpleSymbol} chainId={ChainId} isActive={IsActive}")]
public sealed record PendleMarket
{
    [JsonPropertyName("address")]
    public string Address { get; init; } = string.Empty;

    [JsonPropertyName("chainId")]
    public int ChainId { get; init; }

    [JsonPropertyName("simpleSymbol")]
    public string SimpleSymbol { get; init; } = string.Empty;

    [JsonPropertyName("expiry")]
    public string? Expiry { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("categoryIds")]
    public string[]? CategoryIds { get; init; }

    [JsonPropertyName("liquidity")]
    public PendleLiquidity? Liquidity { get; init; }

    [JsonPropertyName("aggregatedApy")]
    public double AggregatedApy { get; init; }

    [JsonPropertyName("underlyingApy")]
    public double UnderlyingApy { get; init; }

    [JsonPropertyName("pendleApy")]
    public double PendleApy { get; init; }

    [JsonPropertyName("lpRewardApy")]
    public double LpRewardApy { get; init; }

    [JsonPropertyName("swapFeeApy")]
    public double SwapFeeApy { get; init; }

    [JsonPropertyName("tradingVolume")]
    public PendleTradingVolume? TradingVolume { get; init; }
}
