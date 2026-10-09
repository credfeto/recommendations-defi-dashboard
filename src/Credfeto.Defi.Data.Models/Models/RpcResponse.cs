using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("Result={Result}")]
public sealed record RpcResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; init; }
}
