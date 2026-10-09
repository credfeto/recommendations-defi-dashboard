using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("Usd={Usd}")]
public sealed record PendleTradingVolume
{
    [JsonPropertyName("usd")]
    public double? Usd { get; init; }
}
