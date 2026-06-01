using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Coin list entry from CoinGecko, including on-chain contract addresses keyed by platform ID.
/// </summary>
[DebuggerDisplay("{Id} ({Symbol})")]
public sealed record CoinGeckoCoinPlatforms
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("platforms")]
    public IReadOnlyDictionary<string, string>? Platforms { get; init; }
}
