using System.Diagnostics;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Describes entry and exit access restrictions for a pool.
/// </summary>
[DebuggerDisplay("KycEntry={KycRequiredForEntry} Liquid={IsLiquid}")]
public sealed record PoolAccessInfo
{
    /// <summary>
    ///     Whether KYC or accreditation is required to enter (deposit into) the pool.
    ///     null = unknown.
    /// </summary>
    public bool? KycRequiredForEntry { get; init; }

    /// <summary>
    ///     Whether KYC or accreditation is required to exit (withdraw from) the pool.
    ///     null = unknown.
    /// </summary>
    public bool? KycRequiredForExit { get; init; }

    /// <summary>
    ///     Whether a DEX swap can be used as an alternative exit path. null = unknown.
    /// </summary>
    public bool? CanSwapToExit { get; init; }

    /// <summary>
    ///     Whether the pool allows immediate withdrawal without a lock or cooldown period.
    ///     null = unknown.
    /// </summary>
    public bool? IsLiquid { get; init; }

    /// <summary>
    ///     Human-readable description of any lock or cooldown period, if present.
    /// </summary>
    public string? LockupDescription { get; init; }
}
