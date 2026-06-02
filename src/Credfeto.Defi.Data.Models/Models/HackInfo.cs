using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     Information about a DeFi protocol exploit or hack.
/// </summary>
[DebuggerDisplay("{Name} ({Date}) ${AmountUsd}")]
public sealed record HackInfo
{
    /// <summary>
    ///     Name of the protocol that was hacked.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Unix timestamp (seconds) of the hack.
    /// </summary>
    public required long Date { get; init; }

    /// <summary>
    ///     Approximate amount lost in USD.
    /// </summary>
    public required decimal AmountUsd { get; init; }

    /// <summary>
    ///     Classification of the attack vector (e.g. "Rug Pull").
    /// </summary>
    public required string Classification { get; init; }

    /// <summary>
    ///     Specific technique used in the exploit.
    /// </summary>
    public required string Technique { get; init; }

    /// <summary>
    ///     Source URL for the hack report.
    /// </summary>
    public required string Source { get; init; }
}
