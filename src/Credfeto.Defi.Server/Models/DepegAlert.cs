using System.Diagnostics;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Alert raised when a stablecoin deviates materially from its peg.
/// </summary>
[DebuggerDisplay("{Symbol} deviation={Deviation} ({Severity})")]
internal sealed record DepegAlert
{
    /// <summary>
    ///     Stablecoin symbol (e.g. "USDC").
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    ///     Current market price in USD.
    /// </summary>
    public required decimal CurrentPrice { get; init; }

    /// <summary>
    ///     Expected peg price (typically 1.0 for USD stablecoins).
    /// </summary>
    public required decimal PegPrice { get; init; }

    /// <summary>
    ///     Deviation from peg as a fraction (e.g. -0.672 means -67.2% below peg).
    /// </summary>
    public required decimal Deviation { get; init; }

    /// <summary>
    ///     Severity level: "warning" (&gt;0.5%) or "critical" (&gt;2%).
    /// </summary>
    public required string Severity { get; init; }
}
