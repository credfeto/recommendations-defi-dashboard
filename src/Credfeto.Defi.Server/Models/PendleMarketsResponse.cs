using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Paginated response from the Pendle markets API.
/// </summary>
[DebuggerDisplay("Total={Total} Results={Results?.Length}")]
public sealed record PendleMarketsResponse
{
    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("results")]
    public PendleMarket[]? Results { get; init; }
}
