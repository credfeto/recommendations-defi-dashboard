using System.Diagnostics;

namespace Credfeto.Defi.Server.Models;

/// <summary>
///     Metadata describing a pool category exposed via the API.
/// </summary>
[DebuggerDisplay("{Id}: {Name}")]
public sealed record PoolTypeMetadata
{
    /// <summary>
    ///     Machine-readable identifier for the pool category (e.g. "ETH").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     Human-readable display name (e.g. "Ethereum &amp; Liquid Staking").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Short description of what this category contains.
    /// </summary>
    public required string Description { get; init; }
}
