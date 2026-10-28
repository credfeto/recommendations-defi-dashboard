using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("Pegs={Current?.Count}")]
public sealed record DefiLlamaChainCirculation
{
    [JsonPropertyName("current")]
    public IReadOnlyDictionary<string, double>? Current { get; init; }
}
