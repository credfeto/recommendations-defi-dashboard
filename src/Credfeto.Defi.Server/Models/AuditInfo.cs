using System.Diagnostics;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Protocol audit information sourced from DefiLlama.
/// </summary>
[DebuggerDisplay("Audits={Audits}")]
public sealed record AuditInfo
{
    /// <summary>
    ///     Number of audits recorded (0 = none, 1 = single, 2+ = multiple).
    /// </summary>
    public required int Audits { get; init; }

    /// <summary>
    ///     Links to audit reports.
    /// </summary>
    public required string[] AuditLinks { get; init; } = [];
}
