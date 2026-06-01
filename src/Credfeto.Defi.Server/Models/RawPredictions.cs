using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Raw predictions object from DefiLlama API.
/// </summary>
[DebuggerDisplay("PredictedClass={PredictedClass}")]
internal sealed record RawPredictions
{
    [JsonPropertyName("predictedClass")]
    public string? PredictedClass { get; init; }

    [JsonPropertyName("predictedProbability")]
    public double? PredictedProbability { get; init; }

    [JsonPropertyName("binnedConfidence")]
    public double? BinnedConfidence { get; init; }
}
