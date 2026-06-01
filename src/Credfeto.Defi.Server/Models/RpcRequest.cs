using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     JSON-RPC 2.0 request payload.
/// </summary>
[DebuggerDisplay("{Method} id={Id}")]
public sealed record RpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; init; } = "2.0";

    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("params")]
    public string[]? Params { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }
}
