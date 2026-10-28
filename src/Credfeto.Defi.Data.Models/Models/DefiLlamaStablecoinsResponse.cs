using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("Count={PeggedAssets?.Length}")]
public sealed record DefiLlamaStablecoinsResponse
{
    [JsonPropertyName("peggedAssets")]
    public DefiLlamaStablecoin[]? PeggedAssets { get; init; }
}
