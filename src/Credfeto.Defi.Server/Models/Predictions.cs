using System.Diagnostics;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Machine-learning APY predictions from DefiLlama.
/// </summary>
[DebuggerDisplay("PredictedClass={PredictedClass} Probability={PredictedProbability}")]
internal sealed record Predictions
{
    /// <summary>
    ///     Predicted class label (e.g. "stable").
    /// </summary>
    public string? PredictedClass { get; init; }

    /// <summary>
    ///     Predicted probability of the class.
    /// </summary>
    public double? PredictedProbability { get; init; }

    /// <summary>
    ///     Binned confidence score.
    /// </summary>
    public double? BinnedConfidence { get; init; }
}
