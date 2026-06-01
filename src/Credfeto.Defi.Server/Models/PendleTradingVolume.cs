using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Trading volume information within a Pendle market.
/// </summary>
[DebuggerDisplay("Usd={Usd}")]
public sealed record PendleTradingVolume
{
    [JsonPropertyName("usd")]
    public double? Usd { get; init; }
}
