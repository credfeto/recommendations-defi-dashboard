using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Stablecoin market entry from the CoinGecko markets API.
/// </summary>
[DebuggerDisplay("{Symbol} price={CurrentPrice}")]
public sealed record CoinGeckoStablecoin
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("current_price")]
    public decimal? CurrentPrice { get; init; }
}
