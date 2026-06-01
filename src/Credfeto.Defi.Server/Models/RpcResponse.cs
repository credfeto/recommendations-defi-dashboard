using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     JSON-RPC 2.0 response payload.
/// </summary>
[DebuggerDisplay("Result={Result}")]
public sealed record RpcResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; init; }
}
