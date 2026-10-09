using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("Total={Total} Results={Results?.Length}")]
public sealed record PendleMarketsResponse
{
    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("results")]
    public PendleMarket[]? Results { get; init; }
}
