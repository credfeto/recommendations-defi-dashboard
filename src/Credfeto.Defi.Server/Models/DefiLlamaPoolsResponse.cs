using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Top-level response from the DefiLlama yields API.
/// </summary>
[DebuggerDisplay("Count={Data?.Length}")]
internal sealed record DefiLlamaPoolsResponse
{
    [JsonPropertyName("data")]
    public RawPool[]? Data { get; init; }
}
