using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("{Symbol} deviation={Deviation} ({Severity})")]
public sealed record DepegAlert
{
    public required string Symbol { get; init; }

    public required decimal CurrentPrice { get; init; }

    public required decimal PegPrice { get; init; }

    public required decimal Deviation { get; init; }

    public required string Severity { get; init; }
}
