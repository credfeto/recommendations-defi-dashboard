using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     Raw hack entry from the DefiLlama hacks API.
/// </summary>
[DebuggerDisplay("{Name} ${Amount}")]
public sealed record RawHack
{
    [JsonPropertyName("date")]
    public long Date { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("classification")]
    public string? Classification { get; init; }

    [JsonPropertyName("technique")]
    public string? Technique { get; init; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("parentProtocolId")]
    public string? ParentProtocolId { get; init; }
}
