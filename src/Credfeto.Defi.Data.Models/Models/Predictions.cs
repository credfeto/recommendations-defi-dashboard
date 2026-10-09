using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("PredictedClass={PredictedClass} Probability={PredictedProbability}")]
public sealed record Predictions
{
    public string? PredictedClass { get; init; }

    public double? PredictedProbability { get; init; }

    public double? BinnedConfidence { get; init; }
}
