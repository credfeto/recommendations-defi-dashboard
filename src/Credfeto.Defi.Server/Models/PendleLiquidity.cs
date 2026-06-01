using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Liquidity information within a Pendle market.
/// </summary>
[DebuggerDisplay("Usd={Usd}")]
internal sealed record PendleLiquidity
{
    [JsonPropertyName("usd")]
    public double Usd { get; init; }
}
