using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("To={To} Data={Data}")]
public sealed record EthCallTransaction
{
    [JsonPropertyName("to")]
    public string To { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; init; } = string.Empty;
}
