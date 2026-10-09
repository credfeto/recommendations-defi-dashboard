using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("Count={Data?.Length}")]
public sealed record DefiLlamaPoolsResponse
{
    [JsonPropertyName("data")]
    public RawPool[]? Data { get; init; }
}
