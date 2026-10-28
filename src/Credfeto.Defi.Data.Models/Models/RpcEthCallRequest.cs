using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("{Method} id={Id}")]
public sealed record RpcEthCallRequest
{
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; init; } = "2.0";

    [JsonPropertyName("method")]
    public string Method { get; init; } = "eth_call";

    // eth_call params are heterogeneous ([transaction, blockTag]); each element is serialised by its runtime type,
    // so every element type must be registered in AppJsonContext.
    [JsonPropertyName("params")]
    public object[]? Params { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }
}
