using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("{Id}: {Name}")]
public sealed record PoolTypeMetadata
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }
}
