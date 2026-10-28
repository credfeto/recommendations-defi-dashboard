using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("{Symbol} {PegType} {PegMechanism} price={Price}")]
public sealed record DefiLlamaStablecoin
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("pegType")]
    public string PegType { get; init; } = string.Empty;

    [JsonPropertyName("pegMechanism")]
    public string PegMechanism { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    // Supply figures use double: some pegged assets report values beyond decimal's range, and
    // one out-of-range number would otherwise fail deserialisation of the whole response.
    [JsonPropertyName("circulating")]
    public IReadOnlyDictionary<string, double>? Circulating { get; init; }

    [JsonPropertyName("chainCirculating")]
    public IReadOnlyDictionary<string, DefiLlamaChainCirculation>? ChainCirculating { get; init; }
}
