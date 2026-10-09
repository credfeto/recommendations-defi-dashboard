using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("Usd={Usd}")]
public sealed record PendleLiquidity
{
    [JsonPropertyName("usd")]
    public double Usd { get; init; }
}
