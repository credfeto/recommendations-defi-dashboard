using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("PredictedClass={PredictedClass}")]
public sealed record RawPredictions
{
    [JsonPropertyName("predictedClass")]
    public string? PredictedClass { get; init; }

    [JsonPropertyName("predictedProbability")]
    public double? PredictedProbability { get; init; }

    [JsonPropertyName("binnedConfidence")]
    public double? BinnedConfidence { get; init; }
}
