using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("overallRisk={OverallRisk} count={Count} high={High} medium={Medium}")]
public sealed record WebacyAddressResponse
{
    [JsonPropertyName("overallRisk")]
    public double? OverallRisk { get; init; }

    [JsonPropertyName("count")]
    public int? Count { get; init; }

    [JsonPropertyName("medium")]
    public int? Medium { get; init; }

    [JsonPropertyName("high")]
    public int? High { get; init; }
}
